using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Common.Tools;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOffers;
using Inforoom.ReportSystem.ByOrders;
using Inforoom.ReportSystem.Filters;
using Inforoom.ReportSystem.Model;
using Inforoom.ReportSystem.Models.Reports;
using log4net;
using NHibernate;
using NHibernate.Linq;

namespace ReportTuner.Models
{
	public class UpdateReportConfig
	{
		private ISession session;
		private static ILog log = LogManager.GetLogger(typeof(UpdateReportConfig));

		public UpdateReportConfig(ISession session)
		{
			this.session = session;
		}

		public void Execute()
		{
			var procedures = new Dictionary<string, string> {
				{"Payer", "GetPayerCode"},
				{"Mnn", null},
				{"Region", "GetRegion"},
				{"ProductName", "GetProductId"},
				{"FullName", "GetFullCode"},
				{"ShortName", "GetShortCode"},
				{"FirmCr", "GetFirmCr"},
				{"FirmCode", "GetFirmCode"},
				{"ClientCode", "GetAllClientCode"},
				{"Addresses", "GetFirmCode"},
				{"SupplierId", "GetFirmCode"},
				{"UserId", "GetUser"}
			};
			var rootType = typeof(BaseReport);
			//некоторые отчеты унаследованы от базового но на самом деле они не умеют использовать общие настройки
			var configurableReports = new[] {
				typeof(RatingReport),
				typeof(MixedReport),
				typeof(PharmacyMixedReport),
				typeof(OrdersStatistics),
				typeof(WaybillsStatReport),
				typeof(OffersExport),
				typeof(OrderDetails),
				typeof(SpecReport)
			};
			var types = rootType.Assembly.GetTypes()
				.Where(t => t != rootType && !t.IsAbstract && rootType.IsAssignableFrom(t) && configurableReports.Contains(t));
			foreach (var type in types) {
				var reportType = session.Query<ReportType>().FirstOrDefault(r => r.ReportClassName == type.FullName)
					?? new ReportType(type);

				if (typeof(OrdersReport).IsAssignableFrom(type)) {
					var reportInstance = new OrdersReport();
					if (type.GetConstructor(new Type[0]) != null)
						reportInstance = (OrdersReport)Activator.CreateInstance(type);
					var notExists = reportInstance.RegistredField.SelectMany(f => new[] {
						f.reportPropertyPreffix + FilterField.PositionSuffix,
						f.reportPropertyPreffix + FilterField.NonEqualSuffix,
						f.reportPropertyPreffix + FilterField.EqualSuffix,
					}).Except(reportType.Properties.Select(p => p.PropertyName));
					if (reportType.RestrictedFields.Any())
						notExists = notExists.Intersect(reportType.RestrictedFields);
					notExists = notExists.Except(reportType.BlockedFields);
					foreach (var notExist in notExists) {
						if (notExist.EndsWith(FilterField.PositionSuffix)) {
							var field = reportInstance.RegistredField
								.First(f => f.reportPropertyPreffix == notExist.Replace(FilterField.PositionSuffix, ""));
							var property = new ReportTypeProperty(notExist, "INT", $"Позиция \"{field.outputCaption}\" в отчете") {
								Optional = true,
								DefaultValue = "0",
							};
							log.WarnFormat("Добавил опциональный параметр '{0}' для отчета '{1}'",
								property.DisplayName,
								reportType.ReportTypeName);
							reportType.AddProperty(property);
						}
						else if (notExist.EndsWith(FilterField.NonEqualSuffix)) {
							var property = AddListProperty(procedures, reportInstance.RegistredField, reportType, notExist,
								FilterField.NonEqualSuffix, "Список исключений \"{0}\"");
							log.WarnFormat("Добавил опциональный параметр '{0}' для отчета '{1}'",
								property.DisplayName,
								reportType.ReportTypeName);
						}
						else {
							var property = AddListProperty(procedures, reportInstance.RegistredField, reportType, notExist,
								FilterField.EqualSuffix, "Список значений \"{0}\"");
							log.WarnFormat("Добавил опциональный параметр '{0}' для отчета {1}",
								property.DisplayName,
								reportType.ReportTypeName);
						}
					}
				}

				CheckProperties(type, procedures, reportType);

				session.Save(reportType);
			}
			CheckProperties(typeof(SupplierMarketShareByUser), procedures);
		}

		private void CheckProperties(Type type, Dictionary<string, string> procedures, ReportType reportType = null)
		{
			reportType = reportType ?? session.Query<ReportType>().FirstOrDefault(r => r.ReportClassName == type.FullName);
			type.GetProperties().Each(t => { CheckProperty(type, t.Name, t.PropertyType, t, reportType, procedures); });
			var blacklist = new string[0];
			if (type == typeof(PharmacyMixedReport)) {
				blacklist = new[] { "HideSupplierStat" };
			}
			type.GetFields().Where(f => !blacklist.Contains(f.Name))
				.Each(f => CheckProperty(type, f.Name, f.FieldType, f, reportType, procedures));
		}

		private static void CheckProperty(Type reportType, string name, Type type, ICustomAttributeProvider typeProperty,
			ReportType reportTypeModel, Dictionary<string, string> procedures)
		{
			var attributes = typeProperty.GetCustomAttributes(typeof(DescriptionAttribute), true);
			if (attributes.Length == 0)
				return;
			var desc = ((DescriptionAttribute)attributes[0]).Description;

			var prop = reportTypeModel.Properties.FirstOrDefault(p => p.PropertyName.Match(name));
			var optional = false;
			if (prop == null) {
				var localType = "";
				var defaultValue = "0";
				if (type == typeof(bool)) {
					localType = "BOOL";
				}
				else if (type == typeof(int) || type == typeof(uint))
					localType = "INT";
				else if (type == typeof(int?)) {
					localType = "INT";
					optional = true;
				} else if (type.IsEnum)
					localType = "ENUM";
				else
					throw new Exception($"Не знаю как преобразовать тип {type} свойства {name} типа {type}");
				try {
					var report = Activator.CreateInstance(reportType);
					var field = typeProperty as FieldInfo;
					if (field != null) {
						defaultValue = Convert.ToInt32(field.GetValue(report)).ToString();
					}
					var property = typeProperty as PropertyInfo;
					if (property != null)
						defaultValue = Convert.ToInt32(property.GetValue(report, null)).ToString();
				}
				catch (Exception e) {
					//не реализовано используем значение по умолчанию
				}
				var reportTypeProperty = new ReportTypeProperty(name, localType, desc) {
					Optional = optional,
					DefaultValue = defaultValue,
					SelectStoredProcedure = procedures.GetValueOrDefault(name)
				};
				if (type.IsEnum) {
					foreach (var value in Enum.GetValues(type)) {
						var valueName = type.GetMember(value.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description;
						reportTypeProperty.Enum.AddValue(valueName, (int)value);
					}
				}
				reportTypeModel.AddProperty(reportTypeProperty);
				log.Warn($"Добавил параметр '{reportTypeProperty.DisplayName}' для отчета {reportTypeModel.ReportTypeName}");
			}
		}

		private static ReportTypeProperty AddListProperty(Dictionary<string, string> procedures,
			List<FilterField> fields, ReportType reportType, string property, string sufix, string label)
		{
			var prefix = property.Replace(sufix, "");
			var field = fields.First(f => f.reportPropertyPreffix == prefix);
			if (!procedures.ContainsKey(prefix))
				throw new Exception($"Не задана процедура {prefix} для отчета {reportType.ReportClassName}");
			var reportTypeProperty = new ReportTypeProperty(property, "LIST", string.Format(label, field.outputCaption)) {
				Optional = true,
				DefaultValue = "0",
				SelectStoredProcedure = procedures[prefix]
			};
			reportType.AddProperty(reportTypeProperty);
			return reportTypeProperty;
		}
	}
}