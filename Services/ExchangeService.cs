using ExchangeSharp;

public class Exchange
{
    private readonly IExchangeAPI _exchangeAPI;

    public Exchange(ExchangeNameEnum exchangeName, string apiKey, string apiSecret)
    {
        _exchangeAPI = ExchangeAPI.GetExchangeAPIAsync(exchangeName.ToString()).Result;
        _exchangeAPI.LoadAPIKeysUnsecure(apiKey, apiSecret);
    }

    public async Task<decimal> GetPriceAsync(string coinSymbol)
    {
        var ticker = await _exchangeAPI.GetTickerAsync(coinSymbol);
        return ticker.Last;
    }

    public async Task<ExchangeOrderResult?> HandlePlaceOrder(ExchangeOrderRequest orderRequest, bool isPaper = true)
    {
        if (isPaper) return await PlacePaperOrderAsync(orderRequest);
        return await PlaceOrderAsync(orderRequest);
    }

    private async Task<ExchangeOrderResult?> PlaceOrderAsync(ExchangeOrderRequest orderRequest)
    {
        var order = await _exchangeAPI.PlaceOrderAsync(orderRequest);
        return order;
    }

    private async Task<ExchangeOrderResult?> PlacePaperOrderAsync(ExchangeOrderRequest orderRequest)
    {
        var currentPrice = await GetPriceAsync(orderRequest.MarketSymbol);
        return new ExchangeOrderResult
        {
            OrderId = Guid.NewGuid().ToString(),
            ClientOrderId = Guid.NewGuid().ToString(),
            Result = ExchangeAPIOrderResult.Filled,
            Amount = orderRequest.Amount / currentPrice,
            AmountFilled = orderRequest.Amount / currentPrice,
            IsAmountFilledReversed = false,
            Price = currentPrice,
            OrderDate = DateTime.Now.ToUniversalTime(),
            MarketSymbol = orderRequest.MarketSymbol,
            IsBuy = orderRequest.IsBuy,
            Fees = 0
        };
    }
}