using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord("reports.report_type_properties")]
	public class ReportTypeProperty : ActiveRecordBase<ReportTypeProperty>
	{
		[PrimaryKey]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual ulong ReportTypeCode { get; set; }

		[Property]
		public virtual string PropertyName { get; set; }

		[Property]
		public virtual string DisplayName { get; set; }
	}
}
