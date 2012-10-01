using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class CombineProfileFixture : BaseProfileFixture
	{
		[Test, Ignore("Временно, выполняется слишком долго")]
		public void Combine()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Combine);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Combine);
		}

		[Test]
		public void CombineCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineCount);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineCount);
		}

		[Test]
		public void CombineCountAndProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineCountAndProducer);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineCountAndProducer);
		}

		[Test, Ignore("Временно, выполняется слишком долго")]
		public void CombineProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineProducer);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineProducer);
		}

		[Test]
		public void CombineCountProducerByWeightCost()
		{
			Property("ReportType", 4);
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", false);
			Property("CalculateByCatalog", false);
			Property("ByWeightCosts", true);
			BuildReport("CombineCountProducerByWeightCost.xls", typeof(CombReport));
		}
	}
}