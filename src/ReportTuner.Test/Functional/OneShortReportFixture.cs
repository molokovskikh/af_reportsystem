﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ReportTuner.Helpers;
using ReportTuner.Models;
using WatiN.Core;
using System.Diagnostics;
using Test.Support.Web;
using WatiN.Core.Native.Windows;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class OneShortReportFixture : WatinFixture2
	{
		[Test, Ignore]
		public void TestOneShortReport()
		{
			using (var browser = new IE("http://localhost:53759/Reports/GeneralReports.aspx")) {
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
		public void Task_shedule_base_test()
		{
			Open("http://localhost:53759/Reports/schedule.aspx?r=1");
			Click("Выполнить задание");
			Thread.Sleep(3000);
			AssertText("Отчет запущен, ожидайте окончания выполнения операции.");
		}

		[Test]
		public void Set_shedule_month()
		{
			using (var browser = new IE("http://localhost:53759/Reports/schedule.aspx?r=1")) {
				browser.Button(Find.ByClass("addMonthItem")).Click();
				browser.Div("firstSixMonth").ChildOfType<CheckBox>(box => !box.Checked).Checked = true;
				browser.Div("firstFifteenDays").ChildOfType<CheckBox>(box => !box.Checked).Checked = true;
				browser.Button(Find.ByValue("Применить")).Click();
				Assert.That(browser.Text, Is.StringContaining("Временной промежуток от 23:00 до 4:00 является недопустимым для времени выполнения отчета"));
				browser.TextField(Find.ByValue("0:00")).Value = "10:00";
				browser.Button(Find.ByValue("Применить")).Click();
				Assert.That(browser.Text, Is.Not.StringContaining("Временной промежуток от 23:00 до 4:00 является недопустимым для времени выполнения отчета"));
				Assert.That(browser.Text, Is.StringContaining("Задать расписание для отчета "));

				var taskService = ScheduleHelper.GetService();
				var reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
				var currentTask = ScheduleHelper.GetTask(taskService, reportsFolder, (ulong)1, "", "GR");
				Assert.That(currentTask.Definition.Settings.RestartCount == 3);
				Assert.That(currentTask.Definition.Settings.RestartInterval == new TimeSpan(0, 15, 0));
				Assert.That(currentTask.Definition.Settings.StartWhenAvailable == true);
				browser.Button(Find.ByClass("deleteMonthItem")).Click();
				browser.Button(Find.ByValue("Применить")).Click();
			}
		}

		[Test]
		public void Send_ready_report()
		{
			var generalReport = GeneralReport.Find(Convert.ToUInt64(1));
			var ftpDirectory = Path.Combine(ScheduleHelper.ScheduleWorkDir, "OptBox", generalReport.FirmCode.Value.ToString("000"), "Reports");
			if (!Directory.Exists(ftpDirectory))
				Directory.CreateDirectory(ftpDirectory);
			foreach (var file in Directory.GetFiles(ftpDirectory)) {
				File.Delete(file);
			}
			if(generalReport.NoArchive)
				File.WriteAllText(Path.Combine(ftpDirectory, generalReport.ReportFileName), Guid.NewGuid().ToString());
			else {
				File.WriteAllText(Path.Combine(ftpDirectory, generalReport.ReportArchName), Guid.NewGuid().ToString());
			}
			using (var browser = new IE("http://localhost:53759/Reports/schedule.aspx?r=1")) {
				browser.RadioButton(Find.ByValue("RadioMails")).Checked = true;
				browser.TextField("mail_Text").Clear();
				browser.Button(Find.ByValue("Выслать готовый")).Click();
				if(generalReport.Format != "DBF" && generalReport.FirmCode != null)
					Assert.That(browser.Text, Is.StringContaining("Укажите получателя отчета !"));
				browser.TextField("mail_Text").AppendText("KvasovTest@analit.net");
				browser.Button(Find.ByValue("Выслать готовый")).Click();
				if(generalReport.Format != "DBF" && generalReport.FirmCode != null)
					Assert.That(browser.Text, Is.StringContaining("Файл отчета успешно отправлен"));
			}
		}

		[Test]
		public void Test_start_time_table()
		{
			var startTime = DateTime.Now;
			session.CreateSQLQuery("insert into `logs`.reportexecutelogs (generalreportcode, startTime, endTime) value (1, :startTime, :endTime)")
				.SetParameter("startTime", startTime)
				.SetParameter("endTime", DateTime.Now.AddHours(1))
				.ExecuteUpdate();
			session.Flush();
			browser = Open("http://localhost:53759/Reports/schedule.aspx?r=1");
			Assert.That(browser.Text, Is.StringContaining("Статистика запусков отчета"));
			Assert.That(browser.Text, Is.StringContaining(startTime.ToString()));
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
			Assert.That(browser.Text, Is.Not.Contains("Готовить по розничному сегменту"));
		}
	}
}
