/* SPDX-FileCopyrightText: 2022 Christian Günther <cgite@gmx.de>
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using FluentAssertions;
using IbFlexReader.Contracts.Ib;
using IbFlexReader.Contracts;
using IbFlexReader;

namespace Tradez.Sqlite.Tests
{
    public class SqliteDbTests
    {
        private const string TestDbPath = @"..\..\..\testfiles\sqlite.db";

        [SetUp]
        public void Setup()
        {
            System.IO.File.Delete(TestDbPath);
            Can_Create_Tables();
        }

        [TearDown]
        public void TearDown()
        {
            
        }

        [Test]
        public void Can_Create_Tables()
        {
            Database.CreateDatabase(TestDbPath);
            Database.CreateTables(TestDbPath);
        }

        [Test]
        public void Can_Save_Kmi()
        {
            var kmi = GetKMIFromXml();

            kmi.Should().NotBeNull();
            var db = new Database(TestDbPath);
            var result = db.SaveFlexStatements(kmi);

            result.Trades.Total.Should().Be(3);
            result.Trades.New.Should().Be(3);

        }

        [Test]
        public void Can_Get_Trades()
        {
            var db = new Database(TestDbPath);
            db.Should().NotBeNull();

            Can_Save_Kmi();
            db.Trades().ToList()[0].Symbol.Should().Be("KMI");
            db.Trades().Count().Should().Be(3);
        }

        [Test]
        public void Can_Drop_Tables()
        {
            Database.DropTables(TestDbPath);
            Database.TableCount(TestDbPath).Should().Be(0);
        }

        /// <summary>
        /// Create a table with library for handtuning
        /// </summary>
        [Test]
        public void Can_Create_Single_Table()
        {
            var db = new Database(TestDbPath);
            db.CreateTable<CashReportCurrency>();
            db.CreateTable<FxTransaction>();
            db.CreateTable<Transfer>();
        }

        public static FlexStatements GetKMIFromXml()
        {
            var opt = new Options();
            opt.UseXmlReader = true;
            var response = new Reader().
                GetByString(@"..\..\..\testfiles\kmi.xml",
                opt);
            return response.FlexStatements;
        }
    }
}