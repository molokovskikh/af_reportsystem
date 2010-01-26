using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data.OleDb;
using ReportSystem.Profiling;
using System.IO;

namespace Inforoom.ReportSystem
{	//������� �.�. �� ���������� ActiveRecord ������, �� �������� ���������� enum
	public enum ReportFormats 
	{
		Excel,
		DBF
	}

	//�������� �������� �����, ������������ ��� �������� ������ �����
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
	
	//����� ����� ��� ������ � �������
	public abstract class BaseReport
	{
		//������������ �������� ������ � ��������, ���������� ��� ������ � Excel, ���, ��� ����� ������ ����� ���������� ��� mem�
		public const int MaxStringSize = 250;
		
		public const int MaxListName = 26;

		public const string MySQLDateFormat = "yyyy-MM-dd";

		protected DataSet _dsReport;

		protected ulong _reportCode;
		protected string _reportCaption;
		//������������ ����� �������� �������?
		protected bool _parentIsTemporary;

		//������� � ������������ ���������� ������
		protected DataTable dtReportProperties;
		//������� � ������������ ���������� �������-�������
		protected DataTable dtReportPropertyValues;
		//������ ����� ������
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
								throw new ReportException(String.Format("������ ��� ����������� ��������� '{0}' �� ������ '{1}'. ������ : {2}", 
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
									throw new ReportException(String.Format("������ ��� ����������� ��������� '{0}' �� ������ '{1}'. ������ : {2}",
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
								throw new ReportException(String.Format("������ ��� ����������� ��������� '{0}' �� ������ '{1}'. ������ : {2}",
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
								throw new ReportException(String.Format("������ ��� ����������� ��������� '{0}' �� ������ '{1}'. ������ : {2}",
									drProperty[BaseReportColumns.colPropertyType].ToString(),
									drProperty[BaseReportColumns.colPropertyValue].ToString(),
									ex.Message), ex);
							}
							break;

						default:
							throw new ReportException(String.Format("����������� ��� ��������� : '{0}'.", drProperty[BaseReportColumns.colPropertyType].ToString()));
					}
				}
				else
				{
					throw new ReportException(String.Format("�������� '{0}' ����� ������.", currentPropertyName));
				}
			}

			ReadReportParams();
		}

		public abstract void GenerateReport(ExecuteArgs e);

		public abstract void ReadReportParams();

		public void ProcessReport()
		{
			bool res = MethodTemplate.ExecuteMethod<ExecuteArgs, bool>(new ExecuteArgs(), ProcessReportExec, false, _conn);
		}

		protected bool ProcessReportExec(ExecuteArgs e)
		{
			_dsReport.Clear();
			GenerateReport(e);
			return true;
		}

		public virtual void ReportToFile(string fileName)
		{
			if(Format == ReportFormats.DBF && DbfSupported)
			{// ��������� DBF
				string oldFileName = Path.GetFileName(fileName);
				fileName =  Path.Combine(Path.GetDirectoryName(fileName), _reportCaption + ".dbf");
				DataTableToDbf(GetReportTable(), fileName);
			}
			else
			{// ��������� Excel
				DataTableToExcel(GetReportTable(), fileName);
				FormatExcel(fileName);
			}
		}

		protected virtual void DataTableToExcel(DataTable dtExport, string ExlFileName)
		{
			ProfileHelper.Next("DataTableToExcel");
			//��� ����� ���������� ����, � ����� ���������������, �.�. ������� �������� ������ ����� ���������� �����
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

		protected virtual void FormatExcel(string fileName)
		{}

		public virtual bool DbfSupported
		{
			get
			{
				return false;
			}
		}

		protected virtual void DataTableToDbf(DataTable dtExport, string fileName)
		{
			using(var writer = new StreamWriter(fileName, false, Encoding.GetEncoding(866)))
				Inforoom.Data.DBF.Save(dtExport, writer);
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
				throw new ReportException(String.Format("�������� '{0}' �� ������.", ParamName));
		}

		internal bool reportParamExists(string ParamName)
		{
			return _reportParams.ContainsKey(ParamName);
		}
	}
}
