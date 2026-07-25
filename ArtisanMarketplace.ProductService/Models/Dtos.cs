namespace ArtisanMarketplace.ProductService.Models;

public record ProductRequest(string Name, string Description, decimal Price, string ImageUrl, int StockQuantity, int CategoryId);

public record CategoryResponse(int Id, string Name, string Slug);

public record ProductResponse(int Id, string Name, string Description, decimal Price, string ImageUrl, int StockQuantity, CategoryResponse Category);
