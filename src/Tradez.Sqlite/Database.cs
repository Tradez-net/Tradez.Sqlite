/* SPDX-FileCopyrightText: 2022 Christian Günther <cgite@gmx.de>
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using IbFlexReader;
using IbFlexReader.Contracts;
using IbFlexReader.Contracts.Ib;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tradez.Sqlite
{
    [Table("sqlite_master")]
    public class SqliteMaster
    {
        [Column(Name = "type"), NotNull]
        public string Type { get; set; }
    }

    /// <summary>
    /// Database functions for managing trades and 
    /// cashtransactions in a SQLite db.
    /// </summary>
    public class Database
    {
        private Database() { }

        private string dbPath = string.Empty;
        private DataOptions options { get; set; }

        public Database(string dbPath)
        {
            // some example: https://medium.com/@brightoning/dotnet-using-linq2db-with-sqlite3multipleciphers-c726f474d57b

            this.dbPath = dbPath;
            // init cutom mapping without having attributs in class
            // Trade and CashTransaaction
            var mappingSchema = new MappingSchema();
            var builder = new FluentMappingBuilder(mappingSchema);

            builder.Entity<Trade>()
                .HasPrimaryKey(x => x.TradeID)
                .HasTableName(nameof(Trade))
                .Ignore(x => x.TradeTime)
                .Ignore(x => x.TradeDateTime2)
                ;

            builder.Entity<CashTransaction>()
                .HasPrimaryKey(x => x.TransactionID)
                .HasTableName(nameof(CashTransaction))
                ;

            //... other mapping configurations

            // commit configured mappings to mapping schema
            builder.Build();
            options = new DataOptions()
            .UseSQLiteOfficial(GetConnectionString(dbPath))
            .UseMappingSchema(mappingSchema);
        }

        public async Task<SaveResult> SaveFlex(string token, string queryId, string backupFlexFullname)
        {
            var flexResult  = await new Reader().GetByApi(token, queryId, backupFlexFullname, 2);

            return SaveFlexStatements(flexResult.FlexQueryResponse.FlexStatements);
        }

        /// <summary>
        /// Saves all supported FlexStatements (Trade, CashTransaction) to the 
        /// database
        /// </summary>
        /// <param name="token"></param>
        /// <param name="queryId"></param>
        /// <param name="backupFlexFullname"></param>
        /// <returns></returns>
        public SaveResult SaveFlexStatements(FlexStatements statements)
        {
            var result = new SaveResult();
            var connection = $"Data Source = {dbPath}; Version = 3;";
            using (var db = new DataConnection(options))
            {
                db.BeginTransaction();
                foreach (var statement in statements.FlexStatement)
                {
                    var trades = statement.Trades.Trade;
                    var cash = (statement.CashTransactions.CashTransaction != null) ? statement.CashTransactions.CashTransaction : new System.Collections.Generic.List<CashTransaction>();
                    result.Trades.Total += trades.Count();
                    result.Cash.Total += cash.Count();


                    foreach (Trade t in trades)
                    {
                        result.Trades.New += db.InsertOrReplace<Trade>(t);
                    }


                    foreach (CashTransaction c in cash)
                    {
                        result.Cash.New += db.InsertOrReplace<CashTransaction>(c);
                    }
                }

                db.CommitTransaction();
            }

            return result;
        }


        /// <summary>
        /// All Trades in the database as IQueryable for querying through 
        /// linq
        /// </summary>
        /// <returns></returns>
        public IQueryable<Trade> Trades()
        {
            return new DataContext(options).GetTable<Trade>();
        }
        public IQueryable<CashTransaction> CashTransactions()
        {
            return new DataContext(ProviderName.SQLite, GetConnectionString(dbPath)).
                GetTable<CashTransaction>();
        }
        public IQueryable<SqliteMaster> SqliteMaster()
        {
            return new DataContext(ProviderName.SQLite, GetConnectionString(dbPath)).
                GetTable<SqliteMaster>();
        }
        
        public long InsertClosedTrade(IEnumerable<ClosedTrade> trades)
        {
            using (var db = SQLiteTools.CreateDataConnection(GetConnectionString(dbPath)))
            {
                var rows = db.BulkCopy(trades);
                return rows.RowsCopied;
            }
        }

        public static void CreateDatabase(string dbPath)
        {
            var database = new SQLite.SQLiteConnection(dbPath);
        }

#if DEBUG
        /// <summary>
        /// Usefull for creating an sql create statement
        /// </summary>
        /// <typeparam name="Ttable"></typeparam>
        public static void CreateTable<Ttable>(string dbPath)
        {
            var database = new SQLite.SQLiteConnection(dbPath);
            database.CreateTable<Ttable>();
        }
        
        public static void DropTable<Ttable>(string dbPath)
        {
            var database = new SQLite.SQLiteConnection(dbPath);
            database.DropTable<Ttable>();
        }
#endif

        public void CreateTables()
        {
            using (var db = new DataConnection(options))
            {
                db.Execute(SqlCreateTableTrade);
            }
        }

        /// <summary>
        /// Creates the tables "Trades" and "CashTransaction"
        /// if not existing within the database file
        /// </summary>
        /// <param name="dbPath">Fullname of the database file</param>
        public static void CreateTables(string dbPath)
        {
            
        }

        /// <summary>
        /// Drops the tables "Trades" and "CashTransaction"
        /// from the database file
        /// </summary>
        /// <param name="dbPath">Fullname of the database file</param>
        public static void DropTables(string dbPath)
        {
            DropTable<CashReportCurrency>(dbPath);
            DropTable<FxTransaction>(dbPath);
            DropTable<Transfer>(dbPath);
        }

        /// <summary>
        /// Returns the count of all tables of the
        /// database file
        /// </summary>
        /// <param name="dbPath"></param>
        /// <returns></returns>
        public long TableCount(string dbPath)
        {
            return SqliteMaster().Where(t => t.Type == "table").Count();
            //using (var db = SQLiteTools.CreateDataConnection(GetConnectionString(dbPath)))
            //{
            //	db.sq
            //	db.Command.CommandText = "Select Count(*) FROM sqlite_master where type='table';";
            //	return (long) db.Command.ExecuteScalar();
            //}
        }

        /// <summary>
        /// Creates the connectionsstring for a SQLite 3
        /// database from the database fullname
        /// </summary>
        /// <param name="dbPath">Fullname of the database file</param>
        /// <returns></returns>
        private static string GetConnectionString(string dbPath)
        {
            return  $"Data Source = {dbPath}; Version = 3;";
        }

        /// <summary>
        /// Compact View of a trade for using it in python.
        /// There are some additional fields that facilitate calculations
        /// and groupings
        /// </summary>
        private const string SqlCreateViewTrades = @"CREATE VIEW ""Trades"" AS SELECT 
        TradeId,
        ConId,
        AccountId,
        Description, 
        Symbol, 
        UnderlyingSymbol,
        Quantity, 
        TradePrice, 
        ReportDate, 
        TradeDateTime, 
        NetCash, 
        FxRateToBase, 
        NetCash*FxRateToBase as Euro, 
        IbCommission, 
        OpenCloseIndicator, 
        BuySell, 
        AssetCategory,
        Expiry,
        (OpenCloseIndicator == 1) as IsOpenPosition,
        (Notes == 1 || Notes == 16384) as IsExpired,
        (BuySell == 0) as IsBuy,
        SettleDateTarget,
        Proceeds,
        iif(OpenCloseIndicator == 1, ""O"",""C"") as Code
FROM Trade";

        /// <summary>
        /// Handtuned SQL for creating Trade-Tab.
        /// Insert TradeId as PK and changed datatypes to Sqlite types
        /// </summary>
        /// <remarks>Date and Datetime columns should have DATE or DATETIME
        /// type. Otherwise System.Data.SQLite is not working see:<see cref="https://stackoverflow.com/questions/44298684/sqlite-not-storing-decimals-correctly/44312936#44312936"/></remarks>
        private const string SqlCreateTableTrade = @"CREATE TABLE ""Trade"" (
    ""AccruedInterest"" REAL,
    ""AccountId"" TEXT,
    ""AcctAlias"" TEXT,
    ""AssetCategory"" INTEGER,
    ""BrokerageOrderID"" TEXT,
    ""BuySell"" INTEGER,
    ""ChangeInPrice"" REAL,
    ""ChangeInQuantity"" REAL,
    ""ClearingFirmID"" TEXT,
    ""ClosePrice"" REAL,
    ""CommodityType"" TEXT,
    ""Conid"" INTEGER,
    ""Cost"" REAL,
    ""Currency"" INTEGER,
    ""Cusip"" TEXT,
    ""DeliveryType"" TEXT,
    ""Description"" TEXT,
    ""Exchange"" TEXT,
    ""ExchOrderId"" TEXT,
    ""Expiry"" DATETIME,
    ""ExtExecID"" TEXT,
    ""FifoPnlRealized"" REAL,
    ""Figi"" TEXT,
    ""Fineness"" TEXT,
    ""FxPnl"" REAL,
    ""FxRateToBase"" REAL,
    ""HoldingPeriodDateTime"" DATETIME,
    ""IbCommission"" REAL,
    ""IbCommissionCurrency"" INTEGER,
    ""IbExecID"" TEXT,
    ""IbOrderID"" INTEGER,
    ""InitialInvestment"" TEXT,
    ""IsAPIOrder"" TEXT,
    ""Isin"" TEXT,
    ""Issuer"" TEXT,
    ""IssuerCountryCode"" TEXT,
    ""LevelOfDetail"" TEXT,
    ""ListingExchange"" TEXT,
    ""Model"" TEXT,
    ""Multiplier"" REAL,
    ""MtmPnl"" REAL,
    ""NetCash"" REAL,
    ""Notes"" INTEGER,
    ""OpenCloseIndicator"" INTEGER,
    ""OpenDateTime"" DATETIME,
    ""OrderReference"" TEXT,
    ""OrderTime"" DATETIME,
    ""OrderType"" TEXT,
    ""OrigOrderID"" INTEGER,
    ""OrigTradeDate"" DATE,
    ""OrigTradeID"" INTEGER,
    ""OrigTradePrice"" REAL,
    ""OrigTransactionID"" TEXT,
    ""PrincipalAdjustFactor"" TEXT,
    ""Proceeds"" REAL,
    ""PutCall"" INTEGER,
    ""Quantity"" REAL,
    ""ReportDate"" DATE,
    ""RelatedTradeID"" TEXT,
    ""RelatedTransactionID"" TEXT,
    ""Rtn"" TEXT,
    ""SecurityID"" TEXT,
    ""SecurityIDType"" TEXT,
    ""SerialNumber"" TEXT,
    ""SettleDateTarget"" TEXT,
    ""Strike"" REAL,
    ""SubCategory"" TEXT,
    ""Symbol"" TEXT,
    ""Taxes"" REAL,
    ""TraderID"" TEXT,
    ""TradeDate"" DATE,
    ""TradeDateTime"" DATETIME,
    ""TradeID"" INTEGER,
    ""TradeMoney"" REAL,
    ""TradePrice"" REAL,
    ""TransactionID"" INTEGER,
    ""TransactionType"" TEXT,
    ""UnderlyingConid"" INTEGER,
    ""UnderlyingListingExchange"" TEXT,
    ""UnderlyingSecurityID"" TEXT,
    ""UnderlyingSymbol"" TEXT,
    ""VolatilityOrderLink"" TEXT,
    ""Weight"" TEXT,
    ""WhenRealized"" DATETIME,
    ""WhenReopened"" DATETIME,
    PRIMARY KEY(""TradeID"")
);";

        /// <summary>
        /// Handtuned SQL for creating CashTransaction-Tab.
        /// Insert TransactionID as PK and changed datatypes to Sqlite types
        /// </summary>
        /// <remarks>Date and Datetime columns should have DATE or DATETIME
        /// type. Otherwise System.Data.SQLite is not working see:<see cref="https://stackoverflow.com/questions/44298684/sqlite-not-storing-decimals-correctly/44312936#44312936"/></remarks>
        private const string SqlCreateTableCash = @"CREATE TABLE IF NOT EXISTS ""CashTransaction"" (
    ""AccountId""	TEXT,
    ""AcctAlias""	TEXT,
    ""ActionID""	TEXT,
    ""Model""	TEXT,
    ""Currency""	INTEGER,
    ""FxRateToBase""	REAL,
    ""AssetCategory""	INTEGER,
    ""Symbol""	TEXT,
    ""Description""	TEXT,
    ""Conid""	INTEGER,
    ""SecurityID""	TEXT,
    ""SecurityIDType""	TEXT,
    ""Cusip""	TEXT,
    ""Isin""	TEXT,
    ""ListingExchange""	TEXT,
    ""UnderlyingConid""	INTEGER,
    ""UnderlyingSymbol""	TEXT,
    ""UnderlyingSecurityID""	TEXT,
    ""UnderlyingListingExchange""	TEXT,
    ""Issuer""	TEXT,
    ""Multiplier""	REAL,
    ""Strike""	REAL,
    ""Expiry""	DATETIME,
    ""PutCall""	INTEGER,
    ""PrincipalAdjustFactor""	TEXT,
    ""DateTime""	DATETIME,
    ""Amount""	REAL,
    ""Type""	INTEGER,
    ""TradeID""	INTEGER,
    ""Code""	TEXT,
    ""TransactionID""	INTEGER,
    ""ReportDate""	DATE,
    ""ClientReference""	TEXT,
    ""SettleDate""	DATE,
    ""SerialNumber"" TEXT,
    ""SubCategory"" TEXT,
    ""DeliveryType"" TEXT,
    ""CommodityType"" TEXT,
    ""Fineness"" TEXT,
    ""Weight"" TEXT,
    ""LevelOfDetail"" TEXT,
    ""Figi"" TEXT,
    ""IssuerCountryCode"" TEXT,
    ""AvailableForTradingDate"" TEXT,
    ""ExDate"" TEXT,
    PRIMARY KEY(""TransactionID"")
);";
        private const string SqlCreateTableClosedTrade = @"CREATE TABLE ""ClosedTrade"" (
""OpenTradeId"" INTEGER ,
""CloseTradeId"" INTEGER ,
""Quantity"" REAL )";
        
        private const string SqlCreateTableCashReportCurrency = @"CREATE TABLE ""CashReportCurrency"" (
""AccountId"" TEXT ,
""AcctAlias"" TEXT ,
""Model"" TEXT ,
""Currency"" INTEGER ,
""LevelOfDetail"" TEXT ,
""FromDate"" DATETIME ,
""ToDate"" DATETIME ,
""StartingCash"" REAL ,
""StartingCashSec"" REAL ,
""StartingCashCom"" REAL ,
""ClientFees"" REAL ,
""ClientFeesSec"" REAL ,
""ClientFeesCom"" REAL ,
""Commissions"" REAL ,
""CommissionsSec"" REAL ,
""CommissionsCom"" REAL ,
""ReferralFee"" REAL ,
""ReferralFeeSec"" REAL ,
""ReferralFeeCom"" REAL ,
""CommissionCreditsRedemption"" REAL ,
""CommissionCreditsRedemptionSec"" REAL ,
""CommissionCreditsRedemptionCom"" REAL ,
""BillableCommissions"" REAL ,
""BillableCommissionsSec"" REAL ,
""BillableCommissionsCom"" REAL ,
""DepositWithdrawals"" REAL ,
""DepositWithdrawalsSec"" REAL ,
""DepositWithdrawalsCom"" REAL ,
""Deposits"" REAL ,
""DepositsSec"" REAL ,
""DepositsCom"" REAL ,
""Withdrawals"" REAL ,
""WithdrawalsSec"" REAL ,
""WithdrawalsCom"" REAL ,
""CarbonCredits"" REAL ,
""CarbonCreditsSec"" REAL ,
""CarbonCreditsCom"" REAL ,
""Donations"" REAL ,
""DonationsSec"" REAL ,
""DonationsCom"" REAL ,
""AccountTransfers"" REAL ,
""AccountTransfersSec"" REAL ,
""AccountTransfersCom"" REAL ,
""LinkingAdjustments"" REAL ,
""LinkingAdjustmentsSec"" REAL ,
""LinkingAdjustmentsCom"" REAL ,
""InternalTransfers"" REAL ,
""InternalTransfersSec"" REAL ,
""InternalTransfersCom"" REAL ,
""PaxosTransfers"" REAL ,
""PaxosTransfersSec"" REAL ,
""PaxosTransfersCom"" REAL ,
""ExcessFundSweep"" REAL ,
""ExcessFundSweepSec"" REAL ,
""ExcessFundSweepCom"" REAL ,
""DebitCardActivity"" REAL ,
""DebitCardActivitySec"" REAL ,
""DebitCardActivityCom"" REAL ,
""BillPay"" REAL ,
""BillPaySec"" REAL ,
""BillPayCom"" REAL ,
""Dividends"" REAL ,
""DividendsSec"" REAL ,
""DividendsCom"" REAL ,
""InsuredDepositdoubleerest"" REAL ,
""InsuredDepositdoubleerestSec"" REAL ,
""InsuredDepositdoubleerestCom"" REAL ,
""Brokerdoubleerest"" REAL ,
""BrokerdoubleerestSec"" REAL ,
""BrokerdoubleerestCom"" REAL ,
""BrokerFees"" REAL ,
""BrokerFeesSec"" REAL ,
""BrokerFeesCom"" REAL ,
""Bonddoubleerest"" REAL ,
""BonddoubleerestSec"" REAL ,
""BonddoubleerestCom"" REAL ,
""CashSettlingMtm"" REAL ,
""CashSettlingMtmSec"" REAL ,
""CashSettlingMtmCom"" REAL ,
""RealizedVm"" REAL ,
""RealizedVmSec"" REAL ,
""RealizedVmCom"" REAL ,
""RealizedForexVm"" REAL ,
""RealizedForexVmSec"" REAL ,
""RealizedForexVmCom"" REAL ,
""CfdCharges"" REAL ,
""CfdChargesSec"" REAL ,
""CfdChargesCom"" REAL ,
""NetTradesSales"" REAL ,
""NetTradesSalesSec"" REAL ,
""NetTradesSalesCom"" REAL ,
""NetTradesPurchases"" REAL ,
""NetTradesPurchasesSec"" REAL ,
""NetTradesPurchasesCom"" REAL ,
""AdvisorFees"" REAL ,
""AdvisorFeesSec"" REAL ,
""AdvisorFeesCom"" REAL ,
""FeesReceivables"" REAL ,
""FeesReceivablesSec"" REAL ,
""FeesReceivablesCom"" REAL ,
""PaymentInLieu"" REAL ,
""PaymentInLieuSec"" REAL ,
""PaymentInLieuCom"" REAL ,
""TransactionTax"" REAL ,
""TransactionTaxSec"" REAL ,
""TransactionTaxCom"" REAL ,
""TaxReceivables"" REAL ,
""TaxReceivablesSec"" REAL ,
""TaxReceivablesCom"" REAL ,
""WithholdingTax"" REAL ,
""WithholdingTaxSec"" REAL ,
""WithholdingTaxCom"" REAL ,
""Withholding871m"" REAL ,
""Withholding871mSec"" REAL ,
""Withholding871mCom"" REAL ,
""WithholdingCollectedTax"" REAL ,
""WithholdingCollectedTaxSec"" REAL ,
""WithholdingCollectedTaxCom"" REAL ,
""SalesTax"" REAL ,
""SalesTaxSec"" REAL ,
""SalesTaxCom"" REAL ,
""BillableSalesTax"" REAL ,
""BillableSalesTaxSec"" REAL ,
""BillableSalesTaxCom"" REAL ,
""IpoSubscription"" REAL ,
""IpoSubscriptionSec"" REAL ,
""IpoSubscriptionCom"" REAL ,
""FxTranslationGainLoss"" REAL ,
""FxTranslationGainLossSec"" REAL ,
""FxTranslationGainLossCom"" REAL ,
""OtherFees"" REAL ,
""OtherFeesSec"" REAL ,
""OtherFeesCom"" REAL ,
""Other"" REAL ,
""OtherSec"" REAL ,
""OtherCom"" REAL ,
""EndingCash"" REAL ,
""EndingCashSec"" REAL ,
""EndingCashCom"" REAL ,
""EndingSettledCash"" REAL ,
""EndingSettledCashSec"" REAL ,
""EndingSettledCashCom"" REAL ,
""SlbStartingCashCollateral"" REAL ,
""SlbStartingCashCollateralSec"" REAL ,
""SlbStartingCashCollateralCom"" REAL ,
""SlbNetSecuritiesLentActivity"" REAL ,
""SlbNetSecuritiesLentActivitySec"" REAL ,
""SlbNetSecuritiesLentActivityCom"" REAL ,
""SlbEndingCashCollateral"" REAL ,
""SlbEndingCashCollateralSec"" REAL ,
""SlbEndingCashCollateralCom"" REAL ,
""SlbNetCash"" REAL ,
""SlbNetCashSec"" REAL ,
""SlbNetCashCom"" REAL ,
""SlbNetSettledCash"" REAL ,
""SlbNetSettledCashSec"" REAL ,
""SlbNetSettledCashCom"" REAL ,
""InsuredDepositInterest"" REAL ,
""InsuredDepositInterestSec"" REAL ,
""InsuredDepositInterestCom"" REAL ,
""BrokerInterest"" REAL ,
""BrokerInterestSec"" REAL ,
""BrokerInterestCom"" REAL ,
""BondInterest"" REAL ,
""BondInterestSec"" REAL ,
""BondInterestCom"" REAL )";
        
        private const string SqlCreateTableTransfer = @"CREATE TABLE ""Transfer"" (
""AccountId"" TEXT ,
""AcctAlias"" TEXT ,
""Model"" TEXT ,
""Currency"" INTEGER ,
""FxRateToBase"" REAL ,
""AssetCategory"" INTEGER ,
""Symbol"" TEXT ,
""Description"" TEXT ,
""Conid"" INTEGER ,
""SecurityID"" TEXT ,
""SecurityIDType"" TEXT ,
""Cusip"" TEXT ,
""Isin"" TEXT ,
""ListingExchange"" TEXT ,
""UnderlyingConid"" INTEGER ,
""UnderlyingSymbol"" TEXT ,
""UnderlyingSecurityID"" TEXT ,
""UnderlyingListingExchange"" TEXT ,
""Issuer"" TEXT ,
""Multiplier"" REAL ,
""Strike"" REAL ,
""Expiry"" DATETIME ,
""PutCall"" INTEGER ,
""PrincipalAdjustFactor"" TEXT ,
""ReportDate"" TEXT ,
""Date"" DATETIME ,
""TradeDateTime"" DATETIME ,
""Type"" TEXT ,
""Direction"" TEXT ,
""Company"" TEXT ,
""Account"" TEXT ,
""AccountName"" TEXT ,
""Quantity"" REAL ,
""TransferPrice"" REAL ,
""PositionAmount"" REAL ,
""PositionAmountInBase"" REAL ,
""PnlAmount"" REAL ,
""PnlAmountInBase"" REAL ,
""FxPnl"" REAL ,
""CashTransfer"" REAL ,
""Code"" TEXT ,
""ClientReference"" TEXT ,
""TransactionID"" INTEGER )";
        
        private const string SqlCreateTableFxTransaction = @"CREATE TABLE ""FxTransaction"" (
""AccountId"" TEXT ,
""AcctAlias"" TEXT ,
""Model"" TEXT ,
""AssetCategory"" TEXT ,
""ReportDate"" TEXT ,
""FunctionalCurrency"" TEXT ,
""FxCurrency"" TEXT ,
""ActivityDescription"" TEXT ,
""DateTime"" TEXT ,
""Quantity"" REAL ,
""Proceeds"" REAL ,
""Cost"" REAL ,
""RealizedPL"" REAL ,
""Code"" TEXT ,
""LevelOfDetail"" TEXT )";
        
        private const string SqlCreateTableEquitySummaryByReportDateInBase = @"CREATE TABLE ""EquitySummaryByReportDateInBase"" (
""AccountId"" TEXT ,
""AcctAlias"" TEXT ,
""Model"" TEXT ,
""ReportDate"" TEXT ,
""Cash"" REAL ,
""CashLong"" REAL ,
""CashShort"" REAL ,
""SlbCashCollateral"" REAL ,
""SlbCashCollateralLong"" REAL ,
""SlbCashCollateralShort"" REAL ,
""Stock"" REAL ,
""StockLong"" REAL ,
""StockShort"" REAL ,
""SlbDirectSecuritiesBorrowed"" REAL ,
""SlbDirectSecuritiesBorrowedLong"" REAL ,
""SlbDirectSecuritiesBorrowedShort"" REAL ,
""SlbDirectSecuritiesLent"" REAL ,
""SlbDirectSecuritiesLentLong"" REAL ,
""SlbDirectSecuritiesLentShort"" REAL ,
""Options"" REAL ,
""OptionsLong"" REAL ,
""OptionsShort"" REAL ,
""Commodities"" REAL ,
""CommoditiesLong"" REAL ,
""CommoditiesShort"" REAL ,
""Bonds"" REAL ,
""BondsLong"" REAL ,
""BondsShort"" REAL ,
""Notes"" REAL ,
""NotesLong"" REAL ,
""NotesShort"" REAL ,
""Funds"" REAL ,
""FundsLong"" REAL ,
""FundsShort"" REAL ,
""InterestAccruals"" REAL ,
""InterestAccrualsLong"" REAL ,
""InterestAccrualsShort"" REAL ,
""SoftDollars"" REAL ,
""SoftDollarsLong"" REAL ,
""SoftDollarsShort"" REAL ,
""ForexCfdUnrealizedPl"" REAL ,
""ForexCfdUnrealizedPlLong"" REAL ,
""ForexCfdUnrealizedPlShort"" REAL ,
""CfdUnrealizedPl"" REAL ,
""CfdUnrealizedPlLong"" REAL ,
""CfdUnrealizedPlShort"" REAL ,
""DividendAccruals"" REAL ,
""DividendAccrualsLong"" REAL ,
""DividendAccrualsShort"" REAL ,
""FdicInsuredBankSweepAccountCashComponent"" REAL ,
""FdicInsuredBankSweepAccountCashComponentLong"" REAL ,
""FdicInsuredBankSweepAccountCashComponentShort"" REAL ,
""FdicInsuredAccountInterestAccrualsComponent"" REAL ,
""FdicInsuredAccountInterestAccrualsComponentLong"" REAL ,
""FdicInsuredAccountInterestAccrualsComponentShort"" REAL ,
""Total"" REAL ,
""TotalLong"" REAL ,
""TotalShort"" REAL ,
""BrokerCashComponent"" REAL ,
""IpoSubscription"" REAL ,
""IpoSubscriptionLong"" REAL ,
""IpoSubscriptionShort"" REAL ,
""BrokerInterestAccrualsComponent"" REAL ,
""BondInterestAccrualsComponent"" REAL ,
""BrokerFeesAccrualsComponent"" REAL ,
""BrokerFeesAccrualsComponentLong"" REAL ,
""BrokerFeesAccrualsComponentShort"" REAL ,
""PhysDel"" REAL ,
""PhysDelLong"" REAL ,
""PhysDelShort"" REAL ,
""Crypto"" REAL ,
""CryptoLong"" REAL ,
""CryptoShort"" REAL ,
""Currency"" INTEGER ,
""BrokerCashComponentLong"" REAL ,
""BrokerCashComponentShort"" REAL ,
""BrokerInterestAccrualsComponentLong"" REAL ,
""BrokerInterestAccrualsComponentShort"" REAL ,
""BondInterestAccrualsComponentLong"" REAL ,
""BondInterestAccrualsComponentShort"" REAL )";
        
    }
}