using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord("report_property_values", Schema = "reports")]
	public class ReportPropertyValue : ActiveRecordBase<ReportPropertyValue>
	{
		[PrimaryKey]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual ulong ReportPropertyId { get; set; }

		[Property]
		public virtual string Value { get; set; }
	}
}