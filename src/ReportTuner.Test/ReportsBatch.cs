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
		, Ignore("это не тест, а метод для выполнения действий с отчетами")
		]		
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
SELECT firmcode, shortname FROM
  usersettings.ClientsData C
  left join
  (SELECT rp.PropertyValue as ClientCode FROM
  reports.reports r,
  reports.report_properties rp
WHERE r.`GeneralReportCode` = 238
and rp.ReportCode = r.ReportCode
and rp.PropertyId = 144) as existsClientCodes on existsClientCodes.ClientCode = c.FirmCode
where (shortname like '%витамин%'
or  shortname like '%юкон%'
or  shortname like '%лазурит%'
or  shortname like '%бухтиярова%'
or  shortname like '%атромед%'
or  shortname like '%акватик%'
or  shortname like '%аптека 222%'
or  shortname like '%Аптека №1%')
and shortname not like '%отчет%'
and regioncode in (64, 16384, 32768, 65536)
and firmstatus = 1
and firmtype = 1
and existsClientCodes.ClientCode is null
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

		[Test(Description = "создает отчеты по подобию отчета 'Обезличенные предложения для аптеки с привязкой по прайс-листу' для родительского отчета 240 с копированием всех свойств, меняя название заголовка отчета как FirmClientCode и выставляя параметр 'Клиент'")
		, Ignore("это не тест, а метод для выполнения действий с отчетами")
		]
		public void CloneOffersReports()
		{
			ulong sourceGeneralReportId = 240;
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
					new MySqlParameter("?ReportClassName", "Inforoom.ReportSystem.OffersReport"));

				Console.WriteLine("type = {0}", templateReport["ReportTypeCode"]);

				var insertReportCommand = new MySqlCommand(
					@"
insert into reports.reports 
  (GeneralReportCode, ReportCaption, ReportTypeCode, Enabled)
select 
  reports.GeneralReportCode, ?FirmClientCode, reports.ReportTypeCode, 1
from 
  reports.reports 
where reports.ReportCode = ?reportCode;
select last_insert_id() as ReportCode;", connection);
				insertReportCommand.Parameters.AddWithValue("?reportCode", templateReport["ReportCode"]);
				insertReportCommand.Parameters.Add("?FirmClientCode", MySqlDbType.String);

				/*
				insert into report_properties
				  (ReportCode, PropertyId, PropertyValue)
				select
				  ?ReportCode, report_type_properties.Id, ?PropertyValue
				from
				  reports.report_type_properties
				where
					report_type_properties.ReportTypeCode = ?ReportTypeCode
				and report_type_properties.PropertyName = ?PropertyName;
				 */
				var updateReportCommand = new MySqlCommand(
					@"
update
  reports.report_properties,
  reports.report_type_properties
set
  report_properties.PropertyValue = ?PropertyValue
where
    report_type_properties.ReportTypeCode = ?ReportTypeCode
and report_type_properties.PropertyName = ?PropertyName
and report_properties.PropertyId = report_type_properties.Id
and report_properties.ReportCode = ?ReportCode;
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
SELECT * FROM Intersection I, clientsdata c
where pricecode in (4651,187)
and firmclientcode in
(10093556,10093615,10093543,10093602,10093477,10093511,10125459,10093495,10093498,10109682,10128998,10102917,10108755,10093492,10093546,10102908,10149742,10093548,10093601,10093530,10093411,10102768,10146782,
10093603,10093613,10093521,10093486,10125140,10093542,10093418,10138840,10150545,10127219,10093537,10124576,10108753,10102914,10093556)
and clientcode = firmcode
and shortname like '%отчетС%'
group by firmclientcode
"
					);

				foreach (DataRow client in dsClients.Tables[0].Rows)
				{
					insertReportCommand.Parameters["?FirmClientCode"].Value = client["FirmClientCode"];
					var newReportCode = Convert.ToUInt64(insertReportCommand.ExecuteScalar());
					newReportList.Add(newReportCode);
					CopyReportProperties(Convert.ToUInt64(templateReport["ReportCode"]), newReportCode);

					updateReportCommand.Parameters["?ReportCode"].Value = newReportCode;
					updateReportCommand.Parameters["?PropertyValue"].Value = client["ClientCode"];
					var updated = updateReportCommand.ExecuteNonQuery();
					if (updated != 1)
						throw new Exception(String.Format("Не обновили свойство для отчета = {0}", newReportCode));
				}

				connection.Close();
			}
		}

		//Копирует все отчеты из родительского отчета sourceGeneralReportId в родительский отчет destinationGeneralReportId,
		//если в родительском отчете destinationGeneralReportId есть отчеты, то перед копирование происходит их удаление
		private void CopyReports(ulong sourceGeneralReportId, ulong destinationGeneralReportId)
		{
			var newReportList = new List<ulong>();

			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				connection.Open();

				var templateReportDS = MySqlHelper.ExecuteDataset(
					ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
					@"
select
  reports.ReportCode
from
  reports.General_Reports,
  reports.reports
where
    General_Reports.GeneralReportCode = ?GeneralReportCode
and General_Reports.GeneralReportCode = reports.GeneralReportCode
"
					,
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

				foreach (DataRow templateReport in templateReports.Rows)
				{
					var templateReportId = Convert.ToUInt64(templateReport["ReportCode"]);
					insertReportCommand.Parameters["?reportCode"].Value = templateReportId;
					var newReportCode = Convert.ToUInt64(insertReportCommand.ExecuteScalar());
					newReportList.Add(newReportCode);
					CopyReportProperties(templateReportId, newReportCode);
				}
			}

		}

		[Test(Description = "создает отчеты у родительского отчета 213 по подобию отчетов для родительского отчета 210 с копированием всех свойств, задача пришла от Павла")
		, Ignore("это не тест, а метод для выполнения действий с отчетами")
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

		[Test(Description = "создает отчеты у родительского отчета 443 по подобию отчетов для родительского отчета 19 с копированием всех свойств, задача пришла от Борисова")
		, Ignore("это не тест, а метод для выполнения действий с отчетами")
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

    }
}
