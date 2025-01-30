using ExchangeSharp;
using Microsoft.EntityFrameworkCore;

namespace new_listing_bot_cs;

public class BuyListingWorker : BackgroundService
{
    private readonly BotConfig _botConfig;
    private readonly ILogger<BuyListingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;


    public BuyListingWorker(ILogger<BuyListingWorker> logger, IServiceProvider serviceProvider, BotConfig botConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botConfig = botConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Listings Worker");

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var exchangeService = scope.ServiceProvider.GetRequiredService<Exchange>();
                var listingService = scope.ServiceProvider.GetRequiredService<ListingsGetter>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var listings = await listingService.GetListings();
                var latestAnnouncement = listings.Data.Catalogs[0].Articles[0].Title;
                // latestAnnouncement =
                //     "Binance will list Bitcoin (BTC), Solana (SOL), Ethereum (ETH) and Dogecoin (DOGE)"; // For testing
                // _logger.LogInformation($"Latest Announcement: {latestAnnouncement}");

                if (!latestAnnouncement.ToLower().Contains("will list"))
                {
                    // Any lower you'll get rate limited.
                    await Task.Delay(40, stoppingToken);
                    continue;
                }

                _logger.LogInformation($"NEW LISTING ANNOUNCEMENT: {latestAnnouncement}");

                var symbols = ListingsGetter.ExtractSymbols(latestAnnouncement);
                foreach (var symbol in symbols)
                    try
                    {
                        var symbolExists = await dbContext.Portfolio
                            .AnyAsync(x => x.ExchangeOrderResult.MarketSymbol == $"{symbol.ToUpper()}_USDT",
                                stoppingToken);
                        if (symbolExists)
                        {
                            _logger.LogDebug($"{symbol} already bought, skipping...");
                            continue;
                        }

                        _logger.LogInformation(
                            $"Buying {_botConfig.BuyAmount} of {symbol}. We will only Buy the same asset once.");

                        var orderRequest = new ExchangeOrderRequest
                        {
                            MarketSymbol = $"{symbol.ToUpper()}_USDT",
                            Amount = _botConfig.BuyAmount,
                            IsBuy = true,
                            OrderType = OrderType.Market,
                            ExtraParameters =
                            {
                                {
                                    "amount", _botConfig.BuyAmount
                                } // ExchangeSharp BS. We need to pass amount like this if using Poloniex.
                            }
                        };

                        var result = await exchangeService.HandlePlaceOrder(orderRequest);
                        if (result == null)
                        {
                            _logger.LogError($"Failed to place order for {symbol}");
                            continue;
                        }

                        var order = new OrderResult
                        {
                            ExchangeOrderResult = result,
                            Exit = new Exit
                            {
                                TakeProfitPrice = result.Price + result.Price * _botConfig.TakeProfit / 100,
                                StopLossPrice = result.Price - result.Price * _botConfig.StopLoss / 100
                            }
                        };

                        dbContext.OrderResults.Add(order);
                        dbContext.Portfolio.Add(order);

                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"{symbol} Outcome: {result}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing {symbol}: {ex}");
                    }

                // Sleep for a second - only for testing purposes
                await Task.Delay(300, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in BuyListingWorker, likely rate limited. Sleeping for a minute: {ex}");
                await Task.Delay(60000, stoppingToken);
            }
    }
}