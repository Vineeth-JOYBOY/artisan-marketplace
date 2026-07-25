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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });
builder.Services.AddAuthorization();

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

app.MapPost("/api/orders", async (CreateOrderRequest request, ClaimsPrincipal user, OrderDbContext db) =>
{
    if (request.Items.Count == 0)
        return Results.BadRequest(new { message = "Cart is empty." });

    var order = new Order
    {
        UserId = GetUserId(user),
        Status = "Pending",
        TotalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity),
        Items = request.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
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
