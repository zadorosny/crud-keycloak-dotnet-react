using MfaCrud.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MfaCrud.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<Product>();
        product.HasKey(p => p.Id);
        product.Property(p => p.Name).HasMaxLength(120).IsRequired();
        product.Property(p => p.Description).HasMaxLength(1000);
        product.Property(p => p.Price).HasPrecision(12, 2);
        product.HasIndex(p => p.Name);
    }
}
