using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test.MatrixReport
{
	[TestFixture]
	public class MatrixReportFixture : BaseProfileFixture
	{
		[Test]
		public void BaseTest()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MatrixReport);
			var report = new Inforoom.ReportSystem.ByOffers.MatrixReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MatrixReport);
		}
	}
}
