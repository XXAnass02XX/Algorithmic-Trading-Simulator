using TrasingSimulator.Models;

var apple = new Stock(
    symbol:        "AAPL",
    companyName:   "Apple Inc.",
    currentPrice:  189.50m,
    previousClose: 185.20m,
    volume:        52_000_000
);

Console.WriteLine(apple);

Console.WriteLine($"Change today: ${apple.PriceChange():F2}");