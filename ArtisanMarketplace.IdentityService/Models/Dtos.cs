namespace ArtisanMarketplace.IdentityService.Models;

public record RegisterRequest(string Email, string Password, string FullName);

public record LoginRequest(string Email, string Password);

public record UserResponse(Guid Id, string Email, string FullName);

public record AuthResponse(string Token, DateTime ExpiresAt, UserResponse User);
