using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ArtisanMarketplace.IdentityService.Models;

namespace ArtisanMarketplace.IdentityService.Services;

public class TokenService(IConfiguration config)
{
    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var jwtSection = config.GetSection("Jwt");
        var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException(
            "Jwt:Key is not configured. Run 'dotnet user-secrets set \"Jwt:Key\" \"<value>\"' in this project for local dev, or set the Jwt__Key environment variable.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpiresMinutes"] ?? "120"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fullName", user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
