using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SupplierOrdersStatisticsProfileFixture : BaseProfileFixture
	{
		[Test]
		public void SupplierOrdersStatisticsType1Check()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SupplierOrdersStatisticsType1);
			var report = new SupplierOrdersStatistics(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SupplierOrdersStatisticsType1);
		}

		[Test]
		public void SupplierOrdersStatisticsType2Check()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SupplierOrdersStatisticsType2);
			var report = new SupplierOrdersStatistics(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SupplierOrdersStatisticsType2);
		}
	}
}
