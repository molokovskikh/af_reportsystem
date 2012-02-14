using System.Linq;
using System.Threading;
using NUnit.Framework;
using WatiN.Core;
using ReportTuner.Test.Helpers;
using System.Diagnostics;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	class OneShortReportFixture : WatinFixture
	{
		[Test]
		[Ignore]
		public void TestOneShortReport()
		{
			using (var browser = new IE("http://localhost:53759/Reports/GeneralReports.aspx"))
			{
				browser.Visible = true;
				var row = browser.Table(Find.ByClass("DocumentDataTable HighLightCurrentRow")).TableRows.First();
				var row2 = (TableRow)row.NextSibling;
				var cells = row2.OwnTableCells;
				var cell = cells[0];
				browser.GoTo("http://localhost:53759/Reports/schedule.aspx?r=" + cell.Text);

				browser.Button(Find.ByValue("Выполнить")).Click();				
				Assert.That(browser.Text, Is.StringContaining("Успешно запущен разовый отчет"));

				var processes = Process.GetProcesses();
				var finded = false;
				while (true)
				{
					Thread.Sleep(50);
					if (processes.Any(process => process.ProcessName.Contains("ReportSystemBoot"))) {
						finded = true;
					}
					break;
				}
				Assert.That(finded, Is.True);
				Thread.Sleep(15000);
				browser.Refresh();
				Thread.Sleep(15000);
				Assert.That(browser.Text, Is.StringContaining("Операция выполнена"));
			}
		}
	}
}
