using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Data;
using ReportTuner.Models;

namespace ReportTuner.Test
{
	[TestFixture(Description = "Класс для выполнения действий с отчетами в командном режиме")]
	public class ReportsBatch
	{
		//Необходимо скорректировать ReportHelper.CopyReportProperties, чтобы он не использовал ActiveRecord,
		//т.к. тяжело ее подключать в тесте
		internal void CopyReportProperties(ulong sourceReportId, ulong destinationReportId)
		{
			DataSet dsReportProperties = MySqlHelper.ExecuteDataset(
				FixtureSetup.ConnectionString,
				@"
SELECT 
rp.*,
rtp.PropertyType
FROM
  reports.report_properties rp,
  reports.report_type_properties rtp
where
	rp.ReportCode = ?SourceReportId
and rtp.Id = rp.PropertyId
order by rp.PropertyID;
SELECT
rpv.*
FROM
  reports.report_properties rp,
  reports.report_property_values rpv
where
	rp.ReportCode = ?SourceReportId
and rpv.ReportPropertyId = rp.Id;
SELECT 
rp.*,
rtp.PropertyType
FROM
  reports.report_properties rp,
  reports.report_type_properties rtp
where
	rp.ReportCode = ?DestinationReportId
and rtp.Id = rp.PropertyId
order by rp.PropertyID;
",
				new MySqlParameter("?SourceReportId", sourceReportId),
				new MySqlParameter("?DestinationReportId", destinationReportId));

			DataTable dtSourceProperties = dsReportProperties.Tables[0];
			DataTable dtSourcePropertiesValues = dsReportProperties.Tables[1];
			DataTable dtDestinationProperties = dsReportProperties.Tables[2];

			StringBuilder sbCommand = new StringBuilder();

			foreach (DataRow drSourceProperty in dtSourceProperties.Rows) {
				DataRow[] drDestinationProperties = dtDestinationProperties.Select("PropertyId = " + drSourceProperty["PropertyId"]);
				if (drDestinationProperties.Length == 0) {
					//Свойство не существует, поэтому просто вставляем новое
					sbCommand.AppendFormat("insert into reports.report_properties (ReportCode, PropertyId, PropertyValue) values ({0}, {1}, '{2}');\r\n",
						destinationReportId, drSourceProperty["PropertyId"], drSourceProperty["PropertyValue"]);
					if (drSourceProperty["PropertyType"].ToString().Equals("LIST", StringComparison.OrdinalIgnoreCase)) {
						sbCommand.AppendLine("set @LastReportPropertyId = last_insert_id();");
						foreach (DataRow drSourcePropertiesValue in dtSourcePropertiesValues.Select("ReportPropertyId = " + drSourceProperty["Id"])) {
							sbCommand.AppendFormat("insert into reports.report_property_values (ReportPropertyId, Value) values (@LastReportPropertyId, '{0}');\r\n",
								drSourcePropertiesValue["Value"]);
						}
					}
				}
				else {
					//Свойство существует, поэтому обновляем запись
					sbCommand.AppendFormat("update reports.report_properties set PropertyValue = '{0}' where Id = {1};\r\n",
						drSourceProperty["PropertyValue"], drDestinationProperties[0]["Id"]);

					if (drSourceProperty["PropertyType"].ToString().Equals("LIST", StringComparison.OrdinalIgnoreCase)) {
						sbCommand.AppendFormat("delete from reports.report_property_values where ReportPropertyId = {0};\r\n", drDestinationProperties[0]["Id"]);
						foreach (DataRow drSourcePropertiesValue in dtSourcePropertiesValues.Select("ReportPropertyId = " + drSourceProperty["Id"])) {
							sbCommand.AppendFormat("insert into reports.report_property_values (ReportPropertyId, Value) values ({0}, '{1}');\r\n",
								drDestinationProperties[0]["Id"], drSourcePropertiesValue["Value"]);
						}
					}
				}
			}

			MySqlConnection connection = new MySqlConnection(FixtureSetup.ConnectionString);
			connection.Open();
			try {
				MySqlTransaction transaction = connection.BeginTransaction();
				MySqlHelper.ExecuteNonQuery(connection, sbCommand.ToString());
				transaction.Commit();
			}
			finally {
				connection.Close();
			}
		}

		[Test(Description = "создает отчеты по подобию определенного отчета с копированием всех свойств, меняя название заголовка отчета как '2', '3' и т.д."),
		 Ignore("это не тест, а метод для выполнения действий с отчетами")]
		public void CloneReportsFromSourceReport()
		{
			ulong sourceReportId = 585;
			var newReportList = new List<ulong>();

			using (var conn = new MySqlConnection(FixtureSetup.ConnectionString)) {
				conn.Open();

				var command = new MySqlCommand(
					@"insert into reports.reports 
						 (GeneralReportCode, ReportCaption, ReportTypeCode, Enabled)
					  select 
						 GeneralReportCode, ?ReportCaption, ReportTypeCode, Enabled
						from reports.reports
					   where ReportCode = ?reportCode;
					 select last_insert_id() as ReportCode;", conn);
				command.Parameters.AddWithValue("?reportCode", sourceReportId);
				command.Parameters.Add("?ReportCaption", MySqlDbType.String);

				for (int i = 5; i <= 55; i++) {
					command.Parameters["?ReportCaption"].Value = i.ToString();
					newReportList.Add(Convert.ToUInt64(command.ExecuteScalar()));
				}
				conn.Close();
			}

			foreach (var destinationReportId in newReportList)
				CopyReportProperties(sourceReportId, destinationReportId);
		}

		//Копирует все отчеты из родительского отчета sourceGeneralReportId в родительский отчет destinationGeneralReportId,
		//если в родительском отчете destinationGeneralReportId есть отчеты, то перед копирование происходит их удаление
		private void CopyReports(ulong sourceGeneralReportId, ulong destinationGeneralReportId)
		{
			var newReportList = new List<ulong>();

			using (var connection = new MySqlConnection(FixtureSetup.ConnectionString)) {
				connection.Open();

				var templateReportDS = MySqlHelper.ExecuteDataset(
					FixtureSetup.ConnectionString,
					@"
select
  reports.ReportCode
from
  reports.General_Reports,
  reports.reports
where
	General_Reports.GeneralReportCode = ?GeneralReportCode
and General_Reports.GeneralReportCode = reports.GeneralReportCode
",
				new MySqlParameter("?GeneralReportCode", sourceGeneralReportId));

				var templateReports = templateReportDS.Tables[0];

				Console.WriteLine("type = {0}", templateReports.Rows.Count);

				var deletedReports = MySqlHelper.ExecuteNonQuery(
					connection,
					"delete from reports.reports where GeneralReportCode = ?GeneralReportCode",
					new MySqlParameter("?GeneralReportCode", destinationGeneralReportId));

				if (deletedReports > 0)
					Console.WriteLine("For report {0} deleted reports: {1}", destinationGeneralReportId, deletedReports);

				var insertReportCommand = new MySqlCommand(
					@"
insert into reports.reports 
  (GeneralReportCode, ReportCaption, ReportTypeCode, Enabled)
select 
  ?GeneralReportCode, reports.ReportCaption, reports.ReportTypeCode, 1
from 
  reports.reports 
where reports.ReportCode = ?reportCode;
select last_insert_id() as ReportCode;", connection);
				insertReportCommand.Parameters.Add("?reportCode", MySqlDbType.UInt64);
				insertReportCommand.Parameters.AddWithValue("?GeneralReportCode", destinationGeneralReportId);

				foreach (DataRow templateReport in templateReports.Rows) {
					var templateReportId = Convert.ToUInt64(templateReport["ReportCode"]);
					insertReportCommand.Parameters["?reportCode"].Value = templateReportId;
					var newReportCode = Convert.ToUInt64(insertReportCommand.ExecuteScalar());
					newReportList.Add(newReportCode);
					CopyReportProperties(templateReportId, newReportCode);
				}
			}
		}

		[Test(Description = "создает отчеты у родительского отчета 213 по подобию отчетов для родительского отчета 210 с копированием всех свойств, задача пришла от Павла"),
			Ignore("это не тест, а метод для выполнения действий с отчетами")
		]
		public void CloneReportsToDestination()
		{
			//213, 216 как 210
			CopyReports(210, 213);
			CopyReports(210, 216);

			//214, 217 как 211
			CopyReports(211, 214);
			CopyReports(211, 217);

			//215, 218 как 212
			CopyReports(212, 215);
			CopyReports(212, 218);

			//299 305 311 317 как 249
			CopyReports(249, 299);
			CopyReports(249, 305);
			CopyReports(249, 311);
			CopyReports(249, 317);

			//301 307 313 319 как 250
			CopyReports(250, 301);
			CopyReports(250, 307);
			CopyReports(250, 313);
			CopyReports(250, 319);

			//303 309 315 321 как 251
			CopyReports(251, 303);
			CopyReports(251, 309);
			CopyReports(251, 315);
			CopyReports(251, 321);
		}

		[Test(Description = "создает отчеты у родительского отчета 443 по подобию отчетов для родительского отчета 19 с копированием всех свойств, задача пришла от Борисова"),
			Ignore("это не тест, а метод для выполнения действий с отчетами")
		]
		public void CloneReportsToDestinationBy19()
		{
			CopyReports(19, 443);
		}

		[
			Test(Description = "создает отчеты у родительского отчета 459 по подобию отчетов для родительского отчета 393 с копированием всех свойств, задача пришла от Павла"),
			Ignore("это не тест, а метод для выполнения действий с отчетами")
		]
		public void CloneReportsToDestinationBy434()
		{
			CopyReports(393, 459);
		}

		[
			Test(Description = "копирует свойства отчетов у родительского отчета 479, задача пришла от Борисова"),
			Ignore("это не тест, а метод для выполнения действий с отчетами")
		]
		public void CloneReportsForGeneralReport479()
		{
			CopyReportProperties(1595, 1605);
			CopyReportProperties(1595, 1611);
			CopyReportProperties(1595, 1617);
			CopyReportProperties(1595, 1623);
			CopyReportProperties(1595, 1629);

			CopyReportProperties(1601, 1607);
			CopyReportProperties(1601, 1613);
			CopyReportProperties(1601, 1619);
			CopyReportProperties(1601, 1625);
			CopyReportProperties(1601, 1631);

			CopyReportProperties(1603, 1609);
			CopyReportProperties(1603, 1615);
			CopyReportProperties(1603, 1621);
			CopyReportProperties(1603, 1627);
			CopyReportProperties(1603, 1633);
		}

		[Test, Ignore("Для добавления новых параметров")]
		public void AddNewOptions()
		{
			ulong reportTypeCode = 1;

			using (new SessionScope()) {
				var reports = Report.Queryable.Where(r => r.ReportType.Id == reportTypeCode).ToList();

				var prop_types = ReportTypeProperty.Queryable
					.Where(pt => pt.ReportType.Id == reportTypeCode)
					.Where(pt => pt.PropertyName == "ByBaseCosts" ||
						pt.PropertyName == "PriceCodeEqual" ||
						pt.PropertyName == "RegionEqual")
					.ToList();

				foreach (var report in reports) {
					foreach (var prop_type in prop_types) {
						var prop = ReportProperty.Queryable.Where(p => p.Report == report
							&& p.PropertyType.Id == prop_type.Id)
							.ToList();
						if (prop.Count != 0)
							continue;
						var property = new ReportProperty {
							Report = report,
							PropertyType = prop_type,
							Value = prop_type.DefaultValue
						};

						property.Save();
					}
				}
			}
		}
	}
}