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
		public const string colEnabled = "Enabled";
		public const string colAlternateSubject = "AlternateSubject";
		public const string colReportClassName = "ReportClassName";

		public const string colPropertyName = "PropertyName";
		public const string colPropertyValue = "PropertyValue";
		public const string colPropertyType = "PropertyType";
		public const string colPropertyID = "ID";

		public const string colReportPropertyID = "ReportPropertyID";
		public const string colReportPropertyValue = "Value";

	}
	
	//Общий класс для работы с отчетам
	public abstract class BaseReport
	{
		//Максимальное значение строки в колонках, необходимо для вывода в Excel, все, что будет больше будет помечаться как memо
		public const int MaxStringSize = 250;
		
		public const int MaxListName = 26;

		public const string MySQLDateFormat = "yyyy-MM-dd";

		protected DataSet _dsReport;

		protected ulong _reportCode;
		protected string _reportCaption;

		//Таблица с загруженными свойствами отчета
		protected DataTable dtReportProperties;
		//Таблица с загруженными значениями списков-свойств
		protected DataTable dtReportPropertyValues;

		protected MySqlConnection _conn;

		protected Dictionary<string, object> _reportParams;


		public BaseReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
		{
			_reportParams = new Dictionary<string, object>();
			_reportCode = ReportCode;
			_reportCaption = ReportCaption;
			_dsReport = new DataSet();
			_conn = Conn;

			DataSet dsTab = MethodTemplate.ExecuteMethod<ExecuteArgs, DataSet>(new ExecuteArgs(), GetReportProperties, null, _conn, true, false);
			dtReportProperties = dsTab.Tables["ReportProperties"];
			dtReportPropertyValues = dsTab.Tables["ReportPropertyValues"];

			foreach (DataRow drProperty in dtReportProperties.Rows)
			{
				string currentPropertyName = drProperty[BaseReportColumns.colPropertyName].ToString();

				if (!_reportParams.ContainsKey(currentPropertyName))
				{
					switch (drProperty[BaseReportColumns.colPropertyType].ToString())					
					{ 
						case "BOOL":
							try
							{
								_reportParams.Add(currentPropertyName, Convert.ToBoolean(Convert.ToByte(drProperty[BaseReportColumns.colPropertyValue])));
							}
							catch (Exception ex)
							{
								throw new Exception(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'. Ошибка : {2}", 
									drProperty[BaseReportColumns.colPropertyType].ToString(),
									drProperty[BaseReportColumns.colPropertyValue].ToString(),
									ex.Message));
							}
							break;

						case "LIST":
							List<ulong> listValues = new List<ulong>();
							DataRow[] drValues = dtReportPropertyValues.Select(BaseReportColumns.colReportPropertyID + "=" + drProperty[BaseReportColumns.colPropertyID].ToString());
							foreach (DataRow drValue in drValues)
							{
								try
								{
									listValues.Add(Convert.ToUInt64(drValue[BaseReportColumns.colReportPropertyValue]));
								}
								catch (Exception ex)
								{
									throw new Exception(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'. Ошибка : {2}",
										drProperty[BaseReportColumns.colPropertyType].ToString(),
										drValue[BaseReportColumns.colReportPropertyValue].ToString(),
										ex.Message));
								}
							}
							_reportParams.Add(currentPropertyName, listValues);
							break;

						case "STRING":
							_reportParams.Add(currentPropertyName, Convert.ToBoolean(drProperty[BaseReportColumns.colPropertyValue].ToString()));
							break;

						case "DATETIME":
							try
							{
								_reportParams.Add(currentPropertyName, DateTime.ParseExact(drProperty[BaseReportColumns.colPropertyValue].ToString(), MySQLDateFormat, null));
							}
							catch (Exception ex)
							{
								throw new Exception(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'. Ошибка : {2}",
									drProperty[BaseReportColumns.colPropertyType].ToString(),
									drProperty[BaseReportColumns.colPropertyValue].ToString(),
									ex.Message));
							}
							break;

						case "INT":
						case "ENUM":
							try
							{
								_reportParams.Add(currentPropertyName, Convert.ToInt32(drProperty[BaseReportColumns.colPropertyValue].ToString()));
							}
							catch (Exception ex)
							{
								throw new Exception(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'. Ошибка : {2}",
									drProperty[BaseReportColumns.colPropertyType].ToString(),
									drProperty[BaseReportColumns.colPropertyValue].ToString(),
									ex.Message));
							}
							break;

						default:
							throw new Exception(String.Format("Неизвестный тип параметра : '{0}'.", drProperty[BaseReportColumns.colPropertyType].ToString()));
					}
				}
				else
				{
					throw new Exception(String.Format("Параметр '{0}' задан дважды.", currentPropertyName));
				}
			}

			ReadReportParams();

			//foreach (DataRow drProperty in dtReportProperties.Rows)
			//{
			//    string currentPropertyName = drProperty[BaseReportColumns.colPropertyName].ToString();

			//    if (_reportParams.ContainsKey(currentPropertyName))
			//    {
			//        //Если объект уже существует и он int или List<int>
			//        if ((_reportParams[currentPropertyName] is int) || (_reportParams[currentPropertyName] is List<int>))
			//        {
			//            if (_reportParams[currentPropertyName] is int)
			//            {
			//                List<int> l = new List<int>();
			//                l.Add((int)_reportParams[currentPropertyName]);
			//                _reportParams[currentPropertyName] = l;
			//            }
			//            ((List<int>)_reportParams[currentPropertyName]).
			//                Add(int.Parse(drProperty[BaseReportColumns.colPropertyValue].ToString()));
			//        }
			//        else
			//        {
			//            if (_reportParams[currentPropertyName] is string)
			//            {
			//                List<string> l = new List<string>();
			//                l.Add((string)_reportParams[currentPropertyName]);
			//                _reportParams[currentPropertyName] = l;
			//            }
			//            ((List<string>)_reportParams[currentPropertyName]).
			//                Add(drProperty[BaseReportColumns.colPropertyValue].ToString());
			//        }
			//    }
			//    else
			//    {
			//        int tempVal;
			//        if (int.TryParse(drProperty[BaseReportColumns.colPropertyValue].ToString(), out tempVal))
			//            _reportParams.Add(currentPropertyName, tempVal);
			//        else
			//            _reportParams.Add(currentPropertyName, drProperty[BaseReportColumns.colPropertyValue].ToString());
			//    }
			//}

		}

		//Выбираем отчеты из базы
		private DataSet GetReportProperties(ExecuteArgs e)
		{
			DataSet ds = new DataSet();

			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select
  * 
from 
  testreports.Report_Properties rp,
  testreports.report_type_properties rtp
where
    rp.{0} = ?{0}
and rtp.ID = rp.PropertyID", BaseReportColumns.colReportCode);
			e.DataAdapter.SelectCommand.Parameters.Add(BaseReportColumns.colReportCode, _reportCode);
			DataTable res = new DataTable("ReportProperties");
			e.DataAdapter.Fill(res);
			ds.Tables.Add(res);

			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select
  rpv.*
from
  testreports.Report_Properties rp,
  testreports.report_property_values rpv
where
    rp.{0} = ?{0}
and rpv.ReportPropertyID = rp.ID", BaseReportColumns.colReportCode);
			e.DataAdapter.SelectCommand.Parameters.Add(BaseReportColumns.colReportCode, _reportCode);
			res = new DataTable("ReportPropertyValues");
			e.DataAdapter.Fill(res);
			ds.Tables.Add(res);

			return ds;
		}

		public abstract void GenerateReport(ExecuteArgs e);

		public abstract void ReadReportParams();

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
			//Имя листа генерируем сами, а потом переименовываем, т.к. русские названия листов потом невозможно найти
			string generatedListName = "testRep";
			generatedListName = _reportCaption;
			generatedListName = "rep" + _reportCode.ToString();
			OleDbConnection ExcellCon = new OleDbConnection();
			try
			{
				ExcellCon.ConnectionString = @"
Provider=Microsoft.Jet.OLEDB.4.0;Password="""";User ID=Admin;Data Source=" + ExlFileName + 
@";Mode=Share Deny None;Extended Properties=""Excel 8.0;HDR=no"";";
				string CreateSQL = "create table [" + generatedListName + "] (";
				for (int i = 0; i < dtExport.Columns.Count; i++)
				{ 
					CreateSQL += "[F" + (i+1).ToString() + "] ";
					dtExport.Columns[i].ColumnName = "F" + (i + 1).ToString();
					if (dtExport.Columns[i].DataType == typeof(int))
						CreateSQL += " int";
					else
						if (dtExport.Columns[i].DataType == typeof(decimal))
							CreateSQL += " currency";
						else
							if (dtExport.Columns[i].DataType == typeof(double))
								CreateSQL += " real";
							else
								if ((dtExport.Columns[i].DataType == typeof(string)) && (dtExport.Columns[i].MaxLength > -1) && (dtExport.Columns[i].MaxLength <= MaxStringSize))
									CreateSQL += String.Format(" char({0})", MaxStringSize);
								else
									CreateSQL += " memo";
					if (i == dtExport.Columns.Count - 1)
						CreateSQL += ");";
					else
						CreateSQL += ",";
				}
				OleDbCommand cmd = new OleDbCommand(CreateSQL, ExcellCon);
				ExcellCon.Open();
				cmd.ExecuteNonQuery();
				OleDbDataAdapter daExcel = new OleDbDataAdapter("select * from [" + generatedListName + "]", ExcellCon);
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

		internal object getReportParam(string ParamName)
		{
			if (_reportParams.ContainsKey(ParamName))
				return _reportParams[ParamName];
			else
				throw new Exception(String.Format("Параметр '{0}' не найден.", ParamName));
		}

		internal bool reportParamExists(string ParamName)
		{
			return _reportParams.ContainsKey(ParamName);
		}
	}
}
