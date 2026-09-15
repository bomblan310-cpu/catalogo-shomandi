using System.Text.Json.Serialization;

namespace MobileCatalog.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<ProductVariant> Variants { get; set; } = [];
    public List<ProductImage> Images { get; set; } = [];
}

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    [JsonIgnore] public List<Product> Products { get; set; } = [];
}

public sealed class Brand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    [JsonIgnore] public List<Product> Products { get; set; } = [];
}

public sealed class ProductVariant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    [JsonIgnore] public Product Product { get; set; } = null!;
    public string Storage { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal? Price { get; set; }
}

public sealed class ProductImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    [JsonIgnore] public Product Product { get; set; } = null!;
    public string CloudinaryUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}
