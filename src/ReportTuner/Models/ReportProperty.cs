using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace ReportTuner.Models
{
	[ActiveRecord("report_properties", Schema = "reports")]
	public class ReportProperty : ActiveRecordLinqBase<ReportProperty>
	{
		public ReportProperty()
		{}

		public ReportProperty(Report report, ReportTypeProperty property)
		{
			ReportCode = report.Id;
			PropertyType = property;
			Value = property.DefaultValue;
		}

		[PrimaryKey("ID")]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual ulong ReportCode { get; set; }

		[BelongsTo("PropertyID")]
		public virtual ReportTypeProperty PropertyType { get; set; }

		[Property("PropertyValue")]
		public virtual string Value { get; set; }

		[HasMany(typeof(ReportPropertyValue), "ReportPropertyId", "report_property_values", Schema = "reports")]
		public virtual IList<ReportPropertyValue> Values { get; set; }
	}
}
