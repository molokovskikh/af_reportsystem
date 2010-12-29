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
			var type = ReportsTypes.PricesOfCompetitors;
			var props = TestHelper.LoadProperties(type);
			var report = new PricesOfCompetitorsReport(0, "PricesOfCompetitors", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}
	}
}
