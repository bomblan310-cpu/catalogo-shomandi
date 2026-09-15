using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileCatalog.Application.Models;
using MobileCatalog.Application.Services;
using MobileCatalog.Domain.Entities;
using MobileCatalog.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

namespace MobileCatalog.WebApi.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(MobileCatalogDbContext db, WhatsAppLinkService whatsApp) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProductSummary>>> List(
        [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] string? search, [FromQuery] bool? featured,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.AsNoTracking().Where(product => product.IsActive);
        if (categoryId.HasValue) query = query.Where(product => product.CategoryId == categoryId);
        if (brandId.HasValue) query = query.Where(product => product.BrandId == brandId);
        if (minPrice.HasValue) query = query.Where(product => product.Price >= minPrice);
        if (maxPrice.HasValue) query = query.Where(product => product.Price <= maxPrice);
        if (featured.HasValue) query = query.Where(product => product.IsFeatured == featured);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(product => product.Name.ToLower().Contains(search.ToLower()));

        var products = await query.OrderByDescending(product => product.IsFeatured).ThenBy(product => product.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(product => new
            {
                product.Id, product.Name, product.BrandId, Brand = product.Brand.Name,
                product.CategoryId, Category = product.Category.Name, product.Price, product.IsFeatured,
                Image = product.Images.OrderByDescending(image => image.IsPrimary).ThenBy(image => image.SortOrder).Select(image => image.CloudinaryUrl).FirstOrDefault(),
                Variants = product.Variants.Select(variant => new { variant.Stock, variant.Price }).ToList()
            })
            .ToListAsync(cancellationToken);
        var summaries = products.Select(product =>
        {
            var available = product.Variants.Where(variant => variant.Stock > 0).ToList();
            var prices = (available.Count > 0 ? available : product.Variants).Select(variant => variant.Price ?? product.Price).ToList();
            var price = prices.Count > 0 ? prices.Min() : product.Price;
            return new ProductSummary(product.Id, product.Name, product.BrandId, product.Brand,
                product.CategoryId, product.Category, price, product.IsFeatured, product.Image,
                prices.Distinct().Skip(1).Any());
        }).ToList();
        return Ok(summaries);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> Get(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().Include(product => product.Brand).Include(product => product.Category)
            .Include(product => product.Variants).Include(product => product.Images)
            .SingleOrDefaultAsync(product => product.Id == id && product.IsActive, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("{id:guid}/whatsapp")]
    public async Task<ActionResult<WhatsAppResponse>> WhatsApp(Guid id, [FromQuery, Required] string phone,
        [FromQuery] Guid? variantId, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().Include(item => item.Variants)
            .SingleOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);
        if (product is null) return NotFound();
        var variant = product.Variants.SingleOrDefault(item => item.Id == variantId);
        if (variantId.HasValue && variant is null) return BadRequest("La variante no existe para este producto.");
        var price = variant?.Price ?? product.Price;
        var variantText = variant is null ? null : $"{variant.Storage}, {variant.Color}";
        return Ok(new WhatsAppResponse(whatsApp.CreateProductOrderUrl(phone, product.Name, variantText, price)));
    }
}
