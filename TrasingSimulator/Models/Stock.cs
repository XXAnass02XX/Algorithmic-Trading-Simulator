namespace TrasingSimulator.Models;

public class Stock
{
    public string Symbol { get; set; }
    public string CompanyName { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public long Volume { get; set; }
    public DateTime LastUpdated { get; set; }

    #region Methods

    public Stock(string symbol, string companyName, decimal currentPrice, decimal previousClose, long volume)
    {
        Symbol = symbol;
        CompanyName = companyName;
        CurrentPrice = currentPrice;
        PreviousClose = previousClose;
        Volume = volume;
        LastUpdated = DateTime.Now;
    }
    public decimal PriceChangePercent()
    {
        if (PreviousClose == 0) return 0;
        return ((CurrentPrice - PreviousClose) / PreviousClose) * 100;
    }
    public decimal PriceChange()
    {
        return CurrentPrice - PreviousClose;
    }
    public override string ToString()
    {
        decimal change  = PriceChange();
        decimal changePct = PriceChangePercent();
        string  arrow   = change >= 0 ? "▲" : "▼";

        return $"[{Symbol,-6}] {CompanyName,-20} " +
               $"${CurrentPrice,8:F2}  " +
               $"{arrow} {Math.Abs(changePct),5:F2}%  " +
               $"Vol: {Volume:N0}";
    }
    #endregion
}