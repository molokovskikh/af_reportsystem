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
	}
}
