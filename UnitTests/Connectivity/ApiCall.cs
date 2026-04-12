using TrasingSimulator.Services;
using TrasingSimulator.Models;
using NUnit.Framework;

namespace UnitTests;

public class Scripts
{
    [Test]
    public async Task ApiCall()
    {
        string apiKey = File.ReadAllText("C:\\Users\\anass\\me\\work\\CS\\Algorithmic Trading Simulator\\TrasingSimulator\\apikey.txt").Trim();
        MarketDataService marketDataService = new MarketDataService(apiKey);
        TestContext.WriteLine($"API Key starting with: {apiKey.Substring(0, 5)}..."); 
        Stock? stock = await marketDataService.GetQuoteAsync("AAPL");

        TestContext.WriteLine(stock?.ToString()); // ✅ shows in Rider's test output
    }

    [Test]
    public async Task AnotherScript()
    {
        // another thing you want to run
    }
}