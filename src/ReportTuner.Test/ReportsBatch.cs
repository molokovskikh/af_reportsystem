using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Reflection;
using System.Data;


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
				ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
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

			foreach (DataRow drSourceProperty in dtSourceProperties.Rows)
			{
				DataRow[] drDestinationProperties = dtDestinationProperties.Select("PropertyId = " + drSourceProperty["PropertyId"]);
				if (drDestinationProperties.Length == 0)
				{
					//Свойство не существует, поэтому просто вставляем новое
					sbCommand.AppendFormat("insert into reports.report_properties (ReportCode, PropertyId, PropertyValue) values ({0}, {1}, '{2}');\r\n",
						destinationReportId, drSourceProperty["PropertyId"], drSourceProperty["PropertyValue"]);
					if (drSourceProperty["PropertyType"].ToString().Equals("LIST", StringComparison.OrdinalIgnoreCase))
					{
						sbCommand.AppendLine("set @LastReportPropertyId = last_insert_id();");
						foreach (DataRow drSourcePropertiesValue in dtSourcePropertiesValues.Select("ReportPropertyId = " + drSourceProperty["Id"]))
						{
							sbCommand.AppendFormat("insert into reports.report_property_values (ReportPropertyId, Value) values (@LastReportPropertyId, '{0}');\r\n",
								drSourcePropertiesValue["Value"]);
						}
					}
				}
				else
				{
					//Свойство существует, поэтому обновляем запись
					sbCommand.AppendFormat("update reports.report_properties set PropertyValue = '{0}' where Id = {1};\r\n",
						drSourceProperty["PropertyValue"], drDestinationProperties[0]["Id"]);

					if (drSourceProperty["PropertyType"].ToString().Equals("LIST", StringComparison.OrdinalIgnoreCase))
					{
						sbCommand.AppendFormat("delete from reports.report_property_values where ReportPropertyId = {0};\r\n", drDestinationProperties[0]["Id"]);
						foreach (DataRow drSourcePropertiesValue in dtSourcePropertiesValues.Select("ReportPropertyId = " + drSourceProperty["Id"]))
						{
							sbCommand.AppendFormat("insert into reports.report_property_values (ReportPropertyId, Value) values ({0}, '{1}');\r\n",
								drDestinationProperties[0]["Id"], drSourcePropertiesValue["Value"]);
						}
					}
				}
			}

			MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
			connection.Open();
			try
			{
				MySqlTransaction transaction = connection.BeginTransaction();
				MySqlHelper.ExecuteNonQuery(connection, sbCommand.ToString());
				transaction.Commit();
			}
			finally
			{
				connection.Close();
			}
		}

		[Test(Description = "создает отчеты по подобию определенного отчета с копированием всех свойств, меняя название заголовка отчета как '2', '3' и т.д."),
		Ignore("это не тест, а метод для выполнения действий с отчетами")]
		public void CloneReportsFromSourceReport()
		{
			ulong sourceReportId = 585;
			var newReportList = new List<ulong>();

			using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
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

				for (int i = 5; i <= 55; i++)
				{
					command.Parameters["?ReportCaption"].Value = i.ToString();
					newReportList.Add(Convert.ToUInt64(command.ExecuteScalar()));
				}
				conn.Close();
			}

			foreach (var destinationReportId in newReportList)
				CopyReportProperties(sourceReportId, destinationReportId);
		}

		[Test(Description = "создает отчеты по подобию отчета 'Отчет по оптимизации цен' для родительского отчета 238 с копированием всех свойств, меняя название заголовка отчета как краткое имя клиента и выставляя параметр 'Клиент'")
		, Ignore("это не тест, а метод для выполнения действий с отчетами")]		
		public void CloneOptimizationEfficiencyReports()
		{
			ulong sourceGeneralReportId = 238;
			var newReportList = new List<ulong>();

			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				connection.Open();

				var templateReport = MySqlHelper.ExecuteDataRow(
					ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
					@"
select
  reports.ReportCode,
  reports.ReportTypeCode
from
  reports.General_Reports,
  reports.reports,
  reports.ReportTypes
where
    General_Reports.GeneralReportCode = ?GeneralReportCode
and General_Reports.GeneralReportCode = reports.GeneralReportCode
and ReportTypes.ReportTypeCode = reports.ReportTypeCode
and ReportTypes.ReportClassName = ?ReportClassName
limit 1
"
					,
					new MySqlParameter("?GeneralReportCode", sourceGeneralReportId),
					new MySqlParameter("?ReportClassName", "Inforoom.ReportSystem.OptimizationEfficiency"));

				Console.WriteLine("type = {0}", templateReport["ReportTypeCode"]);

				var clientCodes = new int[] { 1349, 360 };

				var insertReportCommand = new MySqlCommand(
					@"
insert into reports.reports 
  (GeneralReportCode, ReportCaption, ReportTypeCode, Enabled)
select 
  reports.GeneralReportCode, cd.ShortName, reports.ReportTypeCode, 1
from 
  reports.reports,
  usersettings.ClientsData cd 
where reports.ReportCode = ?reportCode
and cd.FirmCode = ?ClientCode;
select last_insert_id() as ReportCode;", connection);
				insertReportCommand.Parameters.AddWithValue("?reportCode", templateReport["ReportCode"]);
				insertReportCommand.Parameters.Add("?ClientCode", MySqlDbType.Int32);

				var updateReportCommand = new MySqlCommand(
					@"
insert into report_properties
  (ReportCode, PropertyId, PropertyValue)
select
  ?ReportCode, report_type_properties.Id, ?PropertyValue
from
  reports.report_type_properties
where
    report_type_properties.ReportTypeCode = ?ReportTypeCode
and report_type_properties.PropertyName = ?PropertyName;
"
					, 
					connection);
				updateReportCommand.Parameters.AddWithValue("?PropertyName", "ClientCode");
				updateReportCommand.Parameters.AddWithValue("?ReportTypeCode", templateReport["ReportTypeCode"]);
				updateReportCommand.Parameters.Add("?ReportCode", MySqlDbType.UInt64);
				updateReportCommand.Parameters.Add("?PropertyValue", MySqlDbType.String);

				var dsClients = MySqlHelper.ExecuteDataset(
					connection,
					@"
SELECT firmcode, shortname FROM usersettings.ClientsData C
where (shortname like '%кузнецов%'
or  shortname like '%практика%'
or  shortname like '%мао%'
or  shortname like '%эконом%'
or  shortname like '%рифарм%'
or  shortname like '%ано мсч%'
or  shortname like '%мальцев%'
or  shortname like '%малинка%'
or  shortname like '%челфарм%'
or  shortname like '%русалев%'
or  shortname like '%руско%'
or  shortname like '%онкоцентр%')
and shortname not like '%отчет%'
and regioncode in (64, 16384, 32768, 65536)
and firmstatus = 1
and firmtype = 1
order by billingcode;
"
					);

				foreach (DataRow client in dsClients.Tables[0].Rows)
				{
					insertReportCommand.Parameters["?ClientCode"].Value = client["FirmCode"];
					var newReportCode = Convert.ToUInt64(insertReportCommand.ExecuteScalar());
					newReportList.Add(newReportCode);
					CopyReportProperties(Convert.ToUInt64(templateReport["ReportCode"]), newReportCode);

					updateReportCommand.Parameters["?ReportCode"].Value = newReportCode;
					updateReportCommand.Parameters["?PropertyValue"].Value = client["FirmCode"];
					var updated = updateReportCommand.ExecuteNonQuery();
					if (updated != 1)
						throw new Exception(String.Format("Не обновили свойство для отчета = {0}", newReportCode));
				}

				connection.Close();
			}
		}
	}
}
