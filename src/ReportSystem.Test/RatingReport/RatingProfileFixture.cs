using System.Collections.Generic;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test
{
	[TestFixture]
	public class RatingProfileFixture : BaseProfileFixture
	{
		[Test]
		public void Rating()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Rating);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Rating);
		}

		[Test]
		public void RatingJunkOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingJunkOnly);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingJunkOnly);
		}

		[Test]
		public void RatingNotJunkOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingNotJunkOnly);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingNotJunkOnly);
		}

		[Test]
		public void RatingFull()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingFull);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingFull);
		}

		[Test]
		public void Build_chart()
		{
			Property("ByPreviousMonth", false);
			Property("ClientCodeEqual", new List<ulong> {3110, 465, 11279});
			Property("ProductNamePosition", 0);
			Property("BuildChart", true);
			var file = "Build_chart.xls";
			report = new RatingReport(1, file, Conn, ReportFormats.Excel, properties);
			BuildOrderReport(file);
		}
	}
}
