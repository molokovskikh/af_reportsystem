using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using Microsoft.Office.Interop.Excel;
using NHibernate;
using log4net;
using MySql.Data.MySqlClient;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem
{
	public class EmptyList<T> : IList<T>
	{
		public IEnumerator<T> GetEnumerator()
		{
			return Enumerable.Empty<T>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
		}

		public void Clear()
		{
		}

		public bool Contains(T item)
		{
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
		}

		public bool Remove(T item)
		{
			return false;
		}

		public int Count => 0;
		public bool IsReadOnly => false;
		public int IndexOf(T item)
		{
			return -1;
		}

		public void Insert(int index, T item)
		{
		}

		public void RemoveAt(int index)
		{
		}

		public T this[int index]
		{
			get { return default(T); }
			set {  }
		}
	}

	public enum ReportFormats
	{
		Excel,
		DBF,
		CSV
	}

	//Содержит названия полей, используемых при создании общего отчета
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

		protected MySqlDataAdapter DataAdapter;
		public MySqlConnection Connection;

		protected Dictionary<string, object> _reportParams = new Dictionary<string, object>();

		protected ILog Logger;

		protected DateTime _dtStart; // время запуска отчета
		protected DateTime _dtStop; // время завершения работы отчета

		public bool Interval;
		public DateTime From;
		public DateTime To;
		public ISession Session;

		//для тестов
		public bool CheckEmptyData = true;

		public ulong ReportCode { get; set; }
		public string ReportCaption { get; set; }

		public virtual bool DbfSupported { get; set; }

		//Фильтр, наложенный на рейтинговый отчет. Будет выводится на странице отчета
		public IList<string> Header = new List<string>();

		public List<ColumnGroupHeader> GroupHeaders = new List<ColumnGroupHeader>();
		public List<string> Warnings = new List<string>();
		//тема для отправляемого письма
		//работает если отправлять каждый файл отдельным письмом
		public Dictionary<string, string> MailMetaOverride = new Dictionary<string, string>();

		[Description("Скрыть заголовок")]
		public bool HideHeader;

		protected BaseReport() // конструктор для возможности тестирования
		{
			_dtStart = DateTime.Now;
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
			Connection = connection;

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

		protected abstract void GenerateReport();

		public virtual void ReadReportParams()
		{
			foreach (var property in GetType().GetProperties()) {
				if (ReportParamExists(property.Name)) {
					var value = GetReportParam(property.Name);
					if (value == null)
						continue;
					if (!property.PropertyType.IsInstanceOfType(value))
						value = Convert.ChangeType(value, property.PropertyType);
					property.SetValue(this, value, null);
				}
			}
			foreach (var field in GetType().GetFields()) {
				if (ReportParamExists(field.Name)) {
					var value = GetReportParam(field.Name);
					if (value == null)
						continue;
					if (!field.FieldType.IsInstanceOfType(value)) {
						if (field.FieldType.IsEnum) {
							value = Enum.ToObject(field.FieldType, Convert.ChangeType(value, typeof(int)));
						} else {
							value = Convert.ChangeType(value, field.FieldType);
						}
					}
					field.SetValue(this, value);
				}
			}

			if (HideHeader)
				Header = new EmptyList<string>();
		}

		public void ProcessReport()
		{
			_dtStart = DateTime.Now;
			With.DeadlockWraper(() => {
				DataAdapter = new MySqlDataAdapter("", Connection);
				_dsReport.Clear();
				GenerateReport();
			});
		}

		public virtual void Write(string fileName)
		{
			ReadReportParams();
			ProcessReport();
			var reportTable = GetReportTable();
			if (IsEmpty(reportTable))
				throw new Exception(@"В результате подготовки отчета получился пустой набор данных
1. если это отчет по заказам, то возможно за выбранный период нет заказов или же данные за выбранный период не были импортированы (нужно проверить таблицу ordersold)
2. если это отчет по динамике цен, то возможно не были подготовлены данные, нужно проверить, заполнены ли соответствующие таблицы данными
3. если это отчет по предложениям, то нужно проверить настройки отчета возможно в них ошибка. Так, например:
а) для отчета, формируемом по базовым ценам, нужно убедиться, что ценовая колонка, настроенная в системе, как базовая присутствует в прайс-листе поставщика");

			var writer = GetWriter(Format);
			if (writer != null) {
				// Новый механизм, выносим часть для выгрузки в файл в отдельный класс
				var settings = GetSettings();
				writer.WriteReportToFile(_dsReport, fileName, settings);
				Warnings.AddRange(writer.Warnings);
				return;
			}

			if (Format == ReportFormats.DBF) {
				if(!DbfSupported)
					throw new ReportException("Подотчет не может готовиться в формате DBF.");
				// Формируем DBF
				fileName = Path.Combine(Path.GetDirectoryName(fileName), ReportCaption + ".dbf");
				ProfileHelper.Next("DataTableToDbf");
				DataTableToDbf(reportTable, fileName);
			}
			else if (Format == ReportFormats.CSV) {
				if (!DbfSupported)
					throw new ReportException("Подотчет не может готовиться в формате CSV.");
				//Формируем CSV
				ProfileHelper.Next("CsvHelper.Save");
				fileName = Path.Combine(Path.GetDirectoryName(fileName), ReportCaption + ".csv");
				CsvHelper.Save(reportTable, fileName);
			}
			else {
				// Формируем Excel
				ProfileHelper.Next("DataTableToExcel");
				DataTableToExcel(reportTable, fileName);
				if (File.Exists(fileName)) {
					ProfileHelper.Next("FormatExcel");
					FormatExcel(fileName);
				}
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

			//если таблицы с данными нет то значит в отчете происходит
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
			new BaseExcelWriter().DataTableToExcel(dtExport, exlFileName, ReportCode);
		}

		protected virtual void FormatExcel(string fileName)
		{
			Application exApp = new ApplicationClass();
			try {
				exApp.DisplayAlerts = false;
				var wb = exApp.Workbooks.Open(fileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
					Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
					Type.Missing);
				_Worksheet ws;
				try {
					ws = (_Worksheet)wb.Worksheets["rep" + ReportCode];

					try {
						ws.Name = GetSheetName();

						var res = _dsReport.Tables["Results"];
						if (res == null)
							throw new ReportException($"Данные для отчета не сформирована, возможно отчет не может быть подготовлен в формате {Format}");
						var tableBegin = 1 + Header.Count;
						var groupedHeadersLine = tableBegin;
						if (GroupHeaders.Count > 0)
							tableBegin++;

						for (var i = 0; i < res.Columns.Count; i++) {
							var dataColumn = res.Columns[i];

							ws.Cells[1, i + 1] = "";
							ws.Cells[tableBegin, i + 1] = dataColumn.Caption;
							if (dataColumn.ExtendedProperties.ContainsKey("Width"))
								((Range)ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)dataColumn.ExtendedProperties["Width"]).Value;
							else
								((Range)ws.Columns[i + 1, Type.Missing]).AutoFit();

							if (dataColumn.ExtendedProperties.ContainsKey("Color"))
								ws.Range[ws.Cells[tableBegin, i + 1], ws.Cells[res.Rows.Count + 1, i + 1]].Interior.Color = ColorTranslator.ToOle((Color)dataColumn.ExtendedProperties["Color"]);
						}

						//рисуем границы на всю таблицу
						ws.Range[ws.Cells[tableBegin, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count]].Borders.Weight = XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						ws.Range[ws.Cells[tableBegin, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count]].Select();
						((Range)exApp.Selection).AutoFilter(1, Missing.Value, XlAutoFilterOperator.xlAnd, Missing.Value, true);

						for (var i = 0; i < Header.Count; i++)
							ws.Cells[1 + i, 1] = Header[i];

						foreach (var groupHeader in GroupHeaders) {
							var begin = ColumnIndex(res, groupHeader.BeginColumn);
							var end = ColumnIndex(res, groupHeader.EndColumn);
							if (begin == 0 || end == 0)
								continue;
							var range = ws.Range[ws.Cells[groupedHeadersLine, begin], ws.Cells[groupedHeadersLine, end]];
							range.Select();
							range.Merge(null);
							range.Value2 = groupHeader.Title;
							range.HorizontalAlignment = XlHAlign.xlHAlignCenter;
							range.Borders.Weight = XlBorderWeight.xlThin;
						}

						PostProcessing(exApp, ws);
					}
					finally {
						wb.SaveAs(fileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
					}
				}
				finally {
					ws = null;
					wb = null;
					try {
						exApp.Workbooks.Close();
					}
					catch {
					}
				}
			}
			finally {
				try {
					exApp.Quit();
				}
				catch {
				}
				exApp = null;
			}
			ProfileHelper.End();
		}

		private static int ColumnIndex(DataTable res, string name)
		{
			return res.Columns.Cast<DataColumn>().IndexOf(c => String.Equals(c.ExtendedProperties["OriginalName"] as String, name, StringComparison.InvariantCultureIgnoreCase)) + 1;
		}

		/// <summary>
		/// Дополнительные действия с форматированием отчета, специфичные для отчета
		/// </summary>
		/// <param name="exApp"></param>
		/// <param name="ws"></param>
		protected virtual void PostProcessing(Application exApp, _Worksheet ws)
		{
		}

		protected virtual void DataTableToDbf(DataTable dtExport, string fileName)
		{
			using (var writer = new StreamWriter(fileName, false, Encoding.GetEncoding(866)))
				Dbf.Save(dtExport, writer);
		}

		public virtual DataTable GetReportTable()
		{
			return _dsReport.Tables["Results"];
		}

		protected string GetValuesFromSQL(string sql)
		{
			DataAdapter.SelectCommand.CommandText = sql;
			DataAdapter.SelectCommand.Parameters.Clear();
			var dtValues = new DataTable();
			DataAdapter.Fill(dtValues);

			return (from DataRow dr in dtValues.Rows select dr[0]).Implode();
		}

		public object GetReportParam(string paramName)
		{
			if (_reportParams.ContainsKey(paramName))
				return _reportParams[paramName];
			else
				throw new ReportException(String.Format("Параметр '{0}' не найден.", paramName));
		}

		public bool ReportParamExists(string paramName)
		{
			return _reportParams.ContainsKey(paramName);
		}

		protected virtual IWriter GetWriter(ReportFormats format)
		{
			return null;
		}

		protected virtual BaseReportSettings GetSettings()
		{
			return null;
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

		protected string GetClientsNamesFromSQL(List<ulong> equalValues)
		{
			var filterStr = new StringBuilder("(");
			equalValues.ForEach(val => filterStr.Append(val).Append(','));
			filterStr[filterStr.Length - 1] = ')';

			var valuesList = new List<string>();
			DataAdapter.SelectCommand.CommandText = String.Format(
				@"
select
	c.Name
from
	Customers.Clients c
where
	c.Id in {0}
order by 1", filterStr);
			DataAdapter.SelectCommand.Parameters.Clear();
			var dtValues = new DataTable();
			DataAdapter.Fill(dtValues);
			foreach (DataRow dr in dtValues.Rows)
				valuesList.Add(dr[0].ToString());

			return String.Join(", ", valuesList.ToArray());
		}

		protected string GetSqlFromSuppliers(List<ulong> ids)
		{
			return @"
select concat(supps.Name, ' - ', rg.Region) as FirmShortName
from
Customers.suppliers supps,
farm.regions rg
where
rg.RegionCode = supps.HomeRegion and supps.Id in (" +
				ids.Implode() +
				") order by supps.Name";
		}

		protected string GetSqlFromPrices(List<ulong> ids)
		{
			return @"
SELECT concat(p.PriceName, ' (', s.Name, ' - ', rg.Region, ')') FROM PricesData P
join customers.Suppliers s on s.id = p.FirmCode
join farm.regions rg on rg.RegionCode = s.HomeRegion
where p.PriceCode in (" +
				ids.Implode() +
				") order by p.PriceName";
		}

		protected string GetSqlFromRegions(List<ulong> ids)
		{
			return @"
select rg.Region from farm.regions rg
where rg.RegionCode in (" +
				ids.Implode() +
				") order by rg.Region";
		}

		public void ToLog(ulong generalReportCode, string errDesc = null)
		{
			_dtStop = DateTime.Now;
			ReportResultLog.Log(generalReportCode, ReportCode, _dtStart, _dtStop, errDesc);
		}

		public string GetSheetName()
		{
			return ExcelHelper.GetSheetName(ReportCaption);
		}

		public override string ToString()
		{
			return $"Отчет {ReportCode} {ReportCaption} ${base.ToString()}";
		}
	}
}
