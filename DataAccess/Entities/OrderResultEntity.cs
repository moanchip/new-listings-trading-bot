using ExchangeSharp;

public class Exit
{
    public decimal? StopLossPrice { get; set; }
    public decimal? TakeProfitPrice { get; set; }

    public decimal? Pnl { get; set; }
}

public class OrderResult
{
    public int Id { get; set; }
    public ExchangeOrderResult ExchangeOrderResult { get; set; } // Owned entity
    public Exit Exit { get; set; } // Owned entity
}

public class PortfolioItem
{
    public int Id { get; set; }
    public ExchangeOrderResult ExchangeOrderResult { get; set; }
    public Exit Exit { get; set; }

    public static implicit operator PortfolioItem(OrderResult order)
    {
        return new PortfolioItem
        {
            ExchangeOrderResult = new ExchangeOrderResult
            {
                OrderId = order.ExchangeOrderResult.OrderId,
                ClientOrderId = order.ExchangeOrderResult.ClientOrderId,
                Result = order.ExchangeOrderResult.Result,
                MarketSymbol = order.ExchangeOrderResult.MarketSymbol,
                Amount = order.ExchangeOrderResult.Amount,
                AmountFilled = order.ExchangeOrderResult.AmountFilled,
                IsAmountFilledReversed = order.ExchangeOrderResult.IsAmountFilledReversed,
                Price = order.ExchangeOrderResult.Price,
                IsBuy = order.ExchangeOrderResult.IsBuy,
                Fees = order.ExchangeOrderResult.Fees,
                OrderDate = order.ExchangeOrderResult.OrderDate
            },
            Exit = new Exit
            {
                StopLossPrice = order.Exit.StopLossPrice,
                TakeProfitPrice = order.Exit.TakeProfitPrice,
                Pnl = order.Exit.Pnl
            }
        };
    }
}