using System;
using System.Data;
using System.IO;
using ExecuteTemplate;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.Model
{
	public class ReportPropertiesLoader
	{
		private ulong _reportCode;

		public DataSet LoadProperties(MySqlConnection conn, ulong reportCode)
		{
			_reportCode = reportCode;
			var result = MethodTemplate.ExecuteMethod(new ExecuteArgs(), GetReportProperties, null, conn);
			return result;
		}

		private DataSet GetReportProperties(ExecuteArgs e)
		{
			var ds = new DataSet();

			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select
  *
from
  reports.Report_Properties rp,
  reports.report_type_properties rtp
where
    rp.{0} = ?{0}
and rtp.ID = rp.PropertyID", BaseReportColumns.colReportCode);
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?" + BaseReportColumns.colReportCode, _reportCode);
			DataTable res = new DataTable("ReportProperties");
			e.DataAdapter.Fill(res);
			ds.Tables.Add(res);

			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select
  rpv.*
from
  reports.Report_Properties rp,
  reports.report_property_values rpv
where
    rp.{0} = ?{0}
and rpv.ReportPropertyID = rp.ID", BaseReportColumns.colReportCode);
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?" + BaseReportColumns.colReportCode, _reportCode);
			res = new DataTable("ReportPropertyValues");
			e.DataAdapter.Fill(res);
			ds.Tables.Add(res);

			return ds;
		}
	}
}