﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using ExecuteTemplate;
using MySql.Data.MySqlClient;
using System.IO;

namespace Inforoom.ReportSystem
{
	public class ReportPropertiesLoader : IReportPropertiesLoader
	{
		private ulong _reportCode;


		private void SaveSettingsToFileAndThrowException(DataSet result)
		{
			int i = 1;
			while (File.Exists(_reportCode.ToString() + "(" + i.ToString() + ").xml"))
				i++;
			result.WriteXml(_reportCode.ToString() + "(" + i.ToString() + ").xml");
			throw new Exception("Хватит!!!"); 
		}

		public DataSet LoadProperties(MySqlConnection conn, ulong ReportCode)
		{
			_reportCode = ReportCode;
			var result = MethodTemplate.ExecuteMethod<ExecuteArgs, DataSet>(new ExecuteArgs(), GetReportProperties, null, conn);
#if DEBUG
			// Раскомитить если нужно получить xml файлы для тестов
			//SaveSettingsToFileAndThrowException(result); 
#endif 
			return result;
		}

		private DataSet GetReportProperties(ExecuteArgs e)
		{
			DataSet ds = new DataSet();

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