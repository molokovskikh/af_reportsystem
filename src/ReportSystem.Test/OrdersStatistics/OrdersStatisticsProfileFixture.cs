using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	class OrdersStatisticsProfileFixture : BaseProfileFixture
	{
		[Test]
		public void CheckReport()
		{
			Property("ReportInterval", 7);
			Property("ByPreviousMonth", false);

			var report = new OrdersStatistics(0, "Automate Created Report", Conn, false, ReportFormats.Excel, properties);
			TestHelper.ProcessReport(report, ReportsTypes.OrdersStatistics);
		}
	}
}
