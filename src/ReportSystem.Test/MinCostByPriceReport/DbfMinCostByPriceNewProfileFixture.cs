using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test.MinCostByPriceReport
{
	[TestFixture]
	public class DbfMinCostByPriceNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void DbfMinCostByPriceNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			var report = new SpecShortReport(0, "MinCostByPriceNewDbf", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNew);
		}

		[Test]
		public void DbfMinCostByPriceNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewDifficult);
			var report = new SpecShortReport(0, "MinCostByPriceNewDifficultDbf", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewDifficult);
		}
	}
}
