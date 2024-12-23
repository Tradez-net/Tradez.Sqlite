using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Tradez.Lib;
using Tradez.Sqlite;

namespace Tradez.Tests
{
    public class SqliteDbTests
    {
        private const string DbPath = @"C:\Users\cguenther\Nextcloud\Projekte\Tradez\src\Tradez.Tests\test\sqlite.db";
        
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CreateTablesOrm()
        {
            Database.CreateDatabase(DbPath);

        }
        [Test]
        public void SaveKmi()
        {
            var kmi = Database.GetKMI();

            Assert.IsNotNull(kmi);
            var result = kmi.SaveToDb(DbPath);

            Assert.AreEqual(3, result.Trades.Total);
            Assert.AreEqual(3, result.Trades.New);

        }
        
        [Test]
        public void SaveCompleted()
        {
            var kmi = Database.GetKMI();

            Assert.IsNotNull(kmi);
            var trades = TradeGrouping.CreateCompletedTrades(kmi.FlexStatement[0].Trades.Trade
                .Select(t => new Trade(t)));
            Assert.AreEqual(2, trades.Count);
            var db = new Database(DbPath);
            var res = db.InsertCompletedTrade(trades.Select(ct => new Sqlite.CompletedTrade()
            {
                OpenTradeId = ct.OpenOrder.TradeId,
                CloseTradeId = ct.CloseOrder.TradeId,
                Quantity = ct.OpenOrder.Quantity
            }));
            Assert.That(res, Is.EqualTo(2));
        }
        
        [Test]
        public void SaveAll()
        {
            var kmi = Database.GetAll();

            Assert.IsNotNull(kmi);
            var result = kmi.SaveToDb(DbPath);

            Assert.AreEqual(211, result.Trades.Total);
            Assert.AreEqual(211, result.Trades.New);

        }

        [Test]
        public void GetTrades()
        {
            var db = new Database(DbPath);

            Assert.IsNotNull(db);
            
            Assert.AreEqual("KMI", db.Trades().ToList()[0].Symbol );
            Assert.AreEqual(3, db.Trades().Count());
            
        }

        [Test]
        public void DropTables()
        {
            Database.DropTables(DbPath);
            Assert.AreEqual(0,Database.TableCount(DbPath));
        }

        [Test]
        public void CreateTables()
        {
            Database.CreateTables(DbPath);
            Assert.AreEqual(2, Database.TableCount(DbPath));
        }

        [Test]
        public async Task SaveFlexToDb()
        {
            var db = new Database(DbPath);
            var res = await db.SaveFlex("51354463773021236101084", "581772", null);
            Assert.Greater(res.Trades.New, 0);
        }

    }
}