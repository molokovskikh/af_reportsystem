using System;
using System.Collections.Generic;
using System.Linq;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Filters;
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
				{"Mnn", null}
			};
			var orderReport = new OrdersReport();
			var rootType = typeof(OrdersReport);
			//некоторые отчеты унаследованы от базового но на самом деле они не умеют использовать общие настройки
			var configurableReports = new [] { typeof(RatingReport), typeof(MixedReport), typeof(PharmacyMixedReport) };
			var types = rootType.Assembly.GetTypes()
				.Where(t => t != rootType && !t.IsAbstract && rootType.IsAssignableFrom(t) && configurableReports.Contains(t));
			foreach (var type in types) {
				Console.WriteLine(type.FullName);
				var reportType = session.Query<ReportType>().First(r => r.ReportClassName == type.FullName);
				var notExists = orderReport.registredField.SelectMany(f => new [] {
					f.reportPropertyPreffix + FilterField.PositionSuffix,
					f.reportPropertyPreffix + FilterField.NonEqualSuffix,
					f.reportPropertyPreffix + FilterField.EqualSuffix,
				}).Except(reportType.Properties.Select(p => p.PropertyName));
				foreach (var notExist in notExists) {
					if (notExist.EndsWith(FilterField.PositionSuffix)) {
						var field = orderReport.registredField.First(f => f.reportPropertyPreffix == notExist.Replace(FilterField.PositionSuffix, ""));
						reportType.AddProperty(new ReportTypeProperty(notExist, "INT", string.Format("Позиция \"{0}\" в отчете", field.outputCaption)) {
							Optional = true,
							DefaultValue = "0",
						});
					}
					else if (notExist.EndsWith(FilterField.NonEqualSuffix)) {
						AddListProperty(procedures, orderReport.registredField, reportType, notExist, FilterField.NonEqualSuffix, "Список исключений \"{0}\"");
					}
					else {
						AddListProperty(procedures, orderReport.registredField, reportType, notExist, FilterField.EqualSuffix, "Список значений \"{0}\"");
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