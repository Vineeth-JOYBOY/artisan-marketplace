using Microsoft.EntityFrameworkCore;
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
});

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
});

app.MapDelete("/api/products/{id:int}", async (int id, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
