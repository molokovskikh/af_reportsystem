using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using log4net;

namespace ReportTuner.Models
{
	[ActiveRecord("report_properties", Schema = "reports")]
	public class ReportProperty : ActiveRecordLinqBase<ReportProperty>
	{
		public ReportProperty()
		{}

		public ReportProperty(Report report, ReportTypeProperty property)
		{
			Report = report;
			PropertyType = property;
			Value = property.DefaultValue;
		}

		[PrimaryKey("ID")]
		public virtual ulong Id { get; set; }

		[BelongsTo("ReportCode")]
		public virtual Report Report { get; set; }

		[BelongsTo("PropertyID")]
		public virtual ReportTypeProperty PropertyType { get; set; }

		[Property("PropertyValue")]
		public virtual string Value { get; set; }

		[HasMany(typeof(ReportPropertyValue), "ReportPropertyId", "report_property_values", Schema = "reports")]
		public virtual IList<ReportPropertyValue> Values { get; set; }

		public string Filename
		{
			get { return Path.Combine(Global.Config.SavedFilesPath, Id.ToString()); }
		}

		public void CleanupFiles()
		{
			if (PropertyType.PropertyType != "FILE")
				return;

			try {
				if (File.Exists(Filename))
					File.Delete(Filename);
			}
			catch (Exception e) {
				var log = LogManager.GetLogger(typeof(ReportProperty));
				log.Error(String.Format("Ошибка при удалении файла {0}", Filename), e);
			}
		}

		public bool IsSupplierEditor()
		{
			return (Report.ReportType.ReportTypeFilePrefix != "PharmacyMixed" && IsClientOrSupplierEdit());
		}

		private bool IsClientOrSupplierEdit()
		{
			return PropertyType.PropertyName.ToLower().StartsWith("BusinessRivals".ToLower())
				|| String.Equals(PropertyType.PropertyName, "IgnoredSuppliers", StringComparison.InvariantCultureIgnoreCase)
				|| String.Equals(PropertyType.PropertyName, "FirmCodeEqual", StringComparison.InvariantCultureIgnoreCase)
				|| String.Equals(PropertyType.PropertyName, "suppliers", StringComparison.InvariantCultureIgnoreCase);
		}

		public bool IsClientEditor()
		{
			return (Report.ReportType.ReportTypeFilePrefix == "PharmacyMixed" && IsClientOrSupplierEdit())
				|| String.Equals(PropertyType.PropertyName, "ClientCodeEqual", StringComparison.InvariantCultureIgnoreCase);
		}

		public bool IsAddressesEditor()
		{
			return (Report.ReportType.ReportTypeFilePrefix == "Rating" || 
				Report.ReportType.ReportTypeFilePrefix == "Mixed" ||
				Report.ReportType.ReportTypeFilePrefix == "PharmacyMixed") && 
				(String.Equals(PropertyType.PropertyName, "AddressesList", StringComparison.InvariantCultureIgnoreCase) ||
				String.Equals(PropertyType.PropertyName, "AddressesNonList", StringComparison.InvariantCultureIgnoreCase));
		}
	}
}
