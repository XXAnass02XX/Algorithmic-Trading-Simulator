using TrasingSimulator.Services;
using TrasingSimulator.Models;
using NUnit.Framework;

namespace UnitTests;

public class Scripts
{
    private string _apiKey;
    private MarketDataService _marketDataService;

    [SetUp]
    public void SetUp()
    {
        _apiKey = File.ReadAllText("C:\\Users\\anass\\me\\work\\CS\\Algorithmic Trading Simulator\\TrasingSimulator\\apikey.txt").Trim();
        _marketDataService = new MarketDataService(_apiKey);
    }
    [Test]
    public async Task ApiCall()
    {
        TestContext.WriteLine($"API Key starting with: {_apiKey.Substring(0, 5)}..."); 
        Stock? stock = await _marketDataService.GetQuoteAsync("AAPL");
        TestContext.WriteLine(stock?.ToString());
    }

    [Test]
    public async Task AnotherScript()
    {
        // another test
        List<decimal> historicalPrices = await _marketDataService.GetHistoricalPricesAsync("AAPL");
        //ParseHistoricalPrices(string json, int days)
        foreach (decimal historicalPrice in historicalPrices)
        {
            TestContext.WriteLine($"price {historicalPrice}");
        }
    }
}