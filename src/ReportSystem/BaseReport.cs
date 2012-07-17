using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Common.MySql;
using Common.Tools;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using Inforoom.ReportSystem.Properties;
using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using log4net;
using MySql.Data.MySqlClient;

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
		private Dictionary<string, uint> _reportParamsIds = new Dictionary<string, uint>();

		public const int MaxStringSize = 250;
		
		public const int MaxListName = 26;

		protected DataSet _dsReport;

		//������� � ������������ ���������� ������
		protected DataTable dtReportProperties;
		//������� � ������������ ���������� �������-�������
		protected DataTable dtReportPropertyValues;
		//������ ����� ������
		protected ReportFormats Format;

		protected MySqlConnection _conn;

		protected Dictionary<string, object> _reportParams;

		protected ExecuteArgs args;

		protected ILog Logger;
		protected bool _isRetail;

		protected DateTime _dtStart; // ����� ������� ������
		protected DateTime _dtStop; // ����� ���������� ������ ������

		public bool Interval;
		public DateTime From;
		public DateTime To;

		public ulong ReportCode { get; private set; }
		public string ReportCaption { get; private set; }
		
		public virtual bool DbfSupported { get; set; }

		public Dictionary<string, string> AdditionalFiles { get; private set; }

		protected BaseReport() // ����������� ��� ����������� ������������
		{
			AdditionalFiles = new Dictionary<string, string>();
		}

		public BaseReport(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties)
			: this()
		{
			Logger = LogManager.GetLogger(GetType());
			_reportParams = new Dictionary<string, object>();
			ReportCode = reportCode;
			ReportCaption = reportCaption;
			Format = format;
			_dsReport = new DataSet();
			_conn = connection;

			dtReportProperties = dsProperties.Tables["ReportProperties"];
			dtReportPropertyValues = dsProperties.Tables["ReportPropertyValues"];

			foreach (DataRow drProperty in dtReportProperties.Rows)
			{
				var currentPropertyName = drProperty[BaseReportColumns.colPropertyName].ToString();

				if (!_reportParams.ContainsKey(currentPropertyName))
				{
					_reportParamsIds.Add(currentPropertyName, Convert.ToUInt32(drProperty[BaseReportColumns.colPropertyID]));
					switch (drProperty[BaseReportColumns.colPropertyType].ToString())
					{ 
						case "BOOL":
							try
							{
								_reportParams.Add(currentPropertyName, Convert.ToBoolean(Convert.ToByte(drProperty[BaseReportColumns.colPropertyValue])));
							}
							catch (Exception ex)
							{
								throw new ReportException(String.Format("������ ��� ����������� ��������� '{0}' �� ������ '{1}'.", 
									drProperty[BaseReportColumns.colPropertyType],
									drProperty[BaseReportColumns.colPropertyValue]), ex);
							}
							break;

						case "LIST":
							var listValues = new List<ulong>();
							var drValues = dtReportPropertyValues.Select(BaseReportColumns.colReportPropertyID + "=" + drProperty[BaseReportColumns.colPropertyID].ToString());
							foreach (DataRow drValue in drValues)
							{
								try
								{
									listValues.Add(Convert.ToUInt64(drValue[BaseReportColumns.colReportPropertyValue]));
								}
								catch (Exception ex)
								{
									throw new ReportException(String.Format("������ ��� ����������� ��������� '{0}' �� ������ '{1}'.",
										drProperty[BaseReportColumns.colPropertyType],
										drValue[BaseReportColumns.colReportPropertyValue]), ex);
								}
							}
							_reportParams.Add(currentPropertyName, listValues);
							break;
						case "FILE":
						case "STRING":
							_reportParams.Add(currentPropertyName, drProperty[BaseReportColumns.colPropertyValue].ToString());
							break;
						case "DATETIME":
							try
							{
								if (drProperty[BaseReportColumns.colPropertyValue].ToString().Equals("NOW", StringComparison.OrdinalIgnoreCase))
									_reportParams.Add(currentPropertyName, DateTime.Now);
								else
									_reportParams.Add(currentPropertyName, DateTime.ParseExact(drProperty[BaseReportColumns.colPropertyValue].ToString(), MySqlConsts.MySQLDateFormat, null));
							}
							catch (Exception ex)
							{
								throw new ReportException(String.Format("������ ��� ����������� ��������� '{0}' �� ������ '{1}'.",
									drProperty[BaseReportColumns.colPropertyType],
									drProperty[BaseReportColumns.colPropertyValue]), ex);
							}
							break;

						case "INT":
						case "ENUM":
							try
							{
								string val = drProperty[BaseReportColumns.colPropertyValue].ToString();
								if(!String.IsNullOrEmpty(val))
									_reportParams.Add(currentPropertyName, Convert.ToInt32(drProperty[BaseReportColumns.colPropertyValue].ToString()));
							}
							catch (Exception ex)
							{
								throw new ReportException(String.Format("������ ��� ����������� ��������� '{0}' �� ������ '{1}'.",
									drProperty[BaseReportColumns.colPropertyType],
									drProperty[BaseReportColumns.colPropertyValue]), ex);
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
		}

		public abstract void GenerateReport(ExecuteArgs e);

		public abstract void ReadReportParams();

		public void ProcessReport()
		{
			_dtStart = DateTime.Now;
			MethodTemplate.ExecuteMethod(new ExecuteArgs(), ProcessReportExec, false, _conn);
		}

		protected bool ProcessReportExec(ExecuteArgs e)
		{
			args = e;
			_dsReport.Clear();
			GenerateReport(e);
			return true;
		}

		public virtual void ReportToFile(string fileName)
		{
			var writer = GetWriter(Format);
			if(writer != null)
			{  // ����� ��������, ������� ����� ��� �������� � ���� � ��������� �����
				var settings = GetSettings();
				writer.WriteReportToFile(_dsReport, fileName, settings);
				return;
			}

			if(Format == ReportFormats.DBF && DbfSupported)
			{// ��������� DBF
				fileName =  Path.Combine(Path.GetDirectoryName(fileName), ReportCaption + ".dbf");
				DataTableToDbf(GetReportTable(), fileName);
			}
			else
			{// ��������� Excel
				DataTableToExcel(GetReportTable(), fileName);
				FormatExcel(fileName);
			}
		}

		protected virtual void DataTableToExcel(DataTable dtExport, string exlFileName)
		{
			ProfileHelper.Next("DataTableToExcel");
			new BaseExcelWriter().DataTableToExcel(dtExport, exlFileName, ReportCode);
		}

		protected virtual void FormatExcel(string fileName)
		{}

		protected virtual void DataTableToDbf(DataTable dtExport, string fileName)
		{
			using(var writer = new StreamWriter(fileName, false, Encoding.GetEncoding(866)))
				Dbf.Save(dtExport, writer);
		}

		protected virtual DataTable GetReportTable()
		{
			return _dsReport.Tables["Results"];
		}

		public object getReportParam(string ParamName)
		{
			if (_reportParams.ContainsKey(ParamName))
				return _reportParams[ParamName];
			else
				throw new ReportException(String.Format("�������� '{0}' �� ������.", ParamName));
		}

		public bool reportParamExists(string ParamName)
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

		public string GetProductNameSubquery(string productIdAlias)
		{
			return GetFullFormSubquery(productIdAlias, true);
		}

		public string GetFullFormSubquery(string productIdAlias)
		{
			return GetFullFormSubquery(productIdAlias, false);
		}

		public string GetCatalogProductNameSubquery(string productIdAlias)
		{
			return String.Format(@"
(
	select catalog.Name
	from catalogs.products
		join catalogs.catalog on catalog.Id = products.CatalogId
	where products.Id = {0}
)
", productIdAlias);
		}

		protected string GetFullFormSubquery(string productIdAlias, bool includeName)
		{
			var name = "";
			if (includeName)
				name = "CatalogNames.Name, ' ', CatalogForms.Form, ' ',";
			else
				name = "CatalogForms.Form, ' ',";

			return String.Format(@"
(
	select
	concat({1}
		cast(GROUP_CONCAT(ifnull(PropertyValues.Value, '')
						order by Properties.PropertyName, PropertyValues.Value
						SEPARATOR ', '
						) as char))
	from
		(
			catalogs.products,
			catalogs.catalog,
			catalogs.CatalogForms,
			catalogs.CatalogNames
		)
		left join catalogs.ProductProperties on ProductProperties.ProductId = Products.Id
		left join catalogs.PropertyValues on PropertyValues.Id = ProductProperties.PropertyValueId
		left join catalogs.Properties on Properties.Id = PropertyValues.PropertyId
	where
		products.Id = {0}
	and catalog.Id = products.CatalogId
	and CatalogForms.Id = catalog.FormId
	and CatalogNames.Id = catalog.NameId
)
", productIdAlias, name);
		}

		protected string GetClientsNamesFromSQL(List<ulong> equalValues)
		{
			var filterStr = new StringBuilder("(");
			equalValues.ForEach(val => filterStr.Append(val).Append(','));
			filterStr[filterStr.Length - 1] = ')';

			var valuesList = new List<string>();
			args.DataAdapter.SelectCommand.CommandText = String.Format(
@"
select 
	c.Name
from 
	Customers.Clients c
where 
	c.Id in {0}
order by 1", filterStr);
			args.DataAdapter.SelectCommand.Parameters.Clear();
			var dtValues = new DataTable();
			args.DataAdapter.Fill(dtValues);
			foreach (DataRow dr in dtValues.Rows)
				valuesList.Add(dr[0].ToString());

			return String.Join(", ", valuesList.ToArray());
		}

		public void ToLog(ulong generalReportCode, string errDesc = null)
		{
			_dtStop = DateTime.Now;
			ReportResultLog.Log(generalReportCode, ReportCode, _dtStart, _dtStop, errDesc);
		}

		protected void LoadAdditionFiles()
		{
			var name = "DescriptionFile";
			if (reportParamExists(name))
			{
				var file = (string)getReportParam(name);
				if (!String.IsNullOrEmpty(file))
				{
					var sourceFile = Path.Combine(Settings.Default.SavedFilesPath, _reportParamsIds[name].ToString());
					AdditionalFiles.Add(file, sourceFile);
				}
			}
		}
	}
}
