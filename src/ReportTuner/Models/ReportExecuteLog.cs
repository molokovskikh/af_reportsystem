using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using ReportTuner.Helpers;

namespace ReportTuner.Models
{
	[ActiveRecord("ReportExecuteLogs", Schema = "logs")]
	public class ReportExecuteLog
	{
		public ReportExecuteLog()
		{
		}

		public ReportExecuteLog(GeneralReport report)
		{
			GeneralReportCode = report.Id;
			StartTime = DateTime.Now;
			EndTime = DateTime.Now;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual ulong GeneralReportCode { get; set; }

		[Property]
		public virtual DateTime StartTime { get; set; }

		[Property]
		public virtual DateTime? EndTime { get; set; }

		public string BuildTestFile()
		{
			var ftpDirectory = Path.Combine(ScheduleHelper.ScheduleWorkDir, "History");
			if (!Directory.Exists(ftpDirectory))
				Directory.CreateDirectory(ftpDirectory);
			var contents = Guid.NewGuid().ToString();
			File.WriteAllText(Path.Combine(ftpDirectory, Id + ".txt"), contents);
			return contents;
		}
	}
}