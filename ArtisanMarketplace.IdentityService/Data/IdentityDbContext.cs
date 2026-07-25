using Microsoft.EntityFrameworkCore;
using ArtisanMarketplace.IdentityService.Models;

namespace ArtisanMarketplace.IdentityService.Data;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
