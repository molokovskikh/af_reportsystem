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
	}
}
