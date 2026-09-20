namespace ArtisanMarketplace.OrderService.Models;

// ProductId and Quantity are the only client-supplied fields. Price and product name are
// looked up server-side from ProductService — never trust a client-supplied price.
public record OrderItemRequest(int ProductId, int Quantity);

public record CreateOrderRequest(List<OrderItemRequest> Items);

public record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderResponse(Guid Id, string Status, decimal TotalAmount, DateTime CreatedAt, List<OrderItemResponse> Items);

public record ProductLookupResponse(int Id, string Name, decimal Price, int StockQuantity);

public record AdjustStockRequest(int Delta);
