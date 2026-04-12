using TrasingSimulator.Models;

namespace TrasingSimulator.Services
{
    public enum Signal
    {
        Buy,   // short MA crossed above long MA — upward momentum
        Sell,  // short MA crossed below long MA — downward momentum
        Hold   // no crossover detected — stay put
    }
    // Holds the results of a backtest run — how well did the strategy do?
    public class BacktestResult
    {
        public string   Symbol          { get; set; } = "";
        public decimal  StartingCapital { get; set; }
        public decimal  FinalCapital    { get; set; }
        public decimal  TotalReturn     { get; set; }   // in dollars
        public decimal  ReturnPercent   { get; set; }   // as %
        public decimal  SharpeRatio     { get; set; }   // risk-adjusted return
        public int      TotalTrades     { get; set; }   // buy + sell signals acted on
        public int      WinningTrades   { get; set; }   // trades closed at a profit
        public int      LosingTrades    { get; set; }   // trades closed at a loss
        public decimal  WinRate         { get; set; }   // WinningTrades / TotalTrades %
        public DateTime TestedFrom      { get; set; }
        public DateTime TestedTo        { get; set; }
    }

    public class StrategyEngine
    {
        // ─── Fields ──────────────────────────────────────────────────────────

        private readonly int _shortPeriod;  // default 10-day moving average
        private readonly int _longPeriod;   // default 50-day moving average

        // ─── Constructor ─────────────────────────────────────────────────────

        public StrategyEngine(int shortPeriod = 10, int longPeriod = 50)
        {
            if (shortPeriod >= longPeriod)
                throw new ArgumentException(
                    "Short period must be less than long period.");

            _shortPeriod = shortPeriod;
            _longPeriod  = longPeriod;
        }

        // ─── Core Algorithm ───────────────────────────────────────────────────

        // Calculates the simple moving average of the last `period` prices
        // e.g. period=10 → average of the 10 most recent closing prices
        public decimal MovingAverage(List<decimal> prices, int period)
        {
            if (prices.Count < period)
                return 0m;  // not enough data yet

            // TakeLast(period) gets the N most recent prices
            // .Average() is a LINQ method that sums and divides for you
            return prices.TakeLast(period).Average();
        }

        // The heart of the strategy — compares short MA vs long MA
        // Requires at least `_longPeriod` data points to produce a signal
        public Signal GetSignal(List<decimal> prices)
        {
            if (prices.Count < _longPeriod)
            {
                Console.WriteLine($"  Not enough data. Need {_longPeriod} " +
                                  $"days, only have {prices.Count}.");
                return Signal.Hold;
            }

            decimal shortMA = MovingAverage(prices, _shortPeriod);
            decimal longMA  = MovingAverage(prices, _longPeriod);

            // Short MA above long MA = upward momentum = BUY
            // Short MA below long MA = downward momentum = SELL
            // Equal = no clear direction = HOLD
            if (shortMA > longMA) return Signal.Buy;
            if (shortMA < longMA) return Signal.Sell;
            return Signal.Hold;
        }

        // Returns both MA values for display purposes
        // Useful for showing the user the actual numbers behind the signal
        public (decimal shortMA, decimal longMA) GetMovingAverages(
            List<decimal> prices)
        {
            return (
                MovingAverage(prices, _shortPeriod),
                MovingAverage(prices, _longPeriod)
            );
        }

        // ─── Backtesting ──────────────────────────────────────────────────────

        // Simulates running the strategy on historical data day by day
        // Answers the question: "if I had used this algorithm in the past,
        // how much money would I have made or lost?"
        public BacktestResult Backtest(
            string symbol, List<decimal> prices, decimal startingCapital)
        {
            if (prices.Count < _longPeriod + 1)
            {
                Console.WriteLine("  Not enough historical data to backtest.");
                return new BacktestResult();
            }

            decimal cash        = startingCapital;
            int     shares      = 0;
            int     totalTrades = 0;
            int     wins        = 0;
            int     losses      = 0;
            decimal lastBuyPrice = 0m;

            // Track daily portfolio value for Sharpe Ratio calculation
            var dailyReturns = new List<decimal>();

            // Start from index _longPeriod so we always have enough data
            // for the long moving average window
            for (int i = _longPeriod; i < prices.Count; i++)
            {
                // Build the price window up to day i
                List<decimal> window = prices.Take(i + 1).ToList();

                decimal currentPrice = prices[i];
                Signal  signal       = GetSignal(window);

                decimal previousValue = cash + (shares * (i > 0 ? prices[i-1] : currentPrice));
                decimal currentValue  = cash + (shares * currentPrice);

                // Track daily return for Sharpe calculation
                if (previousValue > 0)
                    dailyReturns.Add((currentValue - previousValue) / previousValue);

                // Act on the signal
                if (signal == Signal.Buy && shares == 0 && cash > 0)
                {
                    // Buy as many shares as we can afford
                    shares       = (int)(cash / currentPrice);
                    lastBuyPrice = currentPrice;
                    cash        -= shares * currentPrice;
                    totalTrades++;
                }
                else if (signal == Signal.Sell && shares > 0)
                {
                    // Sell all shares
                    decimal proceeds = shares * currentPrice;
                    cash            += proceeds;

                    // Was this trade a winner or loser?
                    if (currentPrice > lastBuyPrice) wins++;
                    else                             losses++;

                    shares = 0;
                    totalTrades++;
                }
            }

            // Close any open position at the last price
            decimal finalPrice = prices.Last();
            if (shares > 0)
            {
                cash  += shares * finalPrice;
                shares = 0;
            }

            decimal totalReturn   = cash - startingCapital;
            decimal returnPercent = (totalReturn / startingCapital) * 100;
            decimal sharpe        = CalculateSharpeRatio(dailyReturns);
            decimal winRate       = totalTrades > 0
                                    ? ((decimal)wins / totalTrades) * 100
                                    : 0m;

            return new BacktestResult
            {
                Symbol          = symbol,
                StartingCapital = startingCapital,
                FinalCapital    = cash,
                TotalReturn     = totalReturn,
                ReturnPercent   = returnPercent,
                SharpeRatio     = sharpe,
                TotalTrades     = totalTrades,
                WinningTrades   = wins,
                LosingTrades    = losses,
                WinRate         = winRate,
                TestedFrom      = DateTime.Now.AddDays(-prices.Count),
                TestedTo        = DateTime.Now
            };
        }

        // ─── Private Helpers ──────────────────────────────────────────────────

        // Sharpe Ratio = (average daily return) / (std deviation of returns)
        // Measures risk-adjusted performance. Higher = better.
        // Above 1.0 = good, above 2.0 = very good, below 0 = losing strategy
        private decimal CalculateSharpeRatio(List<decimal> dailyReturns)
        {
            if (dailyReturns.Count < 2) return 0m;

            decimal avg    = dailyReturns.Average();
            decimal stdDev = StandardDeviation(dailyReturns);

            if (stdDev == 0) return 0m;

            // Annualize by multiplying by sqrt(252) — 252 trading days per year
            return (avg / stdDev) * (decimal)Math.Sqrt(252);
        }

        private decimal StandardDeviation(List<decimal> values)
        {
            decimal avg      = values.Average();
            decimal variance = values
                .Select(v => (v - avg) * (v - avg))
                .Average();

            return (decimal)Math.Sqrt((double)variance);
        }
    }
}