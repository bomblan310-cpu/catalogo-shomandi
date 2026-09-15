using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileCatalog.Application.Models;
using MobileCatalog.Application.Services;
using MobileCatalog.Domain.Entities;
using MobileCatalog.Infrastructure.Images;
using MobileCatalog.Infrastructure.Persistence;
using MobileCatalog.WebApi.Security;

namespace MobileCatalog.WebApi.Controllers;

[ApiController]
[Route("api/admin/products")]
[ServiceFilter(typeof(AdminApiKeyFilter))]
public sealed class AdminProductsController(MobileCatalogDbContext db, CloudinaryImageService cloudinary) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Product>>> List([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = db.Products.AsNoTracking().Include(product => product.Variants).Include(product => product.Images)
            .Include(product => product.Brand).Include(product => product.Category)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(product => product.IsActive);
        }

        var products = await query.OrderBy(product => product.Name).ToListAsync(cancellationToken);
        return Ok(products);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.FindAsync([id], cancellationToken);
        if (product is null) return NotFound();
        product.IsActive = true;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = Map(request);
        product.Slug = string.IsNullOrWhiteSpace(request.Slug)
            ? await SlugGenerator.UniqueSlugAsync(db.Products.Select(item => item.Slug), request.Name, cancellationToken)
            : request.Slug.Trim();
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(ProductsController.Get), "Products", new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Product>> Update(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await db.Products.Include(item => item.Variants).Include(item => item.Images)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return NotFound();
        product.Name = request.Name.Trim();
        product.Slug = string.IsNullOrWhiteSpace(request.Slug)
            ? await SlugGenerator.UniqueSlugAsync(db.Products.Where(item => item.Id != id).Select(item => item.Slug), request.Name, cancellationToken)
            : request.Slug.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Price = request.Price; product.IsFeatured = request.IsFeatured; product.IsActive = request.IsActive;
        product.CategoryId = request.CategoryId; product.BrandId = request.BrandId; product.UpdatedAtUtc = DateTime.UtcNow;
        product.Variants.Clear();
        product.Images.Clear();
        await db.SaveChangesAsync(cancellationToken);

        var newVariants = request.Variants.Select(Map).ToList();
        newVariants.ForEach(variant => variant.ProductId = product.Id);
        db.ProductVariants.AddRange(newVariants);

        var newImages = request.Images.Select(Map).ToList();
        newImages.ForEach(image => image.ProductId = product.Id);
        db.ProductImages.AddRange(newImages);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .Include(item => item.Variants)
            .Include(item => item.Images)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null) return NotFound();

        if (product.IsActive)
        {
            product.IsActive = false;
            product.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        db.ProductVariants.RemoveRange(product.Variants);
        db.ProductImages.RemoveRange(product.Images);
        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file, [FromQuery] bool isPrimary = false,
        CancellationToken cancellationToken = default)
    {
        if (await db.Products.FindAsync([id], cancellationToken) is null) return NotFound();
        try
        {
            var url = await cloudinary.UploadAsync(file);
            var image = new ProductImage { ProductId = id, CloudinaryUrl = url, IsPrimary = isPrimary };
            db.ProductImages.Add(image);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { image.Id, image.CloudinaryUrl, image.IsPrimary });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("{productId:guid}/variants/{variantId:guid}/stock/increase")]
    public Task<IActionResult> IncreaseStock(Guid productId, Guid variantId, StockAdjustmentRequest request,
        CancellationToken cancellationToken)
        => AdjustStock(productId, variantId, request.Quantity, cancellationToken);

    [HttpPost("{productId:guid}/variants/{variantId:guid}/stock/decrease")]
    public Task<IActionResult> DecreaseStock(Guid productId, Guid variantId, StockAdjustmentRequest request,
        CancellationToken cancellationToken)
        => AdjustStock(productId, variantId, -request.Quantity, cancellationToken);

    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid productId, Guid imageId, CancellationToken cancellationToken)
    {
        var image = await db.ProductImages.SingleOrDefaultAsync(item => item.Id == imageId && item.ProductId == productId, cancellationToken);
        if (image is null) return NotFound();
        db.ProductImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{productId:guid}/images/{imageId:guid}/primary")]
    public async Task<IActionResult> SetPrimaryImage(Guid productId, Guid imageId, CancellationToken cancellationToken)
    {
        var images = await db.ProductImages.Where(item => item.ProductId == productId).ToListAsync(cancellationToken);
        if (images.Count == 0 || images.All(item => item.Id != imageId)) return NotFound();
        foreach (var image in images) image.IsPrimary = image.Id == imageId;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> AdjustStock(Guid productId, Guid variantId, int amount, CancellationToken cancellationToken)
    {
        var variant = await db.ProductVariants.SingleOrDefaultAsync(item => item.Id == variantId && item.ProductId == productId, cancellationToken);
        if (variant is null) return NotFound();
        if (amount < 0 && variant.Stock < -amount) return Conflict("No hay stock suficiente para descontar esa cantidad.");
        variant.Stock += amount;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { variant.Id, variant.Stock });
    }

    private static Product Map(ProductRequest request) => new()
    {
        Name = request.Name.Trim(), Description = request.Description.Trim(), Price = request.Price,
        IsFeatured = request.IsFeatured, IsActive = request.IsActive, CategoryId = request.CategoryId, BrandId = request.BrandId,
        Variants = request.Variants.Select(Map).ToList(), Images = request.Images.Select(Map).ToList()
    };
    private static ProductVariant Map(VariantRequest request) => new() { Storage = request.Storage.Trim(), Color = request.Color.Trim(), Stock = request.Stock, Price = request.Price };
    private static ProductImage Map(ImageRequest request) => new() { CloudinaryUrl = request.CloudinaryUrl.Trim(), AltText = request.AltText?.Trim(), SortOrder = request.SortOrder, IsPrimary = request.IsPrimary };
}
