namespace TrasingSimulator.Models;

public enum TradeType
{
    Buy = 1,
    Sell = 2
}
public class Trade
{
    #region Properties
    public Guid Id { get;}
    public string Symbol { get;}
    public TradeType Type { get;}
    public int Shares { get;}
    public decimal PricePerShare { get;}
    public DateTime TimeStamp { get;}
    public decimal TotalValue => Shares * PricePerShare;
    #endregion

    public Trade(string symbol, TradeType type, int shares, decimal price)
    {
        Id = Guid.NewGuid();
        Symbol = symbol;
        Type = type;
        Shares = shares;
        PricePerShare = price;
        TimeStamp = DateTime.Now;
    }

    #region Methods

    public override string ToString()
    {
        string typeLabel = Type == TradeType.Buy ? "BUY " : "SELL";

        return $"{TimeStamp:yyyy-MM-dd HH:mm}  " +
               $"{typeLabel}  " +
               $"{Symbol,-6}  " +
               $"{Shares,6} shares @ " +
               $"${PricePerShare,8:F2}  " +
               $"= ${TotalValue,10:F2}";
    }
    // Exports the trade as a CSV row
    public string ToCsvRow()
    {
        return $"{Id},{TimeStamp:O},{Symbol},{Type},{Shares}," +
               $"{PricePerShare:F2},{TotalValue:F2}";
    }

    // Returns a short summary used in console notifications
    public string ToShortSummary()
    {
        string action = Type == TradeType.Buy ? "Bought" : "Sold";
        return $"{action} {Shares} share(s) of {Symbol} at ${PricePerShare:F2}";
    }
    #endregion
}