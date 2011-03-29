using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			using (var browser = new IE("http://localhost:53759/Reports/reports.aspx"))
			{
				//browser.Visible = true;
				TableRow row = browser.Table(Find.ByClass("DocumentDataTable HighLightCurrentRow")).TableRows.First();
				TableRow row2 = (TableRow)row.NextSibling;
				TableCellCollection cells = row2.OwnTableCells;
				var cell = cells[0];
				browser.GoTo("http://localhost:53759/Reports/schedule.aspx?r=" + cell.Text);

				browser.Button(Find.ByValue("Выполнить")).Click();				
				Assert.That(browser.Text, Is.StringContaining("Успешно запущен разовый отчет"));

				Process[] processes = Process.GetProcesses();
				bool finded = false;
				while (true)
				{
					Thread.Sleep(50);
					foreach (var process in processes)
					{
						if (process.ProcessName.Contains("ReportSystemBoot"))
						{
							finded = true;
							break;
						}
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
