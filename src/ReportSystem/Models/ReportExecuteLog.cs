using System;
using Castle.ActiveRecord;

namespace Inforoom.ReportSystem.Model
{
	[ActiveRecord("ReportExecuteLogs", Schema = "logs")]
	public class ReportExecuteLog
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual int GeneralReportCode { get; set; }

		[Property]
		public virtual DateTime StartTime { get; set; }

		[Property]
		public virtual DateTime? EndTime { get; set; }

		[Property]
		public virtual bool EndError { get; set; }
	}
}
