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
			var report = new OptimizationEfficiency(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OptimizationEfficiency);
		}

		[Test]
		public void OptimizationEfficiencyAllClientsTest()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OptimizationEfficiencyAllClients);
			var report = new OptimizationEfficiency(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OptimizationEfficiencyAllClients);
		}

		[Test]
		public void OptimizationEfficiencyNorman()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OptimizationEfficiencyWithSupplier);
			var report = new OptimizationEfficiency(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OptimizationEfficiencyWithSupplier);
		}
	}
}
