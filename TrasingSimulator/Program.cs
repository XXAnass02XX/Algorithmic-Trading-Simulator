using TrasingSimulator.Helpers;
using TrasingSimulator.Services;
using TrasingSimulator.Helpers;
using TrasingSimulator.Models;
using TrasingSimulator.Services;

// ─── Bootstrap ────────────────────────────────────────────────────────────────

ConsoleDisplay.PrintWelcome();

// Replace with your real Alpha Vantage key from alphavantage.co
string API_KEY = File.ReadAllText("C:\\Users\\anass\\me\\work\\CS\\Algorithmic Trading Simulator\\TrasingSimulator\\apikey.txt").Trim();

var marketData = new MarketDataService(API_KEY);
var portfolio  = new PortfolioService(startingBalance: 10_000m);
var strategy   = new StrategyEngine(shortPeriod: 10, longPeriod: 50);

// Load any previously saved portfolio from disk
portfolio.LoadFromFile();

ConsoleDisplay.PrintInfo($"Portfolio loaded — " +
                         $"Cash: ${portfolio.CashBalance:F2}, " +
                         $"Positions: {portfolio.PositionCount}");

// ─── Main Loop ────────────────────────────────────────────────────────────────

bool running = true;

while (running)
{
    ConsoleDisplay.PrintMenu();
    string choice = Console.ReadLine()?.Trim() ?? "";

    switch (choice)
    {
        // ── 1. Portfolio Summary ──────────────────────────────────────────
        case "1":
            var summaryPrices = await FetchCurrentPricesAsync();
            portfolio.PrintSummary(summaryPrices);
            break;

        // ── 2. Open Positions ─────────────────────────────────────────────
        case "2":
            var positionPrices = await FetchCurrentPricesAsync();
            portfolio.PrintPositions(positionPrices);
            break;

        // ── 3. Buy Shares ─────────────────────────────────────────────────
        case "3":
            string buySymbol = ConsoleDisplay.PromptInput("Symbol").ToUpper();
            if (string.IsNullOrEmpty(buySymbol)) break;

            ConsoleDisplay.PrintInfo($"Fetching quote for {buySymbol}...");
            Stock? buyStock = await marketData.GetQuoteAsync(buySymbol);

            if (buyStock == null)
            {
                ConsoleDisplay.PrintError("Could not fetch quote. Check symbol.");
                break;
            }

            ConsoleDisplay.PrintQuote(buyStock);

            int buyShares = ConsoleDisplay.PromptInt("How many shares to buy");
            portfolio.Buy(buySymbol, buyShares, buyStock.CurrentPrice);
            break;

        // ── 4. Sell Shares ────────────────────────────────────────────────
        case "4":
            if (portfolio.PositionCount == 0)
            {
                ConsoleDisplay.PrintError("You have no open positions to sell.");
                break;
            }

            string sellSymbol = ConsoleDisplay.PromptInput("Symbol").ToUpper();
            if (string.IsNullOrEmpty(sellSymbol)) break;

            ConsoleDisplay.PrintInfo($"Fetching quote for {sellSymbol}...");
            Stock? sellStock = await marketData.GetQuoteAsync(sellSymbol);

            if (sellStock == null)
            {
                ConsoleDisplay.PrintError("Could not fetch quote. Check symbol.");
                break;
            }

            ConsoleDisplay.PrintQuote(sellStock);

            int sellShares = ConsoleDisplay.PromptInt("How many shares to sell");
            portfolio.Sell(sellSymbol, sellShares, sellStock.CurrentPrice);
            break;

        // ── 5. Trade History ──────────────────────────────────────────────
        case "5":
            portfolio.PrintTradeHistory();
            break;

        // ── 6. Get Stock Quote ────────────────────────────────────────────
        case "6":
            string quoteSymbol = ConsoleDisplay.PromptInput("Symbol").ToUpper();
            if (string.IsNullOrEmpty(quoteSymbol)) break;

            ConsoleDisplay.PrintInfo("Fetching quote...");
            Stock? quote = await marketData.GetQuoteAsync(quoteSymbol);

            if (quote == null)
                ConsoleDisplay.PrintError("Symbol not found.");
            else
                ConsoleDisplay.PrintQuote(quote);

            break;

        // ── 7. Run Strategy Signal ────────────────────────────────────────
        case "7":
            string sigSymbol = ConsoleDisplay.PromptInput("Symbol").ToUpper();
            if (string.IsNullOrEmpty(sigSymbol)) break;

            ConsoleDisplay.PrintInfo("Fetching historical prices (60 days)...");

            Stock? sigStock = await marketData.GetQuoteAsync(sigSymbol);
            List<decimal> sigPrices = await marketData
                .GetHistoricalPricesAsync(sigSymbol, 60);

            if (sigPrices.Count == 0 || sigStock == null)
            {
                ConsoleDisplay.PrintError("Could not fetch data.");
                break;
            }

            Signal signal = strategy.GetSignal(sigPrices);
            var (shortMA, longMA) = strategy.GetMovingAverages(sigPrices);

            ConsoleDisplay.PrintSignal(
                signal, sigSymbol, shortMA, longMA, sigStock.CurrentPrice);

            // Offer to execute the trade automatically
            if (signal != Signal.Hold)
            {
                string action = signal == Signal.Buy ? "buy" : "sell";
                Console.Write($"\n  Auto-execute {action} signal? (y/n): ");
                string confirm = Console.ReadLine()?.Trim().ToLower() ?? "n";

                if (confirm == "y")
                {
                    if (signal == Signal.Buy)
                    {
                        int autoBuyShares = ConsoleDisplay.PromptInt("Shares to buy");
                        portfolio.Buy(sigSymbol, autoBuyShares, sigStock.CurrentPrice);
                    }
                    else
                    {
                        if (portfolio.Positions.ContainsKey(sigSymbol))
                        {
                            int ownedShares = portfolio.Positions[sigSymbol].Shares;
                            ConsoleDisplay.PrintInfo($"You own {ownedShares} shares.");
                            int autoSellShares = ConsoleDisplay.PromptInt("Shares to sell");
                            portfolio.Sell(sigSymbol, autoSellShares, sigStock.CurrentPrice);
                        }
                        else
                        {
                            ConsoleDisplay.PrintError(
                                $"You don't own any {sigSymbol} to sell.");
                        }
                    }
                }
            }
            break;

        // ── 8. Backtest Strategy ──────────────────────────────────────────
        case "8":
            string btSymbol = ConsoleDisplay.PromptInput("Symbol").ToUpper();
            if (string.IsNullOrEmpty(btSymbol)) break;

            ConsoleDisplay.PrintInfo(
                "Fetching 100 days of historical data for backtest...");

            List<decimal> btPrices = await marketData
                .GetHistoricalPricesAsync(btSymbol, 100);

            if (btPrices.Count < 51)
            {
                ConsoleDisplay.PrintError(
                    "Not enough data. Need at least 51 days.");
                break;
            }

            BacktestResult result = strategy.Backtest(
                btSymbol, btPrices, startingCapital: 10_000m);

            ConsoleDisplay.PrintBacktestResult(result);
            break;

        // ── 9. Export to CSV ──────────────────────────────────────────────
        case "9":
            if (portfolio.TradeCount == 0)
            {
                ConsoleDisplay.PrintError("No trades to export yet.");
                break;
            }

            string csvPath = $"trades_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            ConsoleDisplay.ExportToCsv(portfolio.TradeHistory, csvPath);
            break;

        // ── S. Strategy Simulation ────────────────────────────────────────
        case "s":
        case "S":
            await RunSimulationAsync(marketData);
            break;

        // ── 0. Save & Exit ────────────────────────────────────────────────
        case "0":
            portfolio.SaveToFile();
            ConsoleDisplay.PrintSuccess("Goodbye!");
            running = false;
            break;

        default:
            ConsoleDisplay.PrintError("Invalid choice — enter a number 0–9.");
            break;
    }
}

// ─── Local Helper ─────────────────────────────────────────────────────────────

// Fetches live prices for every symbol in the portfolio
// Returns a Dictionary<symbol, currentPrice> for P&L calculations
async Task<Dictionary<string, decimal>> FetchCurrentPricesAsync()
{
    var prices = new Dictionary<string, decimal>();

    foreach (string sym in portfolio.Positions.Keys)
    {
        Stock? s = await marketData.GetQuoteAsync(sym);
        if (s != null) prices[sym] = s.CurrentPrice;
    }

    return prices;
}

// ─── Strategy Simulation ─────────────────────────────────────────────────────

async Task RunSimulationAsync(MarketDataService marketData)
{
    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("              STRATEGY SIMULATION MODE                  ");
    Console.WriteLine("═══════════════════════════════════════════════════════════");

    string[] symbols = { "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA" };
    int[] daysOfData = { 100, 200 };
    var allResults = new List<(string symbol, int days, BacktestResult result)>();

    foreach (int days in daysOfData)
    {
        Console.WriteLine($"\n  Fetching {days} days of data...");
        
        foreach (string symbol in symbols)
        {
            Console.Write($"    {symbol}... ");
            List<decimal> prices = await marketData.GetHistoricalPricesAsync(symbol, days);
            
            if (prices.Count < 51)
            {
                Console.WriteLine($"SKIPPED (only {prices.Count} days available)");
                await Task.Delay(12000);
                continue;
            }

            var result = strategy.Backtest(symbol, prices, startingCapital: 10_000m);
            allResults.Add((symbol, days, result));
            
            Console.WriteLine($"Return: {result.ReturnPercent:+0.00}% | Sharpe: {result.SharpeRatio:F2} | Win Rate: {result.WinRate:F0}%");
            
            await Task.Delay(12000);
        }
    }

    SimulationDisplay.PrintSimulationSummary(allResults);
}

// ─── Simulation Summary Display ──────────────────────────────────────────────

static class SimulationDisplay
{
    public static void PrintSimulationSummary(List<(string symbol, int days, BacktestResult result)> results)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("              SIMULATION RESULTS SUMMARY                  ");
        Console.WriteLine("═══════════════════════════════════════════════════════════");

        foreach (var group in results.GroupBy(r => r.days).OrderBy(g => g.Key))
        {
            Console.WriteLine($"\n  ── {group.Key} Days of Data ──");
            Console.WriteLine($"  {"Symbol",-8} {"Return",10} {"Sharpe",8} {"WinRate",8} {"Trades",7}");
            Console.WriteLine($"  {"------",-8} {"------",10} {"------",8} {"------",8} {"------",7}");

            var best = group.OrderByDescending(r => r.result.SharpeRatio).FirstOrDefault();
            
            foreach (var (symbol, days, result) in group.OrderByDescending(r => r.result.SharpeRatio))
            {
                bool isBest = result.SharpeRatio == best.result.SharpeRatio && result.SharpeRatio > 0;
                string highlight = isBest ? " ★" : "";
                
                Console.ForegroundColor = result.ReturnPercent >= 0 
                    ? ConsoleColor.Green 
                    : ConsoleColor.Red;
                Console.WriteLine($"  {symbol,-8} {result.ReturnPercent,+9:F2}% {result.SharpeRatio,7:F2} {result.WinRate,7:F0}% {result.TotalTrades,6}{highlight}");
            }
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  ★ = Best Sharpe Ratio in group                          ");
        Console.WriteLine("  Sharpe > 1.0 = Good | > 2.0 = Excellent | < 0 = Losing");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }
}