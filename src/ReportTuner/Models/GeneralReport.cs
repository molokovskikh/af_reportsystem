using System;
using System.Collections.Generic;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord("reports.general_reports")]
	public class GeneralReport : ActiveRecordBase<GeneralReport>
	{
		[PrimaryKey("GeneralReportCode")]
		public virtual ulong Id { get; set; }

		[BelongsTo("FirmCode")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual bool Allow { get; set; }

		[Property]
		public virtual string EMailSubject { get; set; }

		[BelongsTo(Column = "ContactGroupId")]
		public virtual ContactGroup ContactGroup { get; set; }

		[Property]
		public virtual bool Temporary { get; set; }

		[Property]
		public virtual DateTime? TemporaryCreationDate { get; set; }
	}
}
