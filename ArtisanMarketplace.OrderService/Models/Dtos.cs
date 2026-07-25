namespace ArtisanMarketplace.OrderService.Models;

public record OrderItemRequest(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CreateOrderRequest(List<OrderItemRequest> Items);

public record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderResponse(Guid Id, string Status, decimal TotalAmount, DateTime CreatedAt, List<OrderItemResponse> Items);
