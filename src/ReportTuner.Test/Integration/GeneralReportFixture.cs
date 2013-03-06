using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Common.Schedule;
using NUnit.Framework;
using ReportTuner.Helpers;
using ReportTuner.Models;
using Test.Support;

namespace ReportTuner.Test.Integration
{
	[TestFixture]
	public class GeneralReportFixture : IntegrationFixture
	{
		[Test]
		public void Start_time_test()
		{
			session.CreateSQLQuery("delete from `logs`.reportexecutelogs;").ExecuteUpdate();
			Flush();
			var startTime = Reports_schedule.GetStartTime(session, 1);
			Assert.IsNullOrEmpty(startTime);
			session.Save(new ReportExecuteLog { StartTime = DateTime.Now, GeneralReportCode = 1 });
			startTime = Reports_schedule.GetStartTime(session, 1);
			Assert.AreEqual(startTime, string.Format("Отчет запущен {0}. ", DateTime.Now));
			session.Save(new ReportExecuteLog { StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1), GeneralReportCode = 1 });
			startTime = Reports_schedule.GetStartTime(session, 1);
			Assert.AreEqual(startTime, string.Format("Отчет запущен {0}. Среднее время выполнения: 60,0 минут", DateTime.Now));
		}

		[Test]
		public void Resend_report()
		{
			Global.Config.ReportHistoryPath = Path.Combine(ScheduleHelper.ScheduleWorkDir, "History");

			var payer = new Payer("Тестовый плательщик");
			var report1 = new GeneralReport(payer);
			var report2 = new GeneralReport(payer);
			session.Save(payer);
			session.Save(report1);
			session.Save(report2);

			var log1 = new ReportExecuteLog(report1);
			session.Save(log1);
			session.Save(new ReportLog(report1, log1) {
				LogTime = DateTime.Now.AddDays(-2)
			});
			var log2 = new ReportExecuteLog(report2);
			session.Save(log2);
			session.Save(new ReportLog(report2, log2));
			session.Flush();
			var content = log1.BuildTestFile();
			log2.BuildTestFile();

			report1.UnderTest = true;
			report1.ResendReport(session, new List<string> {
				"kvasovtest@analit.net"
			});
			var message = report1.Messages[0];
			var sendedContent = new StreamReader(message.Attachments[0].ContentStream).ReadToEnd();
			Assert.That(sendedContent, Is.EqualTo(content));
		}
	}
}
