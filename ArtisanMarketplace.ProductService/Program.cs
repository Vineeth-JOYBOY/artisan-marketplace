using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ArtisanMarketplace.ProductService.Data;
using ArtisanMarketplace.ProductService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
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

static ProductResponse ToResponse(Product p) => new(
    p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.StockQuantity,
    new CategoryResponse(p.Category!.Id, p.Category.Name, p.Category.Slug));

app.MapGet("/api/categories", async (ProductDbContext db) =>
    Results.Ok(await db.Categories
        .Select(c => new CategoryResponse(c.Id, c.Name, c.Slug))
        .ToListAsync()));

app.MapGet("/api/products", async (ProductDbContext db, int? categoryId, string? search) =>
{
    var query = db.Products.Include(p => p.Category).AsQueryable();

    if (categoryId is not null)
        query = query.Where(p => p.CategoryId == categoryId);

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

    var products = await query.ToListAsync();
    return Results.Ok(products.Select(ToResponse));
});

app.MapGet("/api/products/{id:int}", async (int id, ProductDbContext db) =>
{
    var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
    return product is null ? Results.NotFound() : Results.Ok(ToResponse(product));
});

app.MapPost("/api/products", async (ProductRequest request, ProductDbContext db) =>
{
    var category = await db.Categories.FindAsync(request.CategoryId);
    if (category is null)
        return Results.BadRequest(new { message = "Unknown categoryId." });

    var product = new Product
    {
        Name = request.Name,
        Description = request.Description,
        Price = request.Price,
        ImageUrl = request.ImageUrl,
        StockQuantity = request.StockQuantity,
        CategoryId = request.CategoryId
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();
    await db.Entry(product).Reference(p => p.Category).LoadAsync();

    return Results.Created($"/api/products/{product.Id}", ToResponse(product));
}).RequireAuthorization();

app.MapPut("/api/products/{id:int}", async (int id, ProductRequest request, ProductDbContext db) =>
{
    var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
    if (product is null) return Results.NotFound();

    product.Name = request.Name;
    product.Description = request.Description;
    product.Price = request.Price;
    product.ImageUrl = request.ImageUrl;
    product.StockQuantity = request.StockQuantity;
    product.CategoryId = request.CategoryId;

    await db.SaveChangesAsync();
    await db.Entry(product).Reference(p => p.Category).LoadAsync();
    return Results.Ok(ToResponse(product));
}).RequireAuthorization();

app.MapDelete("/api/products/{id:int}", async (int id, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

// Service-to-service only: lives under /internal, which the gateway has no route for, so it
// is unreachable from outside regardless of the key check below — that check is defense in
// depth, not the only thing standing between the public internet and this endpoint.
// OrderService reserves/releases stock with the shared key; Delta is negative to reserve,
// positive to release on rollback.
app.MapPost("/internal/products/{id:int}/adjust-stock", async (int id, AdjustStockRequest request, HttpContext httpContext, ProductDbContext db) =>
{
    var providedKey = httpContext.Request.Headers["X-Internal-Service-Key"].ToString();
    if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey), Encoding.UTF8.GetBytes(internalServiceKey)))
    {
        return Results.Unauthorized();
    }

    if (!await db.Products.AnyAsync(p => p.Id == id))
        return Results.NotFound();

    // A single conditional UPDATE, not read-then-write: the stock check and the decrement
    // happen as one atomic statement, so concurrent requests can't race past each other and
    // lose an update (which let 15 concurrent orders all "succeed" against 5 units of stock).
    var rowsAffected = await db.Products
        .Where(p => p.Id == id && p.StockQuantity + request.Delta >= 0)
        .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.StockQuantity, p => p.StockQuantity + request.Delta));

    if (rowsAffected == 0)
        return Results.Conflict(new { message = "Insufficient stock." });

    var product = await db.Products.AsNoTracking().FirstAsync(p => p.Id == id);
    return Results.Ok(new StockResponse(product.Id, product.StockQuantity));
});

app.Run();
