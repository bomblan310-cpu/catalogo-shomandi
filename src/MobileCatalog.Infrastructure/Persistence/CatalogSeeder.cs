using Microsoft.EntityFrameworkCore;
using MobileCatalog.Domain.Entities;

namespace MobileCatalog.Infrastructure.Persistence;

public static class CatalogSeeder
{
    public static async Task InitializeAsync(MobileCatalogDbContext database)
    {
        await database.Database.EnsureCreatedAsync();
        if (await database.Products.AnyAsync()) return;

        var smartphones = await database.Categories.SingleOrDefaultAsync(item => item.Slug == "smartphones")
            ?? new Category { Name = "Smartphones", Slug = "smartphones" };
        var apple = await GetOrCreateBrandAsync(database, "Apple", "apple");
        var samsung = await GetOrCreateBrandAsync(database, "Samsung", "samsung");
        var xiaomi = await GetOrCreateBrandAsync(database, "Xiaomi", "xiaomi");
        if (database.Entry(smartphones).State == EntityState.Detached)
            database.Categories.Add(smartphones);
        await database.SaveChangesAsync();

        database.Products.AddRange(
            Create("iPhone 15 Pro", "iphone-15-pro", smartphones, apple, 7999000m, true, "Titanio natural"),
            Create("Galaxy S24 Ultra", "galaxy-s24-ultra", smartphones, samsung, 6499000m, true, "Titanium Gray"),
            Create("Redmi Note 13 Pro", "redmi-note-13-pro", smartphones, xiaomi, 1899000m, false, "Midnight Black"));
        await database.SaveChangesAsync();
    }

    private static async Task<Brand> GetOrCreateBrandAsync(MobileCatalogDbContext database, string name, string slug)
        => await database.Brands.SingleOrDefaultAsync(item => item.Slug == slug)
            ?? new Brand { Name = name, Slug = slug };

    private static Product Create(string name, string slug, Category category, Brand brand, decimal price, bool featured, string color)
    {
        var product = new Product
        {
            Name = name, Slug = slug, Price = price, IsFeatured = featured,
            Category = category, Brand = brand,
            Description = "Equipo seleccionado por VOLTA con garantía y atención directa."
        };
        product.Variants.Add(new ProductVariant { Storage = "256 GB", Color = color, Stock = 5 });
        return product;
    }
}
