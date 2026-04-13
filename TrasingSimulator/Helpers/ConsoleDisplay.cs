using TrasingSimulator.Services;
using TrasingSimulator.Models;
using TrasingSimulator.Services;

namespace TrasingSimulator.Helpers
{
    public static class ConsoleDisplay
    {
        // ─── Banner ───────────────────────────────────────────────────────────

        public static void PrintWelcome()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════╗");
            Console.WriteLine("║        ALGORITHMIC TRADING SIMULATOR     ║");
            Console.WriteLine("║          Powered by Alpha Vantage        ║");
            Console.WriteLine("╚══════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        // ─── Menu ─────────────────────────────────────────────────────────────

        public static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("  ╔════════════════════════════════════════╗");
            Console.WriteLine("  ║              MAIN MENU                 ║");
            Console.WriteLine("  ╠════════════════════════════════════════╣");
            Console.WriteLine("  ║  1.  View portfolio summary           ║");
            Console.WriteLine("  ║  2.  View open positions              ║");
            Console.WriteLine("  ║  3.  Buy shares                        ║");
            Console.WriteLine("  ║  4.  Sell shares                       ║");
            Console.WriteLine("  ║  5.  View trade history                ║");
            Console.WriteLine("  ║  6.  Get stock quote                   ║");
            Console.WriteLine("  ║  7.  Run strategy signal               ║");
            Console.WriteLine("  ║  8.  Backtest strategy                 ║");
            Console.WriteLine("  ║  9.  Export trades to CSV              ║");
            Console.WriteLine("  ║  S.  Run strategy simulation           ║");
            Console.WriteLine("  ║  0.  Save & exit                       ║");
            Console.WriteLine("  ╚════════════════════════════════════════╝");
            Console.WriteLine();
            Console.Write("  Enter your choice: ");
        }

        // ─── Stock Quote ──────────────────────────────────────────────────────

        public static void PrintQuote(Stock stock)
        {
            decimal change    = stock.PriceChange();
            decimal changePct = stock.PriceChangePercent();
            bool    isUp      = change >= 0;

            Console.WriteLine();
            Console.WriteLine($"  ┌─ {stock.Symbol} ──────────────────────────┐");
            Console.WriteLine($"  │  Company   : {stock.CompanyName}");
            Console.Write    ($"  │  Price     : ${stock.CurrentPrice:F2}  ");

            Console.ForegroundColor = isUp ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{(isUp ? "▲" : "▼")} {Math.Abs(changePct):F2}%  " +
                              $"({(isUp ? "+" : "")}{change:F2})");
            Console.ResetColor();

            Console.WriteLine($"  │  Prev Close: ${stock.PreviousClose:F2}");
            Console.WriteLine($"  │  Volume    : {stock.Volume:N0}");
            Console.WriteLine($"  │  Updated   : {stock.LastUpdated:HH:mm:ss}");
            Console.WriteLine($"  └───────────────────────────────────────┘");
        }

        // ─── Signal ───────────────────────────────────────────────────────────

        public static void PrintSignal(
            Signal signal, string symbol,
            decimal shortMA, decimal longMA,
            decimal currentPrice)
        {
            Console.WriteLine();
            Console.WriteLine($"  ─── Strategy Signal for {symbol} ───");
            Console.WriteLine($"  Current Price : ${currentPrice:F2}");
            Console.WriteLine($"  Short MA ({10,2}-day): ${shortMA:F2}");
            Console.WriteLine($"  Long  MA ({50,2}-day): ${longMA:F2}");
            Console.Write    ($"  Signal        : ");

            switch (signal)
            {
                case Signal.Buy:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("▲ BUY  — Short MA crossed above Long MA");
                    break;
                case Signal.Sell:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("▼ SELL — Short MA crossed below Long MA");
                    break;
                case Signal.Hold:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("● HOLD — No crossover detected");
                    break;
            }

            Console.ResetColor();
        }

        // ─── Backtest Result ──────────────────────────────────────────────────

        public static void PrintBacktestResult(BacktestResult r)
        {
            bool isProfit = r.TotalReturn >= 0;

            Console.WriteLine();
            Console.WriteLine("  ════════════════════════════════════════");
            Console.WriteLine($"       BACKTEST RESULTS — {r.Symbol}");
            Console.WriteLine("  ════════════════════════════════════════");
            Console.WriteLine($"  Period         : " +
                              $"{r.TestedFrom:yyyy-MM-dd} → {r.TestedTo:yyyy-MM-dd}");
            Console.WriteLine($"  Starting Capital: ${r.StartingCapital:F2}");
            Console.WriteLine($"  Final Capital   : ${r.FinalCapital:F2}");

            Console.Write    ($"  Total Return    : ");
            Console.ForegroundColor = isProfit ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{(isProfit ? "+" : "")}${r.TotalReturn:F2} " +
                              $"({r.ReturnPercent:+0.00;-0.00}%)");
            Console.ResetColor();

            Console.Write    ($"  Sharpe Ratio    : ");
            Console.ForegroundColor = r.SharpeRatio >= 1.0m
                ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"{r.SharpeRatio:F2}");
            Console.ResetColor();

            Console.WriteLine($"  Total Trades    : {r.TotalTrades}");
            Console.WriteLine($"  Winning Trades  : {r.WinningTrades}");
            Console.WriteLine($"  Losing  Trades  : {r.LosingTrades}");

            Console.Write    ($"  Win Rate        : ");
            Console.ForegroundColor = r.WinRate >= 50m
                ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{r.WinRate:F1}%");
            Console.ResetColor();

            Console.WriteLine("  ════════════════════════════════════════");

            // Interpretation hint
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if      (r.SharpeRatio >= 2.0m) Console.WriteLine("  ★ Excellent risk-adjusted performance");
            else if (r.SharpeRatio >= 1.0m) Console.WriteLine("  ✓ Good risk-adjusted performance");
            else if (r.SharpeRatio >= 0.0m) Console.WriteLine("  ~ Marginal performance — consider tuning");
            else                             Console.WriteLine("  ✗ Poor performance — strategy losing money");
            Console.ResetColor();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        public static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {message}");
            Console.ResetColor();
        }

        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {message}");
            Console.ResetColor();
        }

        public static void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ℹ {message}");
            Console.ResetColor();
        }

        public static void PrintDivider()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  " + new string('─', 42));
            Console.ResetColor();
        }

        // Prompts the user and returns their trimmed input
        public static string PromptInput(string label)
        {
            Console.Write($"  {label}: ");
            return Console.ReadLine()?.Trim() ?? "";
        }

        // Prompts for an integer — loops until valid input is given
        public static int PromptInt(string label)
        {
            while (true)
            {
                Console.Write($"  {label}: ");
                string raw = Console.ReadLine() ?? "";

                if (int.TryParse(raw, out int value) && value > 0)
                    return value;

                PrintError("Please enter a valid positive number.");
            }
        }

        // Exports trade history to a CSV file the user can open in Excel
        public static void ExportToCsv(IReadOnlyList<Trade> trades, string filePath)
        {
            var lines = new List<string>
            {
                "TradeId,Timestamp,Symbol,Type,Shares,PricePerShare,TotalValue"
            };

            foreach (Trade t in trades)
                lines.Add(t.ToCsvRow());

            File.WriteAllLines(filePath, lines);
            PrintSuccess($"Exported {trades.Count} trades to {filePath}");
        }
    }
}