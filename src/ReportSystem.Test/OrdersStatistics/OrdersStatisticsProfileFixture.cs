using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			var props = TestHelper.LoadProperties(ReportsTypes.OrdersStatistics);
			var report = new OrdersStatistics(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OrdersStatistics);
		}
	}
}
