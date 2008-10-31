using System;
using System.Collections.Generic;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord("reports.reports")]
	public class Report : ActiveRecordBase<Report>
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
	}
}
