using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test.PriceCollectionForClientReport
{
	[TestFixture, Ignore("Требуется тестовая база данных")]
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