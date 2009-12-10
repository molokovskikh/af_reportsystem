using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using ReportSystem.Profiling;

namespace ReportSystem.Test
{
	[TestFixture]
	public class DbfMinCostProfileFixture : BaseProfileFixture
	{
		[Test]
		public void DbfMinCost()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCost);
			var report = new CombShortReport(0, "MinCost", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCost);
		}

		[Test]
		public void DbfMinCostCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostCount);
			var report = new CombShortReport(0, "MinCostCount", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostCount);
		}

		[Test]
		public void DbfMinCostCountAndProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostCountAndProducer);
			var report = new CombShortReport(0, "MinCostCountAndProducer", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostCountAndProducer);
		}

		[Test]
		public void DbfMinCostProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostProducer);
			var report = new CombShortReport(0, "MinCostProducer", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostProducer);
		}

		[Test]
		public void DbfMinCostManyClients()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostManyClients);
			var report = new CombShortReport(0, "MinCostManyClients", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostManyClients);
		}
	}
}
