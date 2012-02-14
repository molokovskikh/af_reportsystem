using System;
using System.Collections.Generic;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Models;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;

namespace ReportTuner.Models
{
	[ActiveRecord("reports", Schema = "reports")]
	public class Report : ActiveRecordLinqBase<Report>
	{
		[PrimaryKey("ReportCode")]
		public virtual ulong Id { get; set; }

		[BelongsTo("GeneralReportCode")]
		public virtual GeneralReport GeneralReport { get; set; }

		[BelongsTo("ReportTypeCode")]
		public virtual ReportType ReportType { get; set; }

		[Property]
		public virtual string ReportCaption { get; set; }

		[Property]
		public virtual bool Enabled { get; set; }

		[HasMany]
		public virtual IList<ReportProperty> Properties { get; set; }

		public static Task CreateTemporaryTaskForRunFromInterface(TaskService service, TaskFolder folder, Task source, string action)
		{
			var task = ScheduleHelper.GetTask(service, folder, Convert.ToUInt64(1), "tempTask1", "temp");
			var sourceDefinition = source.Definition;
			sourceDefinition.Triggers.Clear();
			ScheduleHelper.UpdateTaskDefinition(service, folder, Convert.ToUInt64(1), sourceDefinition, "temp");
			ScheduleHelper.SetTaskEnableStatus(1, true, "temp");
			var definition = task.Definition;
			var newAction = new ExecAction(ScheduleHelper.ScheduleAppPath, action, ScheduleHelper.ScheduleWorkDir);
			definition.Actions.RemoveAt(0);
			definition.Actions.Add(newAction);
			ScheduleHelper.UpdateTaskDefinition(service, folder, Convert.ToUInt64(1), definition, "temp");
			return task;
		}
	}
}
