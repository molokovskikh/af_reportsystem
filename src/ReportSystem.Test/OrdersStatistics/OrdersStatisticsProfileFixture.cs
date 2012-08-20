﻿using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	internal class OrdersStatisticsProfileFixture : BaseProfileFixture
	{
		[Test]
		public void CheckReport()
		{
			Property("ReportInterval", 7);
			Property("ByPreviousMonth", false);

			var report = new OrdersStatistics(0, "Automate Created Report", Conn, ReportFormats.Excel, properties);
			TestHelper.ProcessReport(report, ReportsTypes.OrdersStatistics);
		}
	}
}