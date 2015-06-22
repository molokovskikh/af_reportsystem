using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Schedule;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Model;
using log4net.Config;
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
			var report = new GeneralReport();
			session.Save(report);
			var helper = new ScheduleHelper();
			helper.GetTaskOrCreate(report.Id);
			ScheduleHelper.SetTaskAction(report.Id, String.Format("/gr:{0} /manual:true", report.Id));

			session.CreateSQLQuery("delete from `logs`.reportexecutelogs; update  reports.general_reports set allow = 0;").ExecuteUpdate();
			session.Transaction.Commit();
			Program.Main(new[] { String.Format("/gr:{0}", report.Id) });
			var reportLogCount = session.Query<ReportExecuteLog>().Count();
			Assert.AreEqual(reportLogCount, 1);

			var currentTask = helper.FindTask(report.Id);
			Assert.That(((ExecAction)currentTask.Definition.Actions[0]).Arguments, Is.StringContaining("manual:true"));
		}
	}
}
