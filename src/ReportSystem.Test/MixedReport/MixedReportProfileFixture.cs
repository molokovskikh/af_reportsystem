using System;
using System.Configuration;
using System.Data;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MixedReportProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MixedProductName()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedProductName);
			var report = new MixedReport(Conn, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedProductName);
		}

		[Test]
		public void MixedFullName()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedFullName);
			var report = new MixedReport(Conn, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedFullName);
		}

		[Test]
		public void MixedName()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedName);
			var report = new MixedReport(Conn, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedName);
		}

		[Test]
		public void MixedFull()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedFull);
			var report = new MixedReport(Conn, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedFull);
		}

		[Test]
		public void MixedFullNoActual()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedFullNoActual);
			var report = new MixedReport(Conn, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedFullNoActual);
			foreach (DataRow row in report.DSResult.Rows) {
				Assert.That(String.IsNullOrEmpty(row["F1"].ToString()), Is.EqualTo(true));
			}
		}
	}
}