using System;
using Common.Schedule;
using Microsoft.Win32.TaskScheduler;
using NUnit.Framework;
using ReportTuner.Models;

namespace ReportTuner.Test.Integration.Models
{
	[TestFixture]
	public class ReportFixture
	{
		[Test]
		public void Temporary_task_should_not_contains_triggers()
		{
			var service = ScheduleHelper.GetService();
			var folder = ScheduleHelper.GetReportsFolder(service);

			ScheduleHelper.DeleteTask(folder, 100, "GR");
			ScheduleHelper.DeleteTask(folder, 1, "temp");

			var task = ScheduleHelper.GetTaskOrCreate(service, folder, Convert.ToUInt64(100), "", "GR");
			ScheduleHelper.SetTaskEnableStatus(100, true, "GR");
			var definition = task.Definition;
			var trigger = new WeeklyTrigger {
				DaysOfWeek = DaysOfTheWeek.Friday,
				WeeksInterval = 1,
				StartBoundary = DateTime.Now
			};
			definition.Triggers.Add(trigger);
			ScheduleHelper.UpdateTaskDefinition(service, folder, Convert.ToUInt64(100), definition, "GR");

			task = ScheduleHelper.FindTask(service, folder, 100, "GR");
			Assert.That(task.Definition.Triggers.Count, Is.EqualTo(1));
			var temp = Report.CreateTemporaryTaskForRunFromInterface(service, folder, task, "cmd /c echo");
			Assert.That(temp.Definition.Triggers.Count, Is.EqualTo(0));
		}
	}
}