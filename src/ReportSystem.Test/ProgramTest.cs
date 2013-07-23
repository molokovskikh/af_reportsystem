using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Schedule;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Model;
using Microsoft.Win32.TaskScheduler;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	[TestFixture]
	public class ProgramTest : IntegrationFixture
	{
		[Test]
		public void Base_test()
		{
			ScheduleHelper.SetTaskAction(1, "/gr:1 /manual:true");
			session.CreateSQLQuery("delete from `logs`.reportexecutelogs; update  reports.general_reports set allow = 0;").ExecuteUpdate();
			Close();
			Program.Main(new[] { "/gr:1" });
			var reportLogCount = session.Query<ReportExecuteLog>().Count();
			Assert.AreEqual(reportLogCount, 1);

			var taskService = ScheduleHelper.GetService();
			var reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
			var currentTask = ScheduleHelper.GetTask(taskService, reportsFolder, 1, "", "GR");
			Assert.That(((ExecAction)currentTask.Definition.Actions[0]).Arguments, Is.StringContaining("manual:true"));
		}
	}
}
