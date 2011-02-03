using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;


namespace ReportSystem.Test
{
	[TestFixture]
	class PricesOfCompetitorsReportFixture : BaseProfileFixture
	{
		[Test]
		public void PricesOfCompetitorsReport()
		{
			var type = ReportsTypes.Rating;
			var props = TestHelper.LoadProperties(type);
			var report = new RatingReport(0, "Rating", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}
	}
}
