namespace TrasingSimulator.Models;

public class Position
{
    #region Properties
    public string Symbol { get; set; }
    public int Shares { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal TotalInvested { get; set; }
    public DateTime OpenedAt       { get; }
    #endregion
    #region Methods

    public Position(string symbol, int shares,decimal buyPrice)
    {
        Symbol = symbol;
        Shares = shares;
        AverageBuyPrice = buyPrice;
        TotalInvested = shares * buyPrice;
        OpenedAt        = DateTime.Now;
    }
    public void AddShares(int newShares, decimal newPrice)
    {
        AverageBuyPrice = ((Shares * AverageBuyPrice) + (newShares * newPrice))
                          / (Shares + newShares);
        Shares += newShares;
        TotalInvested += newPrice*newShares;
    }
    public void RemoveShares(int sharesToSell, decimal currentPrice)
    {
        if (sharesToSell > Shares)
            throw new InvalidOperationException(
                $"Cannot sell {sharesToSell} shares — only {Shares} held.");

        // Selling doesn't change your average buy price — only buying does
        TotalInvested -= sharesToSell * currentPrice;
        Shares        -= sharesToSell;
    }
    public decimal UnrealizedPnl(decimal currentPrice)
    {
        return MarketValue(currentPrice) - TotalInvested; // avgPrice = Sum(prices)/totalInv
    }

    public decimal PnlPercent(decimal currentPrice)
    {
        if (TotalInvested == 0) return 0;
        return (UnrealizedPnl(currentPrice) / TotalInvested) * 100;
    }

    public decimal MarketValue(decimal currentPrice)
    {
        return Shares * currentPrice;
    }
    public bool IsProfit(decimal currentPrice)
    {
        return UnrealizedPnl(currentPrice) >= 0;
    }
    public string ToDisplayRow(decimal currentPrice)
    {
        decimal pnl     = UnrealizedPnl(currentPrice);
        decimal pnlPct  = PnlPercent(currentPrice);
        string  arrow   = pnl >= 0 ? "▲" : "▼";

        return $"{Symbol,-6}  " +
               $"{Shares,6} shares  " +
               $"avg ${AverageBuyPrice,8:F2}  " +
               $"now ${currentPrice,8:F2}  " +
               $"{arrow} ${Math.Abs(pnl),9:F2}  " +
               $"({pnlPct:+0.00;-0.00}%)";
    }
    #endregion
}