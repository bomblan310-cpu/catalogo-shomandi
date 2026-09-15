using Microsoft.EntityFrameworkCore;
using MobileCatalog.Domain.Entities;

namespace MobileCatalog.Infrastructure.Persistence;

public sealed class MobileCatalogDbContext(DbContextOptions<MobileCatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Price).HasPrecision(12, 2);
            entity.HasIndex(product => product.Slug).IsUnique();
            entity.HasOne(product => product.Category).WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(product => product.Brand).WithMany(brand => brand.Products)
                .HasForeignKey(product => product.BrandId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasIndex(variant => new { variant.ProductId, variant.Storage, variant.Color }).IsUnique();
            entity.Property(variant => variant.Price).HasPrecision(12, 2);
        });

        modelBuilder.Entity<ProductImage>().HasIndex(image => new { image.ProductId, image.SortOrder });
        modelBuilder.Entity<Category>().HasIndex(category => category.Slug).IsUnique();
        modelBuilder.Entity<Brand>().HasIndex(brand => brand.Slug).IsUnique();
    }
}
