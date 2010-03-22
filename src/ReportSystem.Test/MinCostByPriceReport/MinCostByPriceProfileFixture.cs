using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MinCostByPriceProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MinCostByPrice()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPrice);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPrice);
		}

		[Test]
		public void MinCostByPriceCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceCount);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceCount);
		}

		[Test]
		public void MinCostByPriceCountProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceCountProducer);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceCountProducer);
		}

		[Test]
		public void MinCostByPriceProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceProducer);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceProducer);
		}
	}
}