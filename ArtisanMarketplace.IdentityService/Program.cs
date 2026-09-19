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
        options.MapInboundClaims = false;
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
    var email = request.Email.Trim().ToLowerInvariant();

    if (await db.Users.AnyAsync(u => u.Email == email))
        return Results.Conflict(new { message = "Email already registered." });

    var user = new User
    {
        Email = email,
        FullName = request.FullName,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
    };

    db.Users.Add(user);

    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        return Results.Conflict(new { message = "Email already registered." });
    }

    var (token, expiresAt) = tokenService.CreateToken(user);
    return Results.Ok(new AuthResponse(token, expiresAt, new UserResponse(user.Id, user.Email, user.FullName)));
});

app.MapPost("/api/auth/login", async (LoginRequest request, IdentityDbContext db, TokenService tokenService) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    var (token, expiresAt) = tokenService.CreateToken(user);
    return Results.Ok(new AuthResponse(token, expiresAt, new UserResponse(user.Id, user.Email, user.FullName)));
});

app.MapPost("/api/auth/forgot-password", async (ForgotPasswordRequest request, IdentityDbContext db, ILogger<Program> logger) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

    if (user is not null)
    {
        var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
        await db.SaveChangesAsync();

        var resetLink = $"http://localhost:3000/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
        // No email provider is configured for this demo, so the reset link is logged instead of sent.
        logger.LogInformation("Password reset requested for {Email}. Reset link: {ResetLink}", user.Email, resetLink);
    }

    // Always return the same response so the endpoint can't be used to enumerate registered emails.
    return Results.Ok(new { message = "If that email is registered, a password reset link has been sent." });
});

app.MapPost("/api/auth/reset-password", async (ResetPasswordRequest request, IdentityDbContext db) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

    if (user is null || user.PasswordResetToken is null || user.PasswordResetTokenExpiresAt is null
        || user.PasswordResetTokenExpiresAt < DateTime.UtcNow
        || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(user.PasswordResetToken), Encoding.UTF8.GetBytes(request.Token)))
    {
        return Results.BadRequest(new { message = "Invalid or expired reset link." });
    }

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    user.PasswordResetToken = null;
    user.PasswordResetTokenExpiresAt = null;
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Password has been reset. You can now log in." });
});

app.MapGet("/api/auth/me", (System.Security.Claims.ClaimsPrincipal claims) =>
{
    var id = claims.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
    var email = claims.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
    var fullName = claims.FindFirst("fullName")?.Value;
    return Results.Ok(new { id, email, fullName });
}).RequireAuthorization();

app.Run();
