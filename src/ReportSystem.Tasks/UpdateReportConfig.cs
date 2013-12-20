using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Common.Tools;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using Inforoom.ReportSystem.Filters;
using Inforoom.ReportSystem.Model;
using NHibernate;
using NHibernate.Linq;
using ReportTuner.Models;

namespace ReportSysmte.Tasks
{
	public class UpdateReportConfig
	{
		private ISession session;

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
				{"SupplierId", "GetFirmCode"}
			};
			var orderReport = new OrdersReport();
			var rootType = typeof(OrdersReport);
			//некоторые отчеты унаследованы от базового но на самом деле они не умеют использовать общие настройки
			var configurableReports = new [] {
				typeof(RatingReport), typeof(MixedReport), typeof(PharmacyMixedReport), typeof(OrdersStatistics),
				typeof(WaybillsStatReport)
			};
			var types = rootType.Assembly.GetTypes()
				.Where(t => t != rootType && !t.IsAbstract && rootType.IsAssignableFrom(t) && configurableReports.Contains(t));
			foreach (var type in types) {
				var reportType = session.Query<ReportType>().FirstOrDefault(r => r.ReportClassName == type.FullName)
					?? new ReportType(type);
				var reportInstance = orderReport;
				if (typeof(OrdersReport).IsAssignableFrom(type)
					&& type.GetConstructor(new Type[0]) != null)
					reportInstance = (OrdersReport)Activator.CreateInstance(type);
				var notExists = reportInstance.registredField.SelectMany(f => new [] {
					f.reportPropertyPreffix + FilterField.PositionSuffix,
					f.reportPropertyPreffix + FilterField.NonEqualSuffix,
					f.reportPropertyPreffix + FilterField.EqualSuffix,
				}).Except(reportType.Properties.Select(p => p.PropertyName));
				if (reportType.RestrictedFields.Any())
					notExists = notExists.Intersect(reportType.RestrictedFields);

				foreach (var typeProperty in type.GetProperties()) {
					var attributes = typeProperty.GetCustomAttributes(typeof(DescriptionAttribute), true);
					if (attributes.Length == 0)
						continue;
					var desc = ((DescriptionAttribute)attributes[0]).Description;
					var prop = reportType.Properties.FirstOrDefault(p => p.PropertyName.Match(typeProperty.Name));
					if (prop == null) {
						var localType = "";
						if (typeProperty.PropertyType == typeof(bool))
							localType = "BOOL";
						else if (typeProperty.PropertyType == typeof(int))
							localType = "INT";
						else
							throw new Exception(String.Format("Не знаю как преобразовать тип {0} свойства {1} типа {2}",
								typeProperty.PropertyType,
								typeProperty.Name,
								type));
						reportType.AddProperty(new ReportTypeProperty(typeProperty.Name, localType, desc) {
							Optional = false,
							DefaultValue = "0",
							SelectStoredProcedure = procedures.GetValueOrDefault(typeProperty.Name)
						});
					}
				}

				foreach (var notExist in notExists) {
					if (notExist.EndsWith(FilterField.PositionSuffix)) {
						var field = reportInstance.registredField.First(f => f.reportPropertyPreffix == notExist.Replace(FilterField.PositionSuffix, ""));
						reportType.AddProperty(new ReportTypeProperty(notExist, "INT", string.Format("Позиция \"{0}\" в отчете", field.outputCaption)) {
							Optional = true,
							DefaultValue = "0",
						});
					}
					else if (notExist.EndsWith(FilterField.NonEqualSuffix)) {
						AddListProperty(procedures, reportInstance.registredField, reportType, notExist, FilterField.NonEqualSuffix, "Список исключений \"{0}\"");
					}
					else {
						AddListProperty(procedures, reportInstance.registredField, reportType, notExist, FilterField.EqualSuffix, "Список значений \"{0}\"");
					}
				}

				session.Save(reportType);
			}
		}

		private static void AddListProperty(Dictionary<string, string> procedures, List<FilterField> fields, ReportType reportType, string property, string sufix, string label)
		{
			var prefix = property.Replace(sufix, "");
			var field = fields.First(f => f.reportPropertyPreffix == prefix);
			if (!procedures.ContainsKey(prefix))
				throw new Exception(String.Format("Не задана процедура {0}", prefix));
			reportType.AddProperty(new ReportTypeProperty(property, "LIST", string.Format(label, field.outputCaption)) {
				Optional = true,
				DefaultValue = "0",
				SelectStoredProcedure = procedures[prefix]
			});
		}
	}
}