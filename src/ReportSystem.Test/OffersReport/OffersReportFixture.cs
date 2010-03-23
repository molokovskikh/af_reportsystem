using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OffersReportFixture : BaseProfileFixture
	{
		[Test]
		public void Offers_report_to_excel()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OffersReport);
			var report = new OffersReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OffersReport);
		}

		[Test]
		public void Offers_report_to_dbf()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OffersReport);
			var report = new OffersReport(0, "Automate Created Report", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.OffersReport);
		}
	}
}
