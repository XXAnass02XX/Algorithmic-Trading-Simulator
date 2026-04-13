# Algorithmic Trading Simulator

A C# console application that simulates stock trading with real market data from Alpha Vantage.

## Features

- **Portfolio Management** — Track cash balance, open positions, and trade history
- **Market Data** — Real-time stock quotes via Alpha Vantage API
- **Moving Average Strategy** — Buy/sell signals based on short-term vs long-term moving averages
- **Backtesting** — Test the strategy against historical data with Sharpe ratio, win rate, and returns
- **Strategy Simulation** — Run the strategy across multiple stocks to compare performance
- **CSV Export** — Export trade history for analysis

## Setup

1. Get a free API key from [alphavantage.co](https://www.alphavantage.co/support/#api-key)
2. Add your API key in `TrasingSimulator/Program.cs:12`:
   ```csharp
   const string API_KEY = "YOUR_API_KEY_HERE";
   ```

## Build & Run

```powershell
dotnet build
dotnet run --project TrasingSimulator
```

## Run Tests

```powershell
dotnet test
```

## Menu Options

| # | Feature |
|---|---------|
| 1 | View portfolio summary |
| 2 | View open positions |
| 3 | Buy shares |
| 4 | Sell shares |
| 5 | View trade history |
| 6 | Get stock quote |
| 7 | Run strategy signal |
| 8 | Backtest strategy |
| 9 | Export trades to CSV |
| S | Run strategy simulation |
| 0 | Save & exit |

## Strategy

The app uses a **Moving Average Crossover** strategy:

- **Buy Signal**: Short MA crosses above long MA (upward momentum)
- **Sell Signal**: Short MA crosses below long MA (downward momentum)
- **Hold**: No crossover detected

Default periods: 10-day (short) and 50-day (long) moving averages.

## API Rate Limits

Alpha Vantage free tier allows **5 requests per minute**. The app includes automatic rate limiting between API calls.
