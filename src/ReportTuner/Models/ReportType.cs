using System;
using System.Collections.Generic;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord("reports.reporttypes")]
	public class ReportType : ActiveRecordBase<ReportType>
	{
		[PrimaryKey("ReportTypeCode")]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual string ReportTypeName { get; set; }

		[Property]
		public virtual string AlternateSubject { get; set; }

		[Property]
		public virtual string ReportTypeFilePrefix { get; set; }
	}
}
