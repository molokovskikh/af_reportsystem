using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OptimizationEfficiencyFixture : BaseProfileFixture
	{
		[Test]
		public void OptimizationEfficiencyTest()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OptimizationEfficiency);
			var report = new OptimizationEfficiency(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OptimizationEfficiency);
		}

		[Test]
		public void OptimizationEfficiencyAllClientsTest()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OptimizationEfficiencyAllClients);
			var report = new OptimizationEfficiency(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OptimizationEfficiencyAllClients);
		}

		[Test]
		public void OptimizationEfficiencyNorman()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OptimizationEfficiencyWithSupplier);
			var report = new OptimizationEfficiency(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OptimizationEfficiencyWithSupplier);
		}

		[Test]
		public void OptimizationEfficiencyNew()
		{
			var fileName = "OptimizationEfficiencyNew.xls";
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 7);
			Property("FirmCode", 12423);
			report = new OptimizationEfficiency(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OptimizationRivalOrders()
		{
			var fileName = "OptimizationRivalOrders.xls";
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 70);
			report = new OptimizationRivalOrders(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OptimizationRivalOrdersWithSupplier()
		{
			var fileName = "OptimizationRivalOrdersWithSupplier.xls";
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 7);
			Property("FirmCode", 12423);
			report = new OptimizationRivalOrders(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OptimizationRivalOrdersWithClient()
		{
			var fileName = "OptimizationRivalOrdersWithClient.xls";
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 70);
			Property("ClientCode", 376);
			report = new OptimizationRivalOrders(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}