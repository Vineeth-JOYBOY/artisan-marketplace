using Microsoft.EntityFrameworkCore;
using ArtisanMarketplace.ProductService.Models;

namespace ArtisanMarketplace.ProductService.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Pottery", Slug = "pottery" },
            new Category { Id = 2, Name = "Woodwork", Slug = "woodwork" },
            new Category { Id = 3, Name = "Textiles", Slug = "textiles" },
            new Category { Id = 4, Name = "Jewelry", Slug = "jewelry" }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Hand-thrown Ceramic Mug", Description = "Stoneware mug, wheel-thrown and glazed in matte blue.", Price = 28.00m, ImageUrl = "/images/ceramic-mug.jpg", StockQuantity = 40, CategoryId = 1 },
            new Product { Id = 2, Name = "Speckled Clay Bowl Set", Description = "Set of 4 speckled stoneware bowls, food-safe glaze.", Price = 64.00m, ImageUrl = "/images/clay-bowls.jpg", StockQuantity = 15, CategoryId = 1 },
            new Product { Id = 3, Name = "Walnut Cutting Board", Description = "Solid walnut board with hand-carved juice groove.", Price = 89.00m, ImageUrl = "/images/walnut-board.jpg", StockQuantity = 20, CategoryId = 2 },
            new Product { Id = 4, Name = "Oak Wall Shelf", Description = "Floating oak shelf, hand-sanded and oiled finish.", Price = 54.00m, ImageUrl = "/images/oak-shelf.jpg", StockQuantity = 25, CategoryId = 2 },
            new Product { Id = 5, Name = "Handwoven Wool Throw", Description = "Chunky wool throw blanket, woven on a traditional loom.", Price = 120.00m, ImageUrl = "/images/wool-throw.jpg", StockQuantity = 12, CategoryId = 3 },
            new Product { Id = 6, Name = "Linen Table Runner", Description = "100% linen runner, naturally dyed with botanical pigments.", Price = 38.00m, ImageUrl = "/images/linen-runner.jpg", StockQuantity = 30, CategoryId = 3 },
            new Product { Id = 7, Name = "Sterling Silver Leaf Earrings", Description = "Hand-forged sterling silver earrings, leaf motif.", Price = 46.00m, ImageUrl = "/images/silver-earrings.jpg", StockQuantity = 18, CategoryId = 4 },
            new Product { Id = 8, Name = "Copper Wire Pendant", Description = "Wire-wrapped copper pendant with a raw quartz center.", Price = 32.00m, ImageUrl = "/images/copper-pendant.jpg", StockQuantity = 22, CategoryId = 4 }
        );
    }
}
