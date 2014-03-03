using System.Collections.Generic;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class DefectureNewProfileFixture : BaseProfileFixture
	{
		[Test, Ignore]
		public void DefectureNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNew);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNew);
			DefecturePharmacie.TestReportResultOnPharmacie(report.DSResult);
		}

		[Test, Ignore]
		public void DefectureNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNewDifficult);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNewDifficult);
			DefecturePharmacie.TestReportResultOnPharmacie(report.DSResult);
		}
	}
}