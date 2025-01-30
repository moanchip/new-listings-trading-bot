using EntityBuilders;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<OrderResult> OrderResults { get; init; } = null!;
    public DbSet<PortfolioItem> Portfolio { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use the OrderBuilder for configuration
        OrderBuilder.Configure(modelBuilder);
        PortfolioBuilder.Configure(modelBuilder);
    }
}