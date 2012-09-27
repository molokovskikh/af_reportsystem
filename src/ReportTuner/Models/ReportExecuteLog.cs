using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord("ReportExecuteLogs", Schema = "logs")]
	public class ReportExecuteLog
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual ulong GeneralReportCode { get; set; }

		[Property]
		public virtual DateTime StartTime { get; set; }

		[Property]
		public virtual DateTime? EndTime { get; set; }
	}
}