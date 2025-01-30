using ExchangeSharp;
using Microsoft.EntityFrameworkCore;

namespace new_listing_bot_cs;

public class ExitStrategyWorker : BackgroundService
{
    private readonly BotConfig _botConfig;
    private readonly ILogger<BuyListingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ExitStrategyWorker(ILogger<BuyListingWorker> logger, IServiceProvider serviceProvider, BotConfig botConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botConfig = botConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var exchangeService = scope.ServiceProvider.GetRequiredService<Exchange>();

                var portfolio = await dbContext.Portfolio
                    .Where(o => o.ExchangeOrderResult.Result == ExchangeAPIOrderResult.Filled)
                    .ToListAsync(stoppingToken);

                foreach (var order in portfolio)
                {
                    var currentPrice = await exchangeService.GetPriceAsync(order.ExchangeOrderResult.MarketSymbol);

                    //SELL LOGIC
                    if (currentPrice <= order.Exit.StopLossPrice)
                    {
                        _logger.LogInformation(
                            $"Stop Loss reached (current price: ${currentPrice}. Stop Price: ${order.Exit.StopLossPrice} , selling {order.ExchangeOrderResult.Amount} of {order.ExchangeOrderResult.MarketSymbol}");

                        var orderRequest = new ExchangeOrderRequest
                        {
                            MarketSymbol = order.ExchangeOrderResult.MarketSymbol,
                            Amount = order.ExchangeOrderResult.AmountFilled ?? 0,
                            IsBuy = true,
                            OrderType = OrderType.Market,
                            ExtraParameters =
                            {
                                {
                                    "amount", order.ExchangeOrderResult.AmountFilled ?? 0
                                } // ExchangeSharp BS. We need to pass amount like this if using Poloniex.
                            }
                        };

                        var result = await exchangeService.HandlePlaceOrder(orderRequest, _botConfig.TestMode);
                        if (result == null)
                        {
                            _logger.LogError($"Failed to place order for {orderRequest.MarketSymbol}");
                            continue;
                        }

                        dbContext.OrderResults.Add(new OrderResult
                        {
                            ExchangeOrderResult = result,
                            Exit = new Exit
                            {
                                Pnl = (result.Price - order.ExchangeOrderResult.Price) /
                                    order.ExchangeOrderResult.Price * 100
                            }
                        });
                        dbContext.Portfolio.Remove(order);
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }

                    // TRAILING TP LOGIC
                    else if (currentPrice >= order.Exit.TakeProfitPrice)
                    {
                        var newTakeProfitPrice = currentPrice + currentPrice * _botConfig.TakeProfit / 100;
                        var newStopLossPrice = currentPrice - currentPrice * _botConfig.StopLoss / 100;
                        order.Exit.TakeProfitPrice = newTakeProfitPrice;
                        order.Exit.StopLossPrice = newStopLossPrice;

                        _logger.LogInformation(
                            $"Take Profit updated for {order.ExchangeOrderResult.MarketSymbol}. " +
                            $"New TTP/TSL Prices: {newTakeProfitPrice}/{newStopLossPrice}");

                        dbContext.Portfolio.Update(order);
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ExitStrategyWorker: {ex}");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}