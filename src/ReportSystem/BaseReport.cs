using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using Inforoom.ReportSystem.Properties;
using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using NHibernate;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem
{ //Костыль т.к. не используем ActiveRecord модели, то пришлось копировать enum
	public enum ReportFormats
	{
		Excel,
		DBF,
		CSV
	}

	//Содержит названия полей, используемых при создании общего очета
	public sealed class BaseReportColumns
	{
		public const string colReportCode = "ReportCode";
		//public const string colSendFile = "SendFile";
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
		private Dictionary<string, uint> _reportParamsIds = new Dictionary<string, uint>();

		public const int MaxStringSize = 250;

		public const int MaxListName = 26;

		protected DataSet _dsReport;

		//Таблица с загруженными свойствами отчета
		protected DataTable dtReportProperties;
		//Таблица с загруженными значениями списков-свойств
		protected DataTable dtReportPropertyValues;
		//Формат файла отчета
		protected ReportFormats Format;

		protected MySqlConnection _conn;

		protected Dictionary<string, object> _reportParams;

		protected ExecuteArgs args;

		protected ILog Logger;

		protected DateTime _dtStart; // время запуска отчета
		protected DateTime _dtStop; // время завершения работы отчета

		public bool Interval;
		public DateTime From;
		public DateTime To;
		protected ISession Session;

		//для тестов
		public bool CheckEmptyData = true;

		public ulong ReportCode { get; protected set; }
		public string ReportCaption { get; protected set; }

		public virtual bool DbfSupported { get; set; }

		protected BaseReport() // конструктор для возможности тестирования
		{
		}

		public BaseReport(ulong reportCode, string reportCaption, MySqlConnection connection, ReportFormats format, DataSet dsProperties)
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

			foreach (DataRow drProperty in dtReportProperties.Rows) {
				var currentPropertyName = drProperty[BaseReportColumns.colPropertyName].ToString();

				if (!_reportParams.ContainsKey(currentPropertyName)) {
					_reportParamsIds.Add(currentPropertyName, Convert.ToUInt32(drProperty[BaseReportColumns.colPropertyID]));
					switch (drProperty[BaseReportColumns.colPropertyType].ToString()) {
						case "BOOL":
							try {
								_reportParams.Add(currentPropertyName, Convert.ToBoolean(Convert.ToByte(drProperty[BaseReportColumns.colPropertyValue])));
							}
							catch (Exception ex) {
								throw new ReportException(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'.",
									drProperty[BaseReportColumns.colPropertyType],
									drProperty[BaseReportColumns.colPropertyValue]), ex);
							}
							break;

						case "LIST":
							var listValues = new List<ulong>();
							var drValues = dtReportPropertyValues.Select(BaseReportColumns.colReportPropertyID + "=" + drProperty[BaseReportColumns.colPropertyID].ToString());
							foreach (DataRow drValue in drValues) {
								try {
									listValues.Add(Convert.ToUInt64(drValue[BaseReportColumns.colReportPropertyValue]));
								}
								catch (Exception ex) {
									throw new ReportException(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'.",
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
							try {
								if (drProperty[BaseReportColumns.colPropertyValue].ToString().Equals("NOW", StringComparison.OrdinalIgnoreCase))
									_reportParams.Add(currentPropertyName, DateTime.Now);
								else
									_reportParams.Add(currentPropertyName, DateTime.ParseExact(drProperty[BaseReportColumns.colPropertyValue].ToString(), MySqlConsts.MySQLDateFormat, null));
							}
							catch (Exception ex) {
								throw new ReportException(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'.",
									drProperty[BaseReportColumns.colPropertyType],
									drProperty[BaseReportColumns.colPropertyValue]), ex);
							}
							break;

						case "INT":
						case "ENUM":
							try {
								string val = drProperty[BaseReportColumns.colPropertyValue].ToString();
								if (!String.IsNullOrEmpty(val))
									_reportParams.Add(currentPropertyName, Convert.ToInt32(drProperty[BaseReportColumns.colPropertyValue].ToString()));
							}
							catch (Exception ex) {
								throw new ReportException(String.Format("Ошибка при конвертации параметра '{0}' из строки '{1}'.",
									drProperty[BaseReportColumns.colPropertyType],
									drProperty[BaseReportColumns.colPropertyValue]), ex);
							}
							break;

						default:
							throw new ReportException(String.Format("Неизвестный тип параметра : '{0}'.", drProperty[BaseReportColumns.colPropertyType].ToString()));
					}
				}
				else {
					throw new ReportException(String.Format("Параметр '{0}' задан дважды.", currentPropertyName));
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
			using(new SessionScope()) {
				ArHelper.WithSession(s => {
					Session = s;
					GenerateReport(e);
				});
			}
			return true;
		}

		public virtual void ReportToFile(string fileName)
		{
			var reportTable = GetReportTable();
			if (IsEmpty(reportTable))
				throw new Exception("В результате подготовки отчета получился пустой набор данных"
					+ "\r\nэсли это отчет по заказам то возможно не были импортированы данные за выбраный период, нужно проверить ordersold"
					+ "\r\nесли это отчет по динамики цен то возможно не были подготовленны данные"
					+ "\r\nесли это отчет по предложениям то нужно проверить настройки отчета возможно в них ошибка");

			var writer = GetWriter(Format);
			if (writer != null) {
				// Новый механизм, выносим часть для выгрузки в файл в отдельный класс
				var settings = GetSettings();
				writer.WriteReportToFile(_dsReport, fileName, settings);
				return;
			}

			if (Format == ReportFormats.DBF && DbfSupported) {
				// Формируем DBF
				fileName = Path.Combine(Path.GetDirectoryName(fileName), ReportCaption + ".dbf");
				DataTableToDbf(reportTable, fileName);
			}
			else if (Format == ReportFormats.CSV && DbfSupported) {
				fileName = Path.Combine(Path.GetDirectoryName(fileName), ReportCaption + ".csv");
				CsvHelper.Save(reportTable, fileName);
			}
			else {
				// Формируем Excel
				DataTableToExcel(reportTable, fileName);
				FormatExcel(fileName);
			}
		}

		//таблица будет пустой в двух случаях
		//когда она на самом деле пустая и если она содержит только пусты строки
		//пустые строки получатся из-за того что некоторые отчеты их формируют для того что бы
		//зарезервировать место под заголовок отчета
		private bool IsEmpty(DataTable reportTable)
		{
			if (!CheckEmptyData)
				return false;

			//если таблицы с данными нет то значит в отчете проиходит
			//что то специальное
			if (reportTable == null)
				return false;

			if (reportTable.Rows.Count == 0)
				return true;

			if (reportTable.Rows.Count < 100) {
				return reportTable.AsEnumerable().All(r => reportTable.Columns.Cast<DataColumn>().All(c => r[c] is DBNull));
			}
			return false;
		}

		protected virtual void DataTableToExcel(DataTable dtExport, string exlFileName)
		{
			ProfileHelper.Next("DataTableToExcel");
			new BaseExcelWriter().DataTableToExcel(dtExport, exlFileName, ReportCode);
		}

		protected virtual void FormatExcel(string fileName)
		{
		}

		protected virtual void DataTableToDbf(DataTable dtExport, string fileName)
		{
			using (var writer = new StreamWriter(fileName, false, Encoding.GetEncoding(866)))
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
				throw new ReportException(String.Format("Параметр '{0}' не найден.", ParamName));
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
",
				productIdAlias);
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
",
				productIdAlias, name);
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
	}
}
