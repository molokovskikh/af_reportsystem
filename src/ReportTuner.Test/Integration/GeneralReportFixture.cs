using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
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
			var startTime = Reports_schedule.GetStartTime(1);
			Assert.IsNullOrEmpty(startTime);
			session.Save(new ReportExecuteLog { StartTime = DateTime.Now, GeneralReportCode = 1 });
			startTime = Reports_schedule.GetStartTime(1);
			Assert.AreEqual(startTime, string.Format("Отчет запущен {0}. ", DateTime.Now));
			session.Save(new ReportExecuteLog { StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1), GeneralReportCode = 1 });
			startTime = Reports_schedule.GetStartTime(1);
			Assert.AreEqual(startTime, string.Format("Отчет запущен {0}. Среднее время выполнения: 60,0 минут", DateTime.Now));
		}
	}
}
