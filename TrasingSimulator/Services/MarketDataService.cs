using System.Text.Json;
using TrasingSimulator.Models;

namespace TrasingSimulator.Services;

public class MarketDataService
{
    #region Properties
    private HttpClient _httpClient;
    private string _apiKey;
    private const string BASE_URL = "https://www.alphavantage.co/query";
    private readonly Dictionary<string, (Stock stock, DateTime fetchedAt)> _cache
        = new Dictionary<string, (Stock, DateTime)>();
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(1);
    #endregion
    
    #region Constructor
    public MarketDataService(string apiKey)
    {
        _apiKey     = apiKey;
        _httpClient = new HttpClient();

        // Tell the server who we are — some APIs require a User-Agent header
        _httpClient.DefaultRequestHeaders.Add(
            "User-Agent", "TradingSimulator/1.0");
    }
    #endregion
    
    #region Methods

    public async Task<Stock?> GetQuoteAsync(string symbol)
    {
        symbol = symbol.ToUpper().Trim();
        if (_cache.ContainsKey(symbol))
        {
            return _cache[symbol].stock;
        }
        string url = $"{BASE_URL}?function=GLOBAL_QUOTE" +
                     $"&symbol={symbol}&apikey={_apiKey}";
        try
        {
            string json = await _httpClient.GetStringAsync(url);
            Stock? stock = ParseQuote(json, symbol);
            if (stock != null) _cache[symbol] = (stock, DateTime.Now);
            return stock;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error fetching {symbol}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return null;
        }
    }
    public async Task<List<decimal>> GetHistoricalPricesAsync(
        string symbol, int days = 60)
    {
        symbol = symbol.ToUpper().Trim();

        string url = $"{BASE_URL}?function=TIME_SERIES_DAILY" +
                     $"&symbol={symbol}&apikey={_apiKey}";
        try
        {
            string json = await _httpClient.GetStringAsync(url);
            return ParseHistoricalPrices(json, days);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
            return new List<decimal>();   // return empty list, not a crash
        }
    }
    public async Task<List<Stock>> GetMultipleQuotesAsync(
        IEnumerable<string> symbols)
    {
        var results = new List<Stock>();

        foreach (string symbol in symbols)
        {
            Stock? stock = await GetQuoteAsync(symbol);

            if (stock != null)
                results.Add(stock);

            // Free API: 5 requests per minute max — wait between calls
            await Task.Delay(TimeSpan.FromSeconds(12));
        }

        return results;
    }
    
    private Stock? ParseQuote(string json, string symbol)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                // Navigate the JSON tree: root → "Global Quote" → fields
                if (!root.TryGetProperty("Global Quote", out JsonElement quote))
                {
                    Console.WriteLine($"Symbol '{symbol}' not found or invalid.");
                    return null;
                }

                // Each field is a string in the API — we parse them to numbers
                decimal currentPrice  = ParseDecimal(quote, "05. price");
                decimal previousClose = ParseDecimal(quote, "08. previous close");
                long    volume        = ParseLong   (quote, "06. volume");

                // Company name not available in this endpoint — use symbol as fallback
                return new Stock(
                    symbol:        symbol,
                    companyName:   symbol,
                    currentPrice:  currentPrice,
                    previousClose: previousClose,
                    volume:        volume
                );
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse JSON for {symbol}: {ex.Message}");
                return null;
            }
        }

        private List<decimal> ParseHistoricalPrices(string json, int days)
        {
            var prices = new List<decimal>();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("Time Series (Daily)", out JsonElement series))
                    return prices;

                // The API returns dates newest-first — we take the last `days` entries
                int count = 0;
                foreach (JsonProperty day in series.EnumerateObject())
                {
                    if (count >= days) break;

                    JsonElement dayData = day.Value;
                    decimal close = ParseDecimal(dayData, "4. close");
                    prices.Add(close);
                    count++;
                }

                // Reverse so index 0 = oldest, last index = most recent
                // This is what StrategyEngine expects for its sliding window
                prices.Reverse();
                return prices;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse historical data: {ex.Message}");
                return prices;
            }
        }
        
        private decimal ParseDecimal(JsonElement element, string key)
        {
            string raw = element.GetProperty(key).GetString() ?? "0";

            // Some fields have a trailing '%' — strip it before parsing
            raw = raw.Replace("%", "").Trim();

            return decimal.TryParse(raw, out decimal result) ? result : 0m;
        }

        private long ParseLong(JsonElement element, string key)
        {
            string raw = element.GetProperty(key).GetString() ?? "0";
            return long.TryParse(raw, out long result) ? result : 0L;
        }

        // Releases the HttpClient when the service is no longer needed
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    #endregion
}