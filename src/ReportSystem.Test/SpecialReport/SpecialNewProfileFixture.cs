using System.Collections.Generic;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SpecialNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void SpecialNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialNew);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialNew);
		}

		[Test]
		public void SpecialNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialNewDifficult);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialNewDifficult);
		}

		[Test]
		public void SpecialByBaseCosts()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialByBaseCosts);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialByBaseCosts);
		}

		[Test]
		public void SpecialByBaseCostsPriceCodeNonEqual()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialByBaseCostsPriceCodeNonEqual);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialByBaseCostsPriceCodeNonEqual);
		}

		[Test, Ignore("Временно, выполняется слишком долго")]
		public void Get_report_for_retail()
		{
			Property("Retail", true);
			Property("ReportType", 0);
			Property("PriceCode", 200);
			Property("ShowPercents", false);
			Property("ReportIsFull", false);
			Property("CalculateByCatalog", false);
			Property("ReportSortedByPrice", false);
			BuildReport(reportType: typeof(SpecReport));
		}

		[Test]
		public void SpecialByBaseCostNew()
		{
			var fileName = "SpecialByBaseCostNew.xls";
			Property("ReportType", 2);
			Property("RegionEqual", new List<ulong> {
				4096
			});

			//Property("PriceCodeEqual", new List<ulong> {
			//	4838,
			//	4479,
			//	196
			//});

			Property("ReportIsFull", false);
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 200);
			Property("ByBaseCosts", true);
			report = new SpecReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void SpecialByBaseCostAssort()
		{
			var fileName = "SpecialByBaseCostAssort.xls";
			Property("ReportType", 1);
			Property("RegionEqual", new List<ulong> {
				2097152
			});

			Property("PriceCodeEqual", new List<ulong> {
				338,
				4023
			});

			Property("SupplierNoise", 5);
			Property("ReportIsFull", false);
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 5699);
			Property("ByBaseCosts", true);
			report = new SpecReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}
