using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture, Ignore("Требуется тестовая база данных")]
	public class DbfMinCostByPriceProfileFixture : BaseProfileFixture
	{
		[Test]
		public void DbfMinCostByPrice()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPrice);
			var report = new SpecShortReport(0, "MinCostByPrice", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPrice);
		}

		[Test]
		public void DbfMinCostByPriceCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceCount);
			var report = new SpecShortReport(0, "MinCostByPriceCount", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceCount);
		}

		[Test]
		public void DbfMinCostByPriceCountProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceCountProducer);
			var report = new SpecShortReport(0, "MinCostByPriceCountProducer", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceCountProducer);
		}

		[Test]
		public void DbfMinCostByPriceProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceProducer);
			var report = new SpecShortReport(0, "MinCostByPriceProducer", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceProducer);
		}
	}
}