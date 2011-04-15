using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord("report_properties", Schema = "reports")]
	public class ReportProperty : ActiveRecordBase<ReportProperty>
	{
		[PrimaryKey("ID")]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual ulong ReportCode { get; set; }

		[BelongsTo("PropertyID")]
		public virtual ReportTypeProperty PropertyType { get; set; }

		[Property("PropertyValue")]
		public virtual string Value { get; set; }

		[HasMany(typeof(ReportPropertyValue), "ReportPropertyId", "reports.report_property_values")]
		public virtual IList<ReportPropertyValue> Values { get; set; }
	}
}
