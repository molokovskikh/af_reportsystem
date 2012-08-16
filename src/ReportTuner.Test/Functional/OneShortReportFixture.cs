using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ReportTuner.Models;
using WatiN.Core;
using System.Diagnostics;
using Test.Support.Web;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class OneShortReportFixture : WatinFixture2
	{
		[Test]
		public void TestOneShortReport()
		{
			using (var browser = new IE("http://localhost:53759/Reports/GeneralReports.aspx"))
			{
				var row = browser.Table(Find.ByClass("DocumentDataTable HighLightCurrentRow")).TableRows.First();
				var row2 = (TableRow)row.NextSibling;
				var cells = row2.OwnTableCells;
				var cell = cells[0];
				browser.GoTo("http://localhost:53759/Reports/schedule.aspx?r=" + cell.Text);

				browser.Button(Find.ByValue("Выполнить")).Click();
				Assert.That(browser.Text, Is.StringContaining("Успешно запущен разовый отчет"));

				var processes = Process.GetProcesses();
				var finded = false;
				for (int i = 0; i < 20; i++) {
					if (processes.Any(process => process.ProcessName.Contains("ReportSystem"))) {
						finded = true;
					}
					Thread.Sleep(50);
				}
				Assert.That(finded, Is.True);
			}
		}

		[Test]
		public void Set_shedule_month()
		{
			using (var browser = new IE("http://localhost:53759/Reports/schedule.aspx?r=1")) {
				browser.RadioButton(Find.ByValue("Ежемесячно")).Checked = true;
				browser.Div("firstSixMonth").ChildOfType<CheckBox>(box => !box.Checked).Checked = true;
				browser.Div("firstFifteenDays").ChildOfType<CheckBox>(box => !box.Checked).Checked = true;
				browser.Button(Find.ByValue("Применить")).Click();
				Assert.That(browser.Text, Is.StringContaining("Задать расписание для отчета "));
			}
		}

		[Test]
		public void Add_help_file_for_general_report()
		{
			var report = session.Get<GeneralReport>((ulong)1);
			report.Files.Clear();
			session.Save(report);
			session.Flush();
			browser = Open("Reports/Reports.aspx?r=1");
			Click("Добавить файл");
			session.Refresh(report);
			Assert.That(report.Files.Count, Is.EqualTo(1));
			AssertText("Выбор файла");
			browser.Button(Find.ByClass("deleteFileButton")).Click();
			Assert.That(browser.Text, !Is.StringContaining("Выбор файла"));
			session.Refresh(report);
			Assert.That(report.Files.Count, Is.EqualTo(0));
		}

		[Test]
		public void Visit_every_report_type_configuration_page()
		{
			var types = ReportType.FindAll();
			Assert.That(types.Length, Is.GreaterThan(0), "данные для тестов не загружены, выполни bake PrepareLocal profile=reports");
			foreach (var type in types) {
				var report = Report.Queryable.FirstOrDefault(r => r.ReportType == type);
				if (report != null)
					CheckReport(report);
			}
		}

		private void CheckReport(Report report)
		{
			var url = String.Format("http://localhost:53759/Reports/ReportProperties.aspx?rp={0}&r={1}", report.Id, report.GeneralReport.Id);
			browser = Open(url);
			Assert.That(browser.Text, Is.StringContaining("Настройка параметров отчета"));
		}
	}
}
