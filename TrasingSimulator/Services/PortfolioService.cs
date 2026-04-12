using TrasingSimulator.Models;

namespace TrasingSimulator.Services
{
    public class PortfolioService
    {
        // ─── Fields ──────────────────────────────────────────────────────────

        private decimal _cashBalance;
        private readonly decimal _startingBalance;

        // Key = stock symbol (e.g. "AAPL"), Value = the position you hold
        private readonly Dictionary<string, Position> _positions
            = new Dictionary<string, Position>();

        // Every buy and sell ever made — never deleted, append-only
        private readonly List<Trade> _tradeHistory
            = new List<Trade>();

        // The file path where the portfolio is saved between sessions
        private const string SAVE_FILE = "portfolio.json";

        // ─── Constructor ─────────────────────────────────────────────────────

        public PortfolioService(decimal startingBalance = 10_000m)
        {
            _startingBalance = startingBalance;
            _cashBalance     = startingBalance;
        }

        // ─── Read-only Public Properties ─────────────────────────────────────

        // Outside code can READ these but never directly SET them
        public decimal CashBalance   => _cashBalance;
        public int     PositionCount => _positions.Count;
        public int     TradeCount    => _tradeHistory.Count;

        // Returns a copy of positions — callers cannot mutate the dictionary
        public IReadOnlyDictionary<string, Position> Positions => _positions;

        // Returns a copy of trades — callers cannot add/remove trades
        public IReadOnlyList<Trade> TradeHistory => _tradeHistory;

        // ─── Core Trading Methods ─────────────────────────────────────────────

        public bool Buy(string symbol, int shares, decimal currentPrice)
        {
            // ── Validation ──
            if (shares <= 0)
            {
                PrintError("Number of shares must be greater than zero.");
                return false;
            }

            decimal totalCost = shares * currentPrice;

            if (totalCost > _cashBalance)
            {
                PrintError($"Insufficient funds. " +
                           $"Need ${totalCost:F2} but only have ${_cashBalance:F2}.");
                return false;
            }

            // ── Execute the buy ──
            _cashBalance -= totalCost;

            if (_positions.ContainsKey(symbol))
            {
                // Already own this stock — add to existing position
                // Position.AddShares() recalculates the weighted average
                _positions[symbol].AddShares(shares, currentPrice);
            }
            else
            {
                // First purchase of this stock — open a brand new position
                _positions[symbol] = new Position(symbol, shares, currentPrice);
            }

            // Record the trade permanently
            var trade = new Trade(symbol, TradeType.Buy, shares, currentPrice);
            _tradeHistory.Add(trade);

            PrintSuccess($"Bought {shares} share(s) of {symbol} " +
                         $"at ${currentPrice:F2} — Total: ${totalCost:F2}");
            return true;
        }

        public bool Sell(string symbol, int shares, decimal currentPrice)
        {
            // ── Validation ──
            if (shares <= 0)
            {
                PrintError("Number of shares must be greater than zero.");
                return false;
            }

            if (!_positions.ContainsKey(symbol))
            {
                PrintError($"You don't own any shares of {symbol}.");
                return false;
            }

            Position pos = _positions[symbol];

            if (shares > pos.Shares)
            {
                PrintError($"Cannot sell {shares} shares — " +
                           $"you only own {pos.Shares}.");
                return false;
            }

            // ── Calculate realized P&L before modifying the position ──
            decimal costBasis    = shares * pos.AverageBuyPrice;
            decimal saleProceeds = shares * currentPrice;
            decimal realizedPnL  = saleProceeds - costBasis;

            // ── Execute the sell ──
            _cashBalance += saleProceeds;

            try
            {
                pos.RemoveShares(shares, currentPrice);
            }
            catch (InvalidOperationException ex)
            {
                // RemoveShares() is defensive — this shouldn't happen given the
                // check above, but we handle it gracefully just in case
                PrintError($"Unexpected error: {ex.Message}");
                _cashBalance -= saleProceeds; // roll back the cash change
                return false;
            }

            // If the position is now empty, remove it from the dictionary
            if (pos.Shares == 0)
                _positions.Remove(symbol);

            // Record the trade
            var trade = new Trade(symbol, TradeType.Sell, shares, currentPrice);
            _tradeHistory.Add(trade);

            // Show realized P&L with color feedback
            string pnlLabel = realizedPnL >= 0
                ? $"+${realizedPnL:F2} profit"
                : $"-${Math.Abs(realizedPnL):F2} loss";

            PrintSuccess($"Sold {shares} share(s) of {symbol} " +
                         $"at ${currentPrice:F2} — {pnlLabel}");
            return true;
        }

        // ─── Portfolio Metrics ────────────────────────────────────────────────

        // Total value of all shares at current market prices
        public decimal GetPortfolioMarketValue(
            Dictionary<string, decimal> currentPrices)
        {
            decimal total = 0m;

            foreach (var kvp in _positions)
            {
                string symbol = kvp.Key;

                if (currentPrices.TryGetValue(symbol, out decimal price))
                    total += kvp.Value.MarketValue(price);
            }

            return total;
        }

        // Cash + value of all holdings
        public decimal GetTotalAccountValue(
            Dictionary<string, decimal> currentPrices)
        {
            return _cashBalance + GetPortfolioMarketValue(currentPrices);
        }

        // Total unrealized P&L across all open positions
        public decimal GetTotalUnrealizedPnL(
            Dictionary<string, decimal> currentPrices)
        {
            decimal total = 0m;

            foreach (var kvp in _positions)
            {
                string symbol = kvp.Key;

                if (currentPrices.TryGetValue(symbol, out decimal price))
                    total += kvp.Value.UnrealizedPnl(price);
            }

            return total;
        }

        // Overall account return since inception, as a percentage
        public decimal GetOverallReturnPercent(
            Dictionary<string, decimal> currentPrices)
        {
            decimal currentTotal = GetTotalAccountValue(currentPrices);
            return ((currentTotal - _startingBalance) / _startingBalance) * 100;
        }

        // Total cash ever realized from closed sell trades
        public decimal GetTotalRealizedPnL()
        {
            decimal realized = 0m;

            foreach (Trade t in _tradeHistory)
            {
                if (t.Type == TradeType.Sell)
                    realized += t.TotalValue;
                else
                    realized -= t.TotalValue;
            }

            // This gives net cash flow from trading, not counting current holdings
            return realized;
        }

        // ─── Display Methods ──────────────────────────────────────────────────

        public void PrintSummary(Dictionary<string, decimal> currentPrices)
        {
            decimal marketValue  = GetPortfolioMarketValue(currentPrices);
            decimal totalValue   = GetTotalAccountValue(currentPrices);
            decimal unrealizedPnL = GetTotalUnrealizedPnL(currentPrices);
            decimal returnPct    = GetOverallReturnPercent(currentPrices);

            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════════");
            Console.WriteLine("              PORTFOLIO SUMMARY             ");
            Console.WriteLine("════════════════════════════════════════════");
            Console.WriteLine($"  Starting Balance : ${_startingBalance,10:F2}");
            Console.WriteLine($"  Cash Available   : ${_cashBalance,10:F2}");
            Console.WriteLine($"  Holdings Value   : ${marketValue,10:F2}");
            Console.WriteLine($"  Total Value      : ${totalValue,10:F2}");

            // Color the P&L line green or red
            Console.Write("  Unrealized P&L   : ");
            Console.ForegroundColor = unrealizedPnL >= 0
                ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"${unrealizedPnL,10:F2}");
            Console.ResetColor();

            Console.Write("  Overall Return   : ");
            Console.ForegroundColor = returnPct >= 0
                ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{returnPct,9:F2}%");
            Console.ResetColor();

            Console.WriteLine("════════════════════════════════════════════");
        }

        public void PrintPositions(Dictionary<string, decimal> currentPrices)
        {
            if (_positions.Count == 0)
            {
                Console.WriteLine("  No open positions.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"  {"Symbol",-8} {"Shares",7} {"Avg Cost",10} " +
                               $"{"Current",10} {"P&L $",10} {"P&L %",8}");
            Console.WriteLine(new string('─', 60));

            foreach (var kvp in _positions)
            {
                Position pos = kvp.Value;

                if (!currentPrices.TryGetValue(kvp.Key, out decimal price))
                    continue;

                decimal pnl    = pos.UnrealizedPnl(price);
                decimal pnlPct = pos.PnlPercent(price);

                Console.ForegroundColor = pnl >= 0
                    ? ConsoleColor.Green : ConsoleColor.Red;

                Console.WriteLine($"  {pos.Symbol,-8} {pos.Shares,7} " +
                                  $"${pos.AverageBuyPrice,9:F2} " +
                                  $"${price,9:F2} " +
                                  $"${pnl,9:F2} " +
                                  $"{pnlPct,7:F2}%");
            }

            Console.ResetColor();
        }

        public void PrintTradeHistory()
        {
            if (_tradeHistory.Count == 0)
            {
                Console.WriteLine("  No trades yet.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"  {"Date",-17} {"Type",-5} {"Symbol",-8} " +
                               $"{"Shares",7} {"Price",10} {"Total",12}");
            Console.WriteLine(new string('─', 65));

            foreach (Trade t in _tradeHistory)
            {
                Console.ForegroundColor = t.Type == TradeType.Buy
                    ? ConsoleColor.Green : ConsoleColor.Red;

                Console.WriteLine(t.ToString());
            }

            Console.ResetColor();
        }

        // ─── Persistence — Save & Load ────────────────────────────────────────

        public void SaveToFile()
        {
            // Serialize portfolio state to JSON and write to disk
            var data = new PortfolioSnapshot
            {
                CashBalance   = _cashBalance,
                Positions     = _positions.Values.ToList(),
                TradeHistory  = _tradeHistory
            };

            string json = System.Text.Json.JsonSerializer.Serialize(
                data,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(SAVE_FILE, json);
            Console.WriteLine($"  Portfolio saved to {SAVE_FILE}");
        }

        public void LoadFromFile()
        {
            if (!File.Exists(SAVE_FILE))
            {
                Console.WriteLine("  No saved portfolio found — starting fresh.");
                return;
            }

            try
            {
                string json = File.ReadAllText(SAVE_FILE);
                var data = System.Text.Json.JsonSerializer
                    .Deserialize<PortfolioSnapshot>(json);

                if (data == null) return;

                _cashBalance = data.CashBalance;

                _positions.Clear();
                foreach (Position p in data.Positions)
                    _positions[p.Symbol] = p;

                _tradeHistory.Clear();
                _tradeHistory.AddRange(data.TradeHistory);

                Console.WriteLine($"  Portfolio loaded — " +
                                  $"{_positions.Count} positions, " +
                                  $"{_tradeHistory.Count} trades.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed to load portfolio: {ex.Message}");
            }
        }

        // ─── Private Helpers ──────────────────────────────────────────────────

        private void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {message}");
            Console.ResetColor();
        }

        private void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {message}");
            Console.ResetColor();
        }
    }

    // ─── Helper class for JSON serialization ─────────────────────────────────
    // This is a plain data container — no logic, just properties for saving state

    public class PortfolioSnapshot
    {
        public decimal        CashBalance  { get; set; }
        public List<Position> Positions    { get; set; } = new();
        public List<Trade>    TradeHistory { get; set; } = new();
    }
}