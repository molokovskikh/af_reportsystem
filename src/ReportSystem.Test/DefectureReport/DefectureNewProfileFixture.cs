using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class DefectureNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void DefectureNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNew);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNew);
		}

		[Test]
		public void DefectureNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNewDifficult);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNewDifficult);
		}
	}
}
