using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MinCostProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MinCost()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCost);
			var report = new CombShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCost);
		}

		[Test]
		public void MinCostCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostCount);
			var report = new CombShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostCount);
		}

		[Test]
		public void MinCostCountAndProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostCountAndProducer);
			var report = new CombShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostCountAndProducer);
		}

		[Test]
		public void MinCostProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostProducer);
			var report = new CombShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostProducer);
		}

		[Test]
		public void MinCostManyClients()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostManyClients);
			var report = new CombShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostManyClients);
		}
	}
}
