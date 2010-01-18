using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test.SpecialReport
{
	[TestFixture]
	public class SpecialNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void SpecialNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialNew);
			var report = new SpecReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialNew);
		}

		[Test]
		public void SpecialNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialNewDifficult);
			var report = new SpecReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialNewDifficult);
		}
	}
}
