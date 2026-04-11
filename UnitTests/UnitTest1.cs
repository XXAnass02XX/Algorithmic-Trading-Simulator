using TrasingSimulator.Models;
using NUnit.Framework;

namespace UnitTests;

public class Tests
{
    public string APPLE = "apple";
    public string APL = "APL";
    public Stock AppleStock;
    public Position MyPosition;

    [SetUp]
    public void Setup()
    {
        AppleStock = new Stock(APL, APPLE, 9, 10, 1);
        MyPosition = new Position(APPLE, 0, 0); //init an empty portfolio
    }

    [Test]
    public void Test1()
    {
        MyPosition.AddShares(10, 9);
        MyPosition.AddShares(10, 10);
        Assert.That(MyPosition.AverageBuyPrice, Is.EqualTo(9.5));
        Assert.That(MyPosition.UnrealizedPnl(11), Is.EqualTo(30));
        Assert.That(MyPosition.UnrealizedPnl(10), Is.EqualTo(10));        
        Assert.That(MyPosition.IsProfit(10), Is.EqualTo(true));
        Assert.That(MyPosition.IsProfit(8), Is.EqualTo(false));
        MyPosition.RemoveShares(10, 11); // we will have total invested -= 10*AVGPrice
        Assert.That(MyPosition.IsProfit(11), Is.EqualTo(true));
        Assert.That(MyPosition.AverageBuyPrice, Is.EqualTo(9.5));
        Assert.That(MyPosition.UnrealizedPnl(11), Is.EqualTo(30));
    }
}