using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common.Tools;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using Inforoom.ReportSystem.Filters;
using Inforoom.ReportSystem.Model;
using log4net;
using NHibernate;
using NHibernate.Linq;

namespace ReportTuner.Models
{
	public class UpdateReportConfig
	{
		private ISession session;
		private ILog log = LogManager.GetLogger(typeof(UpdateReportConfig));

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
			//��������� ������ ������������ �� �������� �� �� ����� ���� ��� �� ����� ������������ ����� ���������
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
				notExists = notExists.Except(reportType.BlockedFields);

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
							throw new Exception(String.Format("�� ���� ��� ������������� ��� {0} �������� {1} ���� {2}",
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
						var property = new ReportTypeProperty(notExist, "INT", string.Format("������� \"{0}\" � ������", field.outputCaption)) {
							Optional = true,
							DefaultValue = "0",
						};
						log.WarnFormat("������� ������������ �������� '{0}' ��� ������ '{1}'",
							property.DisplayName,
							reportType.ReportTypeName);
						reportType.AddProperty(property);
					}
					else if (notExist.EndsWith(FilterField.NonEqualSuffix)) {
						var property = AddListProperty(procedures, reportInstance.registredField, reportType, notExist, FilterField.NonEqualSuffix, "������ ���������� \"{0}\"");
						log.WarnFormat("������� ������������ �������� '{0}' ��� ������ '{1}'",
							property.DisplayName,
							reportType.ReportTypeName);
					}
					else {
						var property = AddListProperty(procedures, reportInstance.registredField, reportType, notExist, FilterField.EqualSuffix, "������ �������� \"{0}\"");
						log.WarnFormat("������� ������������ �������� '{0}' ��� ������ {1}",
							property.DisplayName,
							reportType.ReportTypeName);
					}
				}

				session.Save(reportType);
			}
		}

		private static ReportTypeProperty AddListProperty(Dictionary<string, string> procedures, List<FilterField> fields, ReportType reportType, string property, string sufix, string label)
		{
			var prefix = property.Replace(sufix, "");
			var field = fields.First(f => f.reportPropertyPreffix == prefix);
			if (!procedures.ContainsKey(prefix))
				throw new Exception(String.Format("�� ������ ��������� {0}", prefix));
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