using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ArtisanMarketplace.IdentityService.Data;
using ArtisanMarketplace.IdentityService.Models;
using ArtisanMarketplace.IdentityService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TokenService>();

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
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
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

app.MapPost("/api/auth/register", async (RegisterRequest request, IdentityDbContext db, TokenService tokenService) =>
{
    if (await db.Users.AnyAsync(u => u.Email == request.Email))
        return Results.Conflict(new { message = "Email already registered." });

    var user = new User
    {
        Email = request.Email,
        FullName = request.FullName,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var (token, expiresAt) = tokenService.CreateToken(user);
    return Results.Ok(new AuthResponse(token, expiresAt, new UserResponse(user.Id, user.Email, user.FullName)));
});

app.MapPost("/api/auth/login", async (LoginRequest request, IdentityDbContext db, TokenService tokenService) =>
{
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    var (token, expiresAt) = tokenService.CreateToken(user);
    return Results.Ok(new AuthResponse(token, expiresAt, new UserResponse(user.Id, user.Email, user.FullName)));
});

app.MapGet("/api/auth/me", (System.Security.Claims.ClaimsPrincipal claims) =>
{
    var id = claims.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
    var email = claims.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
    var fullName = claims.FindFirst("fullName")?.Value;
    return Results.Ok(new { id, email, fullName });
}).RequireAuthorization();

app.Run();
