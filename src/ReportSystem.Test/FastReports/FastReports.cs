using Inforoom.ReportSystem.Helpers;
using NUnit.Framework;
using Inforoom.ReportSystem.FastReports;
using Inforoom.ReportSystem;

namespace ReportSystem.Test.FastReports
{
	[TestFixture]
	public class FastReports : BaseProfileFixture
	{
		[Test]
		public void PharmacyOffersReportTest()
		{
			ProfileHelper.Start();

			var type = ReportsTypes.PharmacyOffers;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyOffersReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);

			ProfileHelper.End();
		}
	}
}
