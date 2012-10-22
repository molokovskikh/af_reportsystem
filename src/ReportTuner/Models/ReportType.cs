using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;

namespace ReportTuner.Models
{
	[ActiveRecord("reporttypes", Schema = "reports")]
	public class ReportType : ActiveRecordLinqBase<ReportType>
	{
		public ReportType()
		{
			Properties = new List<ReportTypeProperty>();
		}

		public ReportType(string name, string clazz)
			: this()
		{
			ReportTypeName = name;
			AlternateSubject = name;
			ReportClassName = clazz;
			ReportTypeFilePrefix = clazz.Split('.').Last();
		}

		[PrimaryKey("ReportTypeCode")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ReportTypeName { get; set; }

		[Property]
		public virtual string ReportClassName { get; set; }

		[Property]
		public virtual string AlternateSubject { get; set; }

		[Property]
		public virtual string ReportTypeFilePrefix { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public IList<ReportTypeProperty> Properties { get; set; }

		[OneToOne(PropertyRef = "ReportType")]
		public FileForReportType File { get; set; }

		public bool IsOrderReport
		{
			get
			{
				return new[] {
					"MixedReport",
					"OrderOutAllowedAssortment",
					"PharmacyMixedReport",
					"ProviderRatingReport",
					"RatingReport",
					"SupplierMarketShareByUser"
				}.Any(n => ReportClassName.EndsWith(n, StringComparison.InvariantCultureIgnoreCase));
			}
		}

		public void AddProperty(ReportTypeProperty property)
		{
			property.ReportType = this;
			Properties.Add(property);
		}

		public ReportTypeProperty GetProperty(string name)
		{
			return Properties.FirstOrDefault(p => p.PropertyName.ToLowerInvariant() == name.ToLowerInvariant());
		}

		public void FixExistReports()
		{
			var reports = Report.Queryable.Where(r => r.ReportType == this).ToList();
			foreach (var report in reports) {
				var propertyValues = report.Properties;
				Properties
					.Where(p => !p.Optional && !propertyValues.Any(pv => pv.PropertyType == p))
					.Select(p => new ReportProperty(report, p))
					.Each(p => p.Save());
			}
		}
	}
}
