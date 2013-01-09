using System;
using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord(Schema = "Logs", Table = "ReportsLogs")]
	public class ReportLog
	{
		public ReportLog()
		{
		}

		public ReportLog(GeneralReport report, ReportExecuteLog executelog = null)
		{
			Report = report;
			LogTime = DateTime.Now;
			MessageID = "123";
			Result = executelog;
		}

		[PrimaryKey("RowId")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime LogTime { get; set; }

		[Property]
		public int SmtpId { get; set; }

		[Property]
		public string MessageID { get; set; }

		[Property]
		public virtual string Email { get; set; }

		[BelongsTo("ResultId")]
		public virtual ReportExecuteLog Result { get; set; }

		[BelongsTo("GeneralReportCode")]
		public virtual GeneralReport Report { get; set; }
	}
}