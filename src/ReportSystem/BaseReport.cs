using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data.OleDb;
using System.IO;
using Inforoom.ReportSystem.Writers;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem
{	//Костыль т.к. не используем ActiveRecord модели, то пришлось копировать enum
	public enum ReportFormats 
	{
		Excel,
		DBF
	}

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
		//родительский отчет является разовым?
		protected bool _parentIsTemporary;

		//Таблица с загруженными свойствами отчета
		protected DataTable dtReportProperties;
		//Таблица с загруженными значениями списков-свойств
		protected DataTable dtReportPropertyValues;
		//Формат файла отчета
		protected ReportFormats Format;

		protected MySqlConnection _conn;

		protected Dictionary<string, object> _reportParams;

		public BaseReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, 
			ReportFormats format, DataSet dsProperties)
		{
			_reportParams = new Dictionary<string, object>();
			_reportCode = ReportCode;
			_reportCaption = ReportCaption;
			Format = format;
			_dsReport = new DataSet();
			_conn = Conn;

			_parentIsTemporary = Temporary;

			//DataSet dsTab = 
			dtReportProperties = dsProperties.Tables["ReportProperties"];
			dtReportPropertyValues = dsProperties.Tables["ReportPropertyValues"];

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
								throw new ReportException(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'. Ошибка : {2}", 
									drProperty[BaseReportColumns.colPropertyType].ToString(),
									drProperty[BaseReportColumns.colPropertyValue].ToString(),
									ex.Message), ex);
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
									throw new ReportException(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'. Ошибка : {2}",
										drProperty[BaseReportColumns.colPropertyType].ToString(),
										drValue[BaseReportColumns.colReportPropertyValue].ToString(),
										ex.Message), ex);
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
								if (drProperty[BaseReportColumns.colPropertyValue].ToString().Equals("NOW", StringComparison.OrdinalIgnoreCase))
									_reportParams.Add(currentPropertyName, DateTime.Now);
								else
									_reportParams.Add(currentPropertyName, DateTime.ParseExact(drProperty[BaseReportColumns.colPropertyValue].ToString(), MySQLDateFormat, null));
							}
							catch (Exception ex)
							{
								throw new ReportException(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'. Ошибка : {2}",
									drProperty[BaseReportColumns.colPropertyType].ToString(),
									drProperty[BaseReportColumns.colPropertyValue].ToString(),
									ex.Message), ex);
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
								throw new ReportException(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'. Ошибка : {2}",
									drProperty[BaseReportColumns.colPropertyType].ToString(),
									drProperty[BaseReportColumns.colPropertyValue].ToString(),
									ex.Message), ex);
							}
							break;

						default:
							throw new ReportException(String.Format("Неизвестный тип параметра : '{0}'.", drProperty[BaseReportColumns.colPropertyType].ToString()));
					}
				}
				else
				{
					throw new ReportException(String.Format("Параметр '{0}' задан дважды.", currentPropertyName));
				}
			}

			ReadReportParams();
		}

		public abstract void GenerateReport(ExecuteArgs e);

		public abstract void ReadReportParams();

		public void ProcessReport()
		{
			MethodTemplate.ExecuteMethod(new ExecuteArgs(), ProcessReportExec, false, _conn);
		}

		protected bool ProcessReportExec(ExecuteArgs e)
		{
			_dsReport.Clear();
			GenerateReport(e);
			return true;
		}

		public virtual void ReportToFile(string fileName)
		{
			IWriter writer = GetWriter(Format);
			if(writer != null)
			{  // Новый механизм, выносим часть для выгрузки в файл в отдельный класс
				var settings = GetSettings();
				writer.WriteReportToFile(_dsReport, fileName, settings);
				return;
			}

			if(Format == ReportFormats.DBF && DbfSupported)
			{// Формируем DBF
				fileName =  Path.Combine(Path.GetDirectoryName(fileName), _reportCaption + ".dbf");
				DataTableToDbf(GetReportTable(), fileName);
			}
			else
			{// Формируем Excel
				DataTableToExcel(GetReportTable(), fileName);
				FormatExcel(fileName);
			}
		}

		protected virtual void DataTableToExcel(DataTable dtExport, string exlFileName)
		{
			ProfileHelper.Next("DataTableToExcel");
			//Имя листа генерируем сами, а потом переименовываем, т.к. русские названия листов потом невозможно найти
			var generatedListName = "rep" + _reportCode;
			var excellCon = new OleDbConnection();
			try
			{
				excellCon.ConnectionString = @"
Provider=Microsoft.Jet.OLEDB.4.0;Password="""";User ID=Admin;Data Source=" + exlFileName + 
@";Mode=Share Deny None;Extended Properties=""Excel 8.0;HDR=no"";";
				string createSql = "create table [" + generatedListName + "] (";
				for (int i = 0; i < dtExport.Columns.Count; i++)
				{ 
					createSql += "[F" + (i+1) + "] ";
					dtExport.Columns[i].ColumnName = "F" + (i + 1);

					if (dtExport.Columns[i].DataType == typeof(int))
						createSql += " int";
					else if (dtExport.Columns[i].DataType == typeof(decimal))
						createSql += " currency";
					else if (dtExport.Columns[i].DataType == typeof(double))
						createSql += " real";
					else if ((dtExport.Columns[i].DataType == typeof(string)) && (dtExport.Columns[i].MaxLength > -1) && (dtExport.Columns[i].MaxLength <= MaxStringSize))
						createSql += String.Format(" char({0})", MaxStringSize);
					else
						createSql += " memo";

					if (i == dtExport.Columns.Count - 1)
						createSql += ");";
					else
						createSql += ",";
				}
				var cmd = new OleDbCommand(createSql, excellCon);
				excellCon.Open();
				cmd.ExecuteNonQuery();
				var daExcel = new OleDbDataAdapter("select * from [" + generatedListName + "]", excellCon);
				var cdExcel = new OleDbCommandBuilder(daExcel) {QuotePrefix = "[", QuoteSuffix = "]"};
				daExcel.Update(dtExport);
			}
			finally
			{
				excellCon.Close();
			}
		}

		protected virtual void FormatExcel(string fileName)
		{}

		public virtual bool DbfSupported { get; private set; }

		protected virtual void DataTableToDbf(DataTable dtExport, string fileName)
		{
			using(var writer = new StreamWriter(fileName, false, Encoding.GetEncoding(866)))
				Dbf.Save(dtExport, writer);
		}

		protected virtual DataTable GetReportTable()
		{
			return _dsReport.Tables["Results"];
		}

		internal object getReportParam(string ParamName)
		{
			if (_reportParams.ContainsKey(ParamName))
				return _reportParams[ParamName];
			else
				throw new ReportException(String.Format("Параметр '{0}' не найден.", ParamName));
		}

		internal bool reportParamExists(string ParamName)
		{
			return _reportParams.ContainsKey(ParamName);
		}

		protected virtual IWriter GetWriter(ReportFormats format)
		{
			return null;
		}

		protected virtual BaseReportSettings GetSettings()
		{
			return null;
		}

		protected string GetClientsNamesFromSQL(ExecuteArgs e, List<ulong> equalValues)
		{
			var filterStr = new StringBuilder("(");
			equalValues.ForEach(val => filterStr.Append(val).Append(','));
			filterStr[filterStr.Length - 1] = ')';

			var valuesList = new List<string>();
			e.DataAdapter.SelectCommand.CommandText = String.Format(
@"select ShortName
    from ClientsData
  where FirmCode in {0}
union
select Name
  from future.Clients
 where Id in {0}", filterStr);
			e.DataAdapter.SelectCommand.Parameters.Clear();
			DataTable dtValues = new DataTable();
			e.DataAdapter.Fill(dtValues);
			foreach (DataRow dr in dtValues.Rows)
				valuesList.Add(dr[0].ToString());

			return String.Join(", ", valuesList.ToArray());
		}
	}
}
