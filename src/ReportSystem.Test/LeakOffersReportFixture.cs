using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class LeakOffersReportFixture : BaseProfileFixture
	{
		[Test]
		public void Make_report()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.LeakOffers);
			var report = new LeakOffersReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.LeakOffers);
		}
	}
}