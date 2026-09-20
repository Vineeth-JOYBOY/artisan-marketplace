using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ArtisanMarketplace.OrderService.Data;
using ArtisanMarketplace.OrderService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ProductService"]!);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException(
    "Jwt:Key is not configured. Run 'dotnet user-secrets set \"Jwt:Key\" \"<value>\"' in this project for local dev, or set the Jwt__Key environment variable.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var internalServiceKey = builder.Configuration["Internal:ServiceKey"] ?? throw new InvalidOperationException(
    "Internal:ServiceKey is not configured. Run 'dotnet user-secrets set \"Internal:ServiceKey\" \"<value>\"' in this project for local dev, or set the Internal__ServiceKey environment variable.");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

static OrderResponse ToResponse(Order o) => new(
    o.Id, o.Status, o.TotalAmount, o.CreatedAt,
    o.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList());

static Guid GetUserId(ClaimsPrincipal user) =>
    Guid.Parse(user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!.Value);

app.MapPost("/api/orders", async (
    CreateOrderRequest request,
    ClaimsPrincipal user,
    OrderDbContext db,
    IHttpClientFactory httpClientFactory) =>
{
    if (request.Items.Count == 0)
        return Results.BadRequest(new { message = "Cart is empty." });

    if (request.Items.Any(i => i.Quantity <= 0))
        return Results.BadRequest(new { message = "Quantity must be greater than zero." });

    var productClient = httpClientFactory.CreateClient("ProductService");

    async Task ReleaseStockAsync(int productId, int quantity)
    {
        var release = new HttpRequestMessage(HttpMethod.Post, $"/internal/products/{productId}/adjust-stock")
        {
            Content = JsonContent.Create(new AdjustStockRequest(quantity))
        };
        release.Headers.TryAddWithoutValidation("X-Internal-Service-Key", internalServiceKey);
        await productClient.SendAsync(release);
    }

    // Prices, names and stock all come from ProductService — never from the client — so a
    // tampered request body can't change what an order actually costs or oversell stock.
    var orderItems = new List<OrderItem>();

    foreach (var line in request.Items)
    {
        ProductLookupResponse? product;
        try
        {
            product = await productClient.GetFromJsonAsync<ProductLookupResponse>($"/api/products/{line.ProductId}");
        }
        catch (HttpRequestException)
        {
            product = null;
        }

        if (product is null)
        {
            foreach (var reservedItem in orderItems)
                await ReleaseStockAsync(reservedItem.ProductId, reservedItem.Quantity);
            return Results.BadRequest(new { message = $"Unknown product {line.ProductId}." });
        }

        var reserveRequest = new HttpRequestMessage(HttpMethod.Post, $"/internal/products/{line.ProductId}/adjust-stock")
        {
            Content = JsonContent.Create(new AdjustStockRequest(-line.Quantity))
        };
        reserveRequest.Headers.TryAddWithoutValidation("X-Internal-Service-Key", internalServiceKey);

        var reserveResponse = await productClient.SendAsync(reserveRequest);
        if (reserveResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            foreach (var reservedItem in orderItems)
                await ReleaseStockAsync(reservedItem.ProductId, reservedItem.Quantity);
            return Results.Conflict(new { message = $"Insufficient stock for {product.Name}." });
        }
        reserveResponse.EnsureSuccessStatusCode();

        orderItems.Add(new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = line.Quantity
        });
    }

    var order = new Order
    {
        UserId = GetUserId(user),
        Status = "Pending",
        TotalAmount = orderItems.Sum(i => i.UnitPrice * i.Quantity),
        Items = orderItems
    };

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created($"/api/orders/{order.Id}", ToResponse(order));
}).RequireAuthorization();

app.MapGet("/api/orders", async (ClaimsPrincipal user, OrderDbContext db) =>
{
    var userId = GetUserId(user);
    var orders = await db.Orders.Include(o => o.Items)
        .Where(o => o.UserId == userId)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();

    return Results.Ok(orders.Select(ToResponse));
}).RequireAuthorization();

app.MapGet("/api/orders/{id:guid}", async (Guid id, ClaimsPrincipal user, OrderDbContext db) =>
{
    var userId = GetUserId(user);
    var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

    if (order is null || order.UserId != userId)
        return Results.NotFound();

    return Results.Ok(ToResponse(order));
}).RequireAuthorization();

app.Run();
