using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using ReportSystem.Profiling;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MixedReportProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MixedProductName()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedProductName);
			var report = new MixedReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedProductName);
		}

		[Test]
		public void MixedFullName()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedFullName);
			var report = new MixedReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedFullName);
		}

		[Test]
		public void MixedName()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedName);
			var report = new MixedReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedName);
		}

		[Test]
		public void MixedFull()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedFull);
			var report = new MixedReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedFull);
		}
	}
}
