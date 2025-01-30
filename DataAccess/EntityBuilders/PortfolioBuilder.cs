using ExchangeSharp;
using Microsoft.EntityFrameworkCore;

namespace EntityBuilders;

public static class PortfolioBuilder
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortfolioItem>(entity =>
        {
            entity.ToTable("portfolio");

            entity.HasKey(o => o.Id);

            // Configure ExchangeOrderResult as an owned entity
            entity.OwnsOne(o => o.ExchangeOrderResult, exchangeOrderResult =>
            {
                exchangeOrderResult.Property(e => e.OrderId)
                    .HasColumnName(nameof(ExchangeOrderResult.OrderId))
                    .IsRequired();

                exchangeOrderResult.Property(e => e.MarketSymbol)
                    .HasColumnName(nameof(ExchangeOrderResult.MarketSymbol))
                    .IsRequired()
                    .HasMaxLength(50);

                exchangeOrderResult.Property(e => e.Amount)
                    .HasColumnName(nameof(ExchangeOrderResult.Amount))
                    .IsRequired();

                exchangeOrderResult.Property(e => e.Price)
                    .HasColumnName(nameof(ExchangeOrderResult.Price))
                    .IsRequired(false);

                exchangeOrderResult.Property(e => e.IsBuy)
                    .HasColumnName(nameof(ExchangeOrderResult.IsBuy))
                    .IsRequired();

                exchangeOrderResult.Property(e => e.OrderDate)
                    .HasColumnName(nameof(ExchangeOrderResult.OrderDate))
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();
            });

            // Configure Exit as an owned entity
            entity.OwnsOne(o => o.Exit, exit =>
            {
                exit.Property(e => e.StopLossPrice)
                    .HasColumnName(nameof(Exit.StopLossPrice))
                    .IsRequired(false);

                exit.Property(e => e.TakeProfitPrice)
                    .HasColumnName(nameof(Exit.TakeProfitPrice))
                    .IsRequired(false);
            });
        });
    }
}