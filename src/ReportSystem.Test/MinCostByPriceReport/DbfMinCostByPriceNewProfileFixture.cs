using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test
{
	[TestFixture]
	public class DbfMinCostByPriceNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void DbfMinCostByPriceNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			var report = new SpecShortReport(0, "MinCostByPriceNewDbf", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNew);
		}

		[Test]
		public void DbfMinCostByPriceNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewDifficult);
			var report = new SpecShortReport(0, "MinCostByPriceNewDifficultDbf", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewDifficult);
		}

		[Test, Ignore("Разобраться")]
		public void DbfMinCostByPriceNewWithClients()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewWithClients);
			var report = new SpecShortReport(0, "MinCostByPriceNewWithClientsDbf", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewWithClients);
		}

		[Test, Ignore("Разобраться")]
		public void DbfMinCostByPriceNewWithClientsWithoutAssortmentPrice()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewWithClientsWithoutAssortmentPrice);
			var report = new SpecShortReport(0, "MinCostByPriceNewWithClientsWithoutAssortmentPriceDbf", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewWithClientsWithoutAssortmentPrice);
		}
	}
}