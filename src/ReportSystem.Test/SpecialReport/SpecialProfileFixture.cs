using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SpecialProfileFixture : BaseProfileFixture
	{
		[Test]
		public void Special()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Special);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Special);
		}

		[Test]
		public void SpecialCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialCount);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialCount);
		}

		[Test]
		public void SpecialCountProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialCountProducer);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialCountProducer);
		}

		[Test]
		public void SpecialProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialProducer);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialProducer);
		}
	}
}
