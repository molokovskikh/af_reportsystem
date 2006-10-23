using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data.OleDb;

namespace Inforoom.ReportSystem
{
	//Содержит названия полей, используемых при создании общего очета
	public sealed class BaseReportColumns
	{
		public const string colReportCode = "ReportCode";
		public const string colGeneralReportCode = "GeneralReportCode";
		public const string colReportCaption = "ReportCaption";
		public const string colReportTypeCode = "ReportTypeCode";

		public const string colPropertyName = "PropertyName";
		public const string colPropertyValue = "PropertyValue";
	}
	
	//Общий класс для работы с отчетам
	public abstract class BaseReport
	{
		protected DataSet _dsReport;

		protected ulong _reportCode;
		protected string _reportCaption;

		protected MySqlConnection _conn;

		protected Dictionary<string, object> _reportParams;


		public BaseReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
		{
			_reportParams = new Dictionary<string, object>();
			_reportCode = ReportCode;
			_reportCaption = ReportCaption;
			_dsReport = new DataSet();
			_conn = Conn;

			DataTable dtReportProperties = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(new ExecuteArgs(), GetReportProperties, null, _conn, true, false);

			foreach (DataRow drProperty in dtReportProperties.Rows)
			{
				if (_reportParams.ContainsKey(drProperty[BaseReportColumns.colPropertyName].ToString()))
				{
					if (_reportParams[drProperty[BaseReportColumns.colPropertyName].ToString()] is int)
					{
						int v = (int)_reportParams[drProperty[BaseReportColumns.colPropertyName].ToString()];
						List<int> l = new List<int>();
						l.Add(v);
						_reportParams[drProperty[BaseReportColumns.colPropertyName].ToString()] = l;
					}
					((List<int>)_reportParams[drProperty[BaseReportColumns.colPropertyName].ToString()]).
						Add(int.Parse(drProperty[BaseReportColumns.colPropertyValue].ToString()));
				}
				else
					_reportParams.Add(drProperty[BaseReportColumns.colPropertyName].ToString(), int.Parse(drProperty[BaseReportColumns.colPropertyValue].ToString()));
			}

		}

		//Выбираем отчеты из базы
		private DataTable GetReportProperties(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = String.Format("select * from reports.ReportProperties where {0} = ?{0}", BaseReportColumns.colReportCode);
			e.DataAdapter.SelectCommand.Parameters.Add(BaseReportColumns.colReportCode, _reportCode);
			DataTable res = new DataTable();
			e.DataAdapter.Fill(res);
			return res;
		}

		public abstract void GenerateReport(ExecuteArgs e);

		public void ProcessReport()
		{
			bool res = MethodTemplate.ExecuteMethod<ExecuteArgs, bool>(new ExecuteArgs(), ProcessReportExec, false, _conn, true, false);
		}

		protected bool ProcessReportExec(ExecuteArgs e)
		{
			_dsReport.Clear();
			GenerateReport(e);
			return true;
		}

		public abstract void ReportToFile(string FileName);

		protected void DataTableToExcel(DataTable dtExport, string ExlFileName)
		{
			string tmp = "testRep";
			tmp = _reportCaption;
			tmp = "rep" + _reportCode.ToString();
			OleDbConnection ExcellCon = new OleDbConnection();
			try
			{
				ExcellCon.ConnectionString = @"
Provider=Microsoft.Jet.OLEDB.4.0;Password="""";User ID=Admin;Data Source=" + ExlFileName + 
@";Mode=Share Deny None;Extended Properties=""Excel 8.0;HDR=no"";";
				string CreateSQL = "create table [" + tmp + "] (";
				for (int i = 0; i < dtExport.Columns.Count; i++)
				{ 
					CreateSQL += "[F" + (i+1).ToString() + "] ";
					//CreateSQL += "[" + dtExport.Columns[i].ColumnName + "] ";
					dtExport.Columns[i].ColumnName = "F" + (i + 1).ToString();
					if (dtExport.Columns[i].DataType == typeof(int))
						CreateSQL += " int";
					else
						if (dtExport.Columns[i].DataType == typeof(decimal))
							CreateSQL += " currency";
						else
							CreateSQL += " char(250)";
					if (i == dtExport.Columns.Count - 1)
						CreateSQL += ");";
					else
						CreateSQL += ",";
				}
				OleDbCommand cmd = new OleDbCommand(CreateSQL, ExcellCon);
				ExcellCon.Open();
				cmd.ExecuteNonQuery();
				OleDbDataAdapter daExcel = new OleDbDataAdapter("select * from [" + tmp + "]", ExcellCon);
				OleDbCommandBuilder cdExcel = new OleDbCommandBuilder(daExcel);
				cdExcel.QuotePrefix = "[";
				cdExcel.QuoteSuffix = "]";
				daExcel.Update(dtExport);
			}
			finally
			{
				ExcellCon.Close();
			}
		}
	}
}
