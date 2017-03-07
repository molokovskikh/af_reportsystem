using System;
using System.Linq;
using System.Threading;
using Common.Schedule;
using Microsoft.Win32.TaskScheduler;
using NUnit.Framework;
using ReportTuner.Models;
using WatiN.Core;
using System.Diagnostics;
using OpenQA.Selenium;
using Test.Support;
using Test.Support.Selenium;
using Test.Support.Web;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class OneShortReportFixture : SeleniumFixture
	{
		[Test]
		public void Task_shedule_base_test()
		{
			Open("Reports/schedule.aspx?r=1");
			Click("Выполнить задание");
			WaitForText("Отчет запущен ( № 1), ожидайте окончания выполнения операции.");
		}

		[Test]
		public void CurrentTaskStartReport()
		{
			Open("/Reports/schedule.aspx?r=50");
			Click("Выполнить задание");

			Thread.Sleep(500);

			var taskService = ScheduleHelper.GetService();
			var reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
			var currentTask = ScheduleHelper.GetTaskOrCreate(taskService, reportsFolder, 50, "", "GR");
			Assert.That(((ExecAction)currentTask.Definition.Actions[0]).Arguments, Does.Contain("manual:true"));
		}

		[Test]
		public void Set_shedule_month()
		{
			Open("/Reports/schedule.aspx?r=1");

			Css(".addMonthItem").Click();
			//Чекбоксы должны быть выбраны по-умолчанию, но на всякий случай оставляю код
			//browser.Div("firstSixMonth").ChildOfType<CheckBox>(box => !box.Checked).Checked = true;
			//browser.Div("firstFifteenDays").ChildOfType<CheckBox>(box => !box.Checked).Checked = true;
			Click("Применить");
			AssertText("Временной промежуток от 23:00 до 4:00 является недопустимым для времени выполнения отчета");
			var text = browser.FindElementsByCssSelector("input type=\"text\"").First(x => x.GetAttribute("value") == "0:00");
			text.Clear();
			text.SendKeys("10:00");
			Click("Применить");
			AssertNoText("Временной промежуток от 23:00 до 4:00 является недопустимым для времени выполнения отчета");
			AssertText("Задать расписание для отчета ");

			var taskService = ScheduleHelper.GetService();
			var reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
			var currentTask = ScheduleHelper.GetTaskOrCreate(taskService, reportsFolder, 1, "", "GR");
			Assert.That(currentTask.Definition.Settings.RestartCount == 3);
			Assert.That(currentTask.Definition.Settings.RestartInterval == new TimeSpan(0, 15, 0));
			Assert.That(currentTask.Definition.Settings.StartWhenAvailable);
			Click("deleteMonthItem");
			Click("Применить");
		}

		[Test]
		public void Send_ready_report()
		{
			var payer = new TestPayer("Тестовый плательщик");
			session.Save(payer);
			var report = new GeneralReport(session.Load<Payer>(payer.Id));
			session.Save(report);
			report.LastSuccess = DateTime.Now;
			var executelog = new ReportExecuteLog(report);
			session.Save(executelog);
			session.Save(new ReportLog(report, executelog));
			session.Flush();

			executelog.BuildTestFile();

			Open($"/Reports/schedule.aspx?r={report.Id}");
			var radio = browser.FindElementsByCssSelector("input[type=\"radio\"]").First(x => x.GetAttribute("Value") == "RadioMails");
			radio.Click();
			browser.FindElementById("mail_Text").Clear();
			Click("Выслать готовый");

			AssertText("Укажите получателя отчета !");
			browser.FindElementById("mail_Text").SendKeys("KvasovTest@analit.net");
			Click("Выслать готовый");
			AssertText("Файл отчета успешно отправлен");
		}

		[Test]
		public void Test_start_time_table()
		{
			var startTime = DateTime.Now;
			session.CreateSQLQuery("insert into `logs`.reportexecutelogs (generalreportcode, startTime, endTime) value (1, :startTime, :endTime)")
				.SetParameter("startTime", startTime)
				.SetParameter("endTime", DateTime.Now.AddHours(1))
				.ExecuteUpdate();

			Open("/Reports/schedule.aspx?r=1");
			AssertText("Статистика запусков отчета");
			AssertText(startTime.ToString());
		}

		[Test]
		public void Add_help_file_for_general_report()
		{
			var report = session.Get<GeneralReport>((ulong)1);
			report.Files.Clear();
			session.Save(report);
			Open("Reports/Reports.aspx?r=1");
			Click("Добавить файл");
			session.Refresh(report);
			Assert.That(report.Files.Count, Is.EqualTo(1));
			AssertText("Выбор файла");
			FlushAndCommit();
			Css(".deleteFileButton").Click();
			AssertNoText("Выбор файла");
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
			Open($"/Reports/ReportProperties.aspx?rp={report.Id}&r={report.GeneralReport.Id}");
			AssertText("Настройка параметров отчета");
			AssertNoText("Готовить по розничному сегменту");
		}
	}
}
