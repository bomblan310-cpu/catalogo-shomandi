using System.ComponentModel.DataAnnotations;

namespace MobileCatalogAPI.Models;

public sealed class ProductRequest
{
    [Required, StringLength(160)] public string Name { get; init; } = string.Empty;
    [StringLength(160)] public string? Slug { get; init; }
    [StringLength(4000)] public string Description { get; init; } = string.Empty;
    [Range(typeof(decimal), "0", "999999999")] public decimal Price { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsActive { get; init; } = true;
    [Required] public Guid CategoryId { get; init; }
    [Required] public Guid BrandId { get; init; }
    public List<VariantRequest> Variants { get; init; } = [];
    public List<ImageRequest> Images { get; init; } = [];
}

public sealed class VariantRequest
{
    [Required, StringLength(40)] public string Storage { get; init; } = string.Empty;
    [Required, StringLength(60)] public string Color { get; init; } = string.Empty;
    [Range(0, int.MaxValue)] public int Stock { get; init; }
    [Range(typeof(decimal), "0", "999999999")] public decimal? Price { get; init; }
}

public sealed class ImageRequest
{
    [Required, Url] public string CloudinaryUrl { get; init; } = string.Empty;
    [StringLength(160)] public string? AltText { get; init; }
    [Range(0, 100)] public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class TaxonomyRequest
{
    [Required, StringLength(120)] public string Name { get; init; } = string.Empty;
}

public sealed record ProductSummary(Guid Id, string Name, Guid BrandId, string Brand, Guid CategoryId, string Category, decimal Price, bool IsFeatured, string? PrimaryImageUrl, bool HasPriceRange = false);
public sealed record WhatsAppResponse(string Url);
public sealed record StoreContact(string WhatsAppPhone);
public sealed record StockAdjustmentRequest([Range(1, int.MaxValue)] int Quantity);
