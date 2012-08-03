using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OrdersReportFixture
	{
		[Test]
		public void Get_emtpy_rows()
		{
			var report = new OrdersReport();
			report.GroupHeaders.Add(new ColumnGroupHeader("test", "test1", "test2"));
			report.FilterDescriptions.Add("Тестовый отчет");
			report.FilterDescriptions.Add("Тестовый отчет");
			Assert.That(report.EmptyRowCount, Is.EqualTo(3));
		}
	}
}