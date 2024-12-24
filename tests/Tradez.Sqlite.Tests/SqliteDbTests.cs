/* SPDX-FileCopyrightText: 2022 Christian Günther <cgite@gmx.de>
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using FluentAssertions;
using IbFlexReader.Contracts.Ib;
using IbFlexReader.Contracts;
using IbFlexReader;
using Tradez.Sqlite.Tests.secret;
using System;

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

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Can_Save_Kmi()
        {
            var kmi = FlexQuery.FromFile(@"..\..\..\testfiles\kmi.xml");

            kmi.Should().NotBeNull();
            var db = new Database(TestDbPath);
            var result = db.SaveFlexStatements(kmi);

            result.Trades.Total.Should().Be(3);
            result.Trades.New.Should().Be(3);
        }

        [Test]
        public void Can_Get_Trades_From_Db()
        {
            var db = new Database(TestDbPath);
            db.Should().NotBeNull();

            Can_Save_Kmi();
            db.Trades().ToList()[0].Symbol.Should().Be("KMI");
            db.Trades().Count().Should().Be(3);
        }

        [Test]
        public async Task Can_Get_Statements_From_ApiAsync()
        {
            // Use with your credentials and with caution
            // espacially if it's your live depot
            // https://www.interactivebrokers.com/campus/ibkr-api-page/flex-web-service/
            // https://www.ibkrguides.com/clientportal/performanceandstatements/flex-web-service.htm
            var statements = await FlexQuery.FromApiAsync(Apikey.IbToken, Apikey.QueryIdOpti,
                @$"..\..\..\backup\{DateTime.Now.ToShortDateString()} Backup.xml");
            statements.Should().NotBeNull();    
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
    }
}