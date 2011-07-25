﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord("reports.reporttypes")]
	public class ReportType : ActiveRecordBase<ReportType>
	{
		public ReportType()
		{
			Properties = new List<ReportTypeProperty>();
		}

		[PrimaryKey("ReportTypeCode")]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual string ReportTypeName { get; set; }

		[Property]
		public virtual string AlternateSubject { get; set; }

		[Property]
		public virtual string ReportTypeFilePrefix { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public IList<ReportTypeProperty> Properties { get; set; }

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
			foreach (var report in reports)
			{
				var propertyValues = ReportProperty.Queryable.Where(p => p.ReportCode == report.Id);
				Properties
					.Where(p => !p.Optional && !propertyValues.Any(pv => pv.PropertyType == p))
					.Select(p => new ReportProperty(report, p))
					.Each(p => p.Save());
			}
		}
	}
}
