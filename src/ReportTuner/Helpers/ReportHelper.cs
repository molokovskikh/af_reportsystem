using System;
using System.Collections.Generic;
using System.Web;
using ReportTuner.Models;
using System.Data;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Text;

namespace ReportTuner.Helpers
{
	public static class ReportHelper
	{
		/// <summary>
		/// Дублирует все значения свойств из одного отчета в другой (клонирование)
		/// </summary>
		/// <param name="sourceReportId">код исходного отчета из таблицы reports.reports</param>
		/// <param name="destinationReportId">код отчета-приемника из таблицы reports.reports</param>
		public static void CopyReportProperties(ulong sourceReportId, ulong destinationReportId)
		{
			Report _sourceReport = Report.Find(sourceReportId);
			Report _destinationReport = Report.Find(destinationReportId);
			if (_sourceReport.ReportType != _destinationReport.ReportType)
				throw new Exception(
					String.Format(
						"Тип клонируемого отчета отличается от конечного отчета. Тип исходного отчета: {0}. Тип отчета-приемника: {1}", 
						_sourceReport.ReportType.ReportTypeName,
						_destinationReport.ReportType.ReportTypeName));

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
	}
}
