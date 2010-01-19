using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MixedReportNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MixedNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedNew);
			var report = new MixedReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedNew);
		}

		[Test]
		public void MixedNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedNewDifficult);
			var report = new MixedReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedNewDifficult);
		}
	}
}
