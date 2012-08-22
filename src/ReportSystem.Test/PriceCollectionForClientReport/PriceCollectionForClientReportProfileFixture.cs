using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test.PriceCollectionForClientReport
{
	[TestFixture]
	public class PriceCollectionForClientReportProfileFixture : BaseProfileFixture
	{
		[Test]
		public void CheckReport()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.PriceCollectionForClientReport);
			var report = new Inforoom.ReportSystem.PriceCollectionForClientReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.PriceCollectionForClientReport);
		}
	}
}