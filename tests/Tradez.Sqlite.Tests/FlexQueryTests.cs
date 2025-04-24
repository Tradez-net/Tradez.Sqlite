using FluentAssertions;
using Tradez.Sqlite;

namespace OptScanner.Tests
{
    [TestFixture]
    public class FlexQueryTests
    {
        [Test]
        public void CompletedTradesScenario()
        {

            // bsp2: buy1
            // buy 9   sell 10
            var trades = FlexQuery.FromFile(@"D:\Projekte\MeinDepot\src\OptScanner.Tests\test\kmi.xml").FlexStatement;
            var groupedTrades = trades.GroupBy(t => t.Description);
            groupedTrades.Should().HaveCount(1);
            var c = FlexQuery.MapTradesFifo(groupedTrades.ElementAtOrDefault(0));
            c.Should().HaveCount(2, "KMI");
            c[0].Buy.Quantity.Should().Be(100d);
            c[0].Sell.Quantity.Should().Be(-100);
            c[1].Buy.Quantity.Should().Be(100d);
            c[1].Sell.Quantity.Should().Be(-100);
            c[0].Result.Should().Be(129);
            c[1].Result.Should().Be(179);
            c[0].ResultBaseCur.Should().Be(124);
            c[1].ResultBaseCur.Should().Be(180.07d);

            // bsp1: buy 5   sell 3
            // sell 2
            trades = FlexQuery.FromFile(@"D:\Projekte\MeinDepot\src\OptScanner.Tests\test\gme.xml");
            groupedTrades = trades.GroupBy(t => t.Description);
            groupedTrades.Should().HaveCount(1);
            c = FlexQuery.MapTradesFifo(groupedTrades.ElementAtOrDefault(0));
            c.Should().HaveCount(2, "GME");
            c[0].Buy.Quantity.Should().Be(3d);
            c[0].Sell.Quantity.Should().Be(-3);
            c[1].Buy.Quantity.Should().Be(2d);
            c[1].Sell.Quantity.Should().Be(-2);
            c[0].Result.Should().Be(210.18d);
            c[1].Result.Should().Be(270);
            c[0].ResultBaseCur.Should().Be(169.75d);
            c[1].ResultBaseCur.Should().Be(228.94d);
        }
    }
}