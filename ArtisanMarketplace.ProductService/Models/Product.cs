namespace ArtisanMarketplace.ProductService.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = default!;
    public int StockQuantity { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
