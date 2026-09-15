using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileCatalogAPI.Data;
using MobileCatalogAPI.Models;
using MobileCatalogAPI.Services;

namespace MobileCatalogAPI.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(MobileCatalogDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpGet("contact")]
    public ActionResult<StoreContact> Contact()
    {
        var phone = new string((configuration["WhatsApp:PhoneNumber"] ?? string.Empty).Where(char.IsDigit).ToArray());
        return Ok(new StoreContact(phone));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyCollection<Category>>> Categories(CancellationToken cancellationToken)
        => Ok(await db.Categories.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken));

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyCollection<Brand>>> Brands(CancellationToken cancellationToken)
        => Ok(await db.Brands.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken));

    [HttpPost("categories")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<Category>> CreateCategory(TaxonomyRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var slug = await SlugGenerator.UniqueSlugAsync(db.Categories.Select(item => item.Slug), name, cancellationToken);
        var category = new Category { Name = name, Slug = slug };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Categories), value: category);
    }

    [HttpPost("brands")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<Brand>> CreateBrand(TaxonomyRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var slug = await SlugGenerator.UniqueSlugAsync(db.Brands.Select(item => item.Slug), name, cancellationToken);
        var brand = new Brand { Name = name, Slug = slug };
        db.Brands.Add(brand);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Brands), value: brand);
    }
}