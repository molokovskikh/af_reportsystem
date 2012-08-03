using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using Inforoom.ReportSystem.Filters;
using ExecuteTemplate;
using System.Data;
using DataTable = System.Data.DataTable;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	public class ColumnGroupHeader
	{
		public ColumnGroupHeader(string title, string beginColumn, string endColumn)
		{
			Title = title;
			BeginColumn = beginColumn;
			EndColumn = endColumn;
		}

		public string Title;
		public string BeginColumn;
		public string EndColumn;
	}

	public class OrdersReport : BaseReport
	{
		protected const string reportIntervalProperty = "ReportInterval";
		protected const string byPreviousMonthProperty = "ByPreviousMonth";

		protected List<FilterField> registredField;
		protected List<FilterField> selectedField;

		protected DateTime dtFrom;
		protected DateTime dtTo;

		protected bool ByPreviousMonth;
		protected int _reportInterval;

		//Фильтр, наложенный на рейтинговый отчет. Будет выводится на странице отчета
		protected List<string> filterDescriptions = new List<string>();

		protected bool SupportProductNameOptimization;
		protected bool includeProductName;
		protected bool isProductName = true;
		protected bool firmCrPosition; // есть ли параметр "Позиция производителя"
		protected string OrdersSchema = "Orders";

		protected List<ColumnGroupHeader> GroupHeaders
			= new List<ColumnGroupHeader>();

		public OrdersReport()
		{}

		public OrdersReport(ulong reportCode, string reportCaption, MySqlConnection conn, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, conn, format, dsProperties)
		{
#if !DEBUG
			OrdersSchema = "OrdersOld";
#endif
		}

		protected void FillFilterFields()
		{
			registredField = new List<FilterField>();
			registredField.Add(new FilterField("p.Id", @"concat(cn.Name, cf.Form, ' ',
			  (select
				 ifnull(GROUP_CONCAT(ifnull(PropertyValues.Value, '')
									order by Properties.PropertyName, PropertyValues.Value
									SEPARATOR ', '), '')
			  from
				 catalogs.products inp
				 left join catalogs.ProductProperties on ProductProperties.ProductId = inp.Id
				 left join catalogs.PropertyValues on PropertyValues.Id = ProductProperties.PropertyValueId
				 left join catalogs.Properties on Properties.Id = PropertyValues.PropertyId
			   where inp.Id = p.Id)) as ProductName", 
				"ProductName", "ProductName", "Наименование и форма выпуска", 
				"catalogs.products p, catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf", 
				"and c.Id = p.CatalogId and cn.id = c.NameId and cf.Id = c.FormId", 0, 
				"В отчет включены следующие продукты", "Следующие продукты исключены из отчета", 40));

			registredField.Add(new FilterField("c.Id", "concat(cn.Name, ' ', cf.Form) as CatalogName", "CatalogName", "FullName", "Наименование и форма выпуска", "catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf", "and cn.id = c.NameId and cf.Id = c.FormId", 0, "В отчет включены следующие наименования", "Следующие наименования исключены из отчета", 40));
			registredField.Add(new FilterField("cn.Id", "cn.Name as PosName", "PosName", "ShortName", "Наименование", "catalogs.catalognames cn", null, 0, "В отчет включены следующие наименования", "Следующие наименования исключены из отчета", 40));
			registredField.Add(new FilterField("cfc.Id", "cfc.Name as FirmCr", "FirmCr", "FirmCr", "Производитель", "catalogs.Producers cfc", null, 1, "В отчет включены следующие производители", "Следующие производители исключены из отчета", 15));
			registredField.Add(new FilterField("rg.RegionCode", "rg.Region as RegionName", "RegionName", "Region", "Регион", "farm.regions rg", null, 2, "В отчет включены следующие регионы", "Следующие регионы исключены из отчета"));
			registredField.Add(new FilterField("prov.Id", "concat(prov.Name, ' - ', provrg.Region) as FirmShortName", "FirmShortName", "FirmCode", "Поставщик", "Customers.suppliers prov, farm.regions provrg", "and prov.HomeRegion = provrg.RegionCode", 3, "В отчет включены следующие поставщики", "Следующие поставщики исключены из отчета", 10));
			registredField.Add(new FilterField("pd.PriceCode", "concat(prov.Name , ' (', pd.PriceName, ') - ', provrg.Region) as PriceName", "PriceName", "PriceCode", "Прайс-лист", "usersettings.pricesdata pd, Customers.suppliers prov, farm.regions provrg", "and prov.Id = pd.FirmCode and prov.HomeRegion = provrg.RegionCode", 4, "В отчет включены следующие прайс-листы поставщиков", "Следующие прайс-листы поставщиков исключены из отчета", 10));
			registredField.Add(new FilterField("cl.Id", "cl.Name as ClientShortName", "ClientShortName", "ClientCode", "Аптека", "Customers.clients cl", null, 5, "В отчет включены следующие аптеки", "Следующие аптеки исключены из отчета", 10));
			registredField.Add(new FilterField("payers.PayerId", "payers.ShortName as PayerName", "PayerName", "Payer", "Плательщик", "billing.payers", null, 6, "В отчет включены следующие плательщики", "Следующие плательщики исключены из отчета"));
			registredField.Add(new FilterField("ad.Id", "concat(ad.Address, ' (', cl.Name, ') ') as AddressName", "AddressName", "Addresses", "Адрес доставки", "customers.addresses ad, Customers.Clients cl", "and ad.ClientId = cl.Id", 7, "В отчет включены следующие адреса доставки", "Следующие адреса доставки исключены из отчета"));
		}

		public override void ReadReportParams()
		{
			ByPreviousMonth = (bool)getReportParam(byPreviousMonthProperty);
			if (Interval)
			{
				dtFrom = From;
				dtTo = To;
				dtTo = dtTo.Date.AddDays(1);
			}
			else if (ByPreviousMonth)
			{
				dtTo = DateTime.Now;
				dtTo = dtTo.AddDays(-(dtTo.Day - 1)).Date; // Первое число текущего месяца
				dtFrom = dtTo.AddMonths(-1).Date;
			}
			else
			{
				_reportInterval = (int)getReportParam(reportIntervalProperty);
				dtTo = DateTime.Now;
				//От текущей даты вычитаем интервал - дата начала отчета
				dtFrom = dtTo.AddDays(-_reportInterval).Date;
				//К текущей дате 00 часов 00 минут является окончанием периода и ее в отчет не включаем
				dtTo = dtTo.Date;
			}
			filterDescriptions.Add(String.Format("Период дат: {0} - {1}", dtFrom.ToString("dd.MM.yyyy HH:mm:ss"), dtTo.ToString("dd.MM.yyyy HH:mm:ss")));

			LoadFilters();
			CheckAfterLoadFields();

			selectedField.Sort((x, y) => (x.position - y.position));
		}

		protected void LoadFilters()
		{
			FillFilterFields();
			selectedField = registredField.Where(f => f.LoadFromDB(this)).ToList();
		}

		protected virtual void CheckAfterLoadFields()
		{
			firmCrPosition = reportParamExists("FirmCrPosition");
		}

		protected string GetValuesFromSQL(string SQL)
		{
			args.DataAdapter.SelectCommand.CommandText = SQL;
			args.DataAdapter.SelectCommand.Parameters.Clear();
			var dtValues = new DataTable();
			args.DataAdapter.Fill(dtValues);

			return (from DataRow dr in dtValues.Rows select dr[0]).Implode();
		}

		public override void GenerateReport(ExecuteArgs e)
		{}

		protected override void FormatExcel(string fileName)
		{
			ProfileHelper.Next("FormatExcel");
			Application exApp = new ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				var wb = exApp.Workbooks.Open(fileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
					Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
					Type.Missing);
				_Worksheet ws;
				try
				{
					ws = (_Worksheet) wb.Worksheets["rep" + ReportCode];

					try
					{
						ws.Name = ReportCaption.Substring(0, (ReportCaption.Length < MaxListName) ? ReportCaption.Length : MaxListName);

						var res = _dsReport.Tables["Results"];
						var tableBegin = 1 + filterDescriptions.Count;
						var groupedHeadersLine = tableBegin;
						if (GroupHeaders.Count > 0)
							tableBegin++;

						for (var i = 0; i < res.Columns.Count; i++)
						{
							var dataColumn = res.Columns[i];

							ws.Cells[1, i + 1] = "";
							ws.Cells[tableBegin, i + 1] = dataColumn.Caption;
							if (dataColumn.ExtendedProperties.ContainsKey("Width"))
								((Range) ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?) dataColumn.ExtendedProperties["Width"]).Value;
							else
								((Range) ws.Columns[i + 1, Type.Missing]).AutoFit();

							if (dataColumn.ExtendedProperties.ContainsKey("Color"))
								ws.Range[ws.Cells[tableBegin, i + 1], ws.Cells[res.Rows.Count + 1, i + 1]].Interior.Color = ColorTranslator.ToOle((Color) dataColumn.ExtendedProperties["Color"]);
						}

						//рисуем границы на всю таблицу
						ws.Range[ws.Cells[tableBegin, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count]].Borders.
							Weight = XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						ws.Range[ws.Cells[tableBegin, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count]].Select();
						((Range) exApp.Selection).AutoFilter(1, Missing.Value, XlAutoFilterOperator.xlAnd, Missing.Value, true);

						for (var i = 0; i < filterDescriptions.Count; i++)
							ws.Cells[1 + i, 1] = filterDescriptions[i];

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
					finally
					{
						wb.SaveAs(fileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
					}
				}
				finally
				{
					ws = null;
					wb = null;
					try { exApp.Workbooks.Close(); }
					catch { }
				}
			}
			finally
			{
				try { exApp.Quit(); }
				catch { }
				exApp = null;
			}
			ProfileHelper.End();
		}

		private static int ColumnIndex(DataTable res, string name)
		{
			return res.Columns.Cast<DataColumn>()
				.IndexOf(c => String.Equals(c.ExtendedProperties["OriginalName"] as String, name, StringComparison.InvariantCultureIgnoreCase)) + 1;
		}

		/// <summary>
		/// Дополнительные действия с форматированием отчета, специфичные для отчета
		/// </summary>
		/// <param name="exApp"></param>
		/// <param name="ws"></param>
		protected virtual void PostProcessing(Application exApp, _Worksheet ws)
		{}

		protected string ApplyFilters(string selectCommand)
		{
			FillFilterDescriptions();
			selectCommand = ApplyUserFilters(selectCommand);

			selectCommand = String.Concat(selectCommand, String.Format(Environment.NewLine + "and (oh.WriteTime > '{0}')", dtFrom.ToString(MySqlConsts.MySQLDateFormat)));
			selectCommand = String.Concat(selectCommand, String.Format(Environment.NewLine + "and (oh.WriteTime < '{0}')", dtTo.ToString(MySqlConsts.MySQLDateFormat)));

			return selectCommand;
		}

		protected string ApplyUserFilters(string selectCommand)
		{
			foreach (var rf in selectedField)
			{
				if (rf.equalValues != null && rf.equalValues.Count > 0)
					selectCommand = String.Concat(selectCommand, Environment.NewLine + "and ", rf.GetEqualValues());
				if ((rf.nonEqualValues != null) && (rf.nonEqualValues.Count > 0))
					selectCommand = String.Concat(selectCommand, Environment.NewLine + "and ", rf.GetNonEqualValues());
			}
			return selectCommand;
		}

		protected void FillFilterDescriptions()
		{
			foreach (var field in selectedField)
			{
				if (field.nonEqualValues != null && field.nonEqualValues.Count > 0)
					filterDescriptions.Add(String.Format("{0}: {1}", field.nonEqualValuesCaption, ReadNames(field, field.nonEqualValues)));
				if (field.equalValues != null && field.equalValues.Count > 0)
					filterDescriptions.Add(String.Format("{0}: {1}", field.equalValuesCaption, GetValuesFromSQL(field.GetNamesSql(field.equalValues))));
			}
		}

		protected string ReadNames(FilterField field, List<ulong> ids)
		{
			return GetValuesFromSQL(field.GetNamesSql(ids));
		}

		protected string BuildSelect()
		{
			var selectCommand = "";
			if (SupportProductNameOptimization)
			{
				foreach (var rf in selectedField) // В целях оптимизации при некоторых случаях используем
					if (rf.visible && (rf.reportPropertyPreffix == "ProductName" || // временные таблицы
						rf.reportPropertyPreffix == "FullName"))
					{
						rf.primaryField = "ol.Productid";
						rf.viewField = "ol.Productid as pid";
						includeProductName = true;
						if (rf.reportPropertyPreffix == "FullName")
						{
							rf.primaryField = "p.CatalogId";
							rf.viewField = "p.CatalogId as pid";
							isProductName = false;
						}
					}

				if (includeProductName)
					selectCommand = @"
drop temporary table IF EXISTS MixedData;
create temporary table MixedData ENGINE=MEMORY
";
			}

			return selectCommand + selectedField.Where(rf => rf.visible).Aggregate("select ", (current, rf) => String.Concat(current, rf.primaryField, ", ", rf.viewField, ", "));
		}

		protected string ApplyGroupAndSort(string selectCommand, string sort)
		{
			selectCommand = String.Concat(selectCommand, Environment.NewLine + "group by ", String.Join(",", (from rf in selectedField where rf.visible select rf.primaryField).ToArray()));
			selectCommand = String.Concat(selectCommand, Environment.NewLine + String.Format("order by {0}", sort));
			return selectCommand;
		}

		protected DataTable BuildResultTable(DataTable selectTable)
		{
			var res = new DataTable();
			foreach (var rf in selectedField.Where(f => f.visible))
			{
				var dc = res.Columns.Add(rf.outputField, selectTable.Columns[rf.outputField].DataType);
				dc.Caption = rf.outputCaption;
				if (rf.width.HasValue)
					dc.ExtendedProperties.Add("Width", rf.width);
			}

			//Добавляем несколько пустых строк, чтобы потом вывести в них значение фильтра в Excel
			var emptyRowCount = EmptyRowCount;
			for (var i = 0; i < emptyRowCount; i++)
				res.Rows.InsertAt(res.NewRow(), 0);

			res = res.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
			return res;
		}

		protected int EmptyRowCount
		{
			get { return GroupHeaders.Count + (filterDescriptions.Count > 0 ? 1 : 0); }
		}

		protected void CopyData(DataTable source, DataTable destination)
		{
			int visbleCount = selectedField.Count(x => x.visible);
			destination.BeginLoadData();
			foreach (DataRow dr in source.Rows)
			{
				var newrow = destination.NewRow();

				foreach (var rf in selectedField)
					if (rf.visible)
						newrow[rf.outputField] = dr[rf.outputField];

				//Выставляем явно значения определенного типа для полей: "Сумма", "Доля рынка в %" и т.д.
				//(visbleCount * 2) - потому, что участвует код (первичный ключ) и строковое значение,
				//пример: PriceCode и PriceName.
				for (int i = (visbleCount * 2); i < source.Columns.Count; i++)
				{
					if (!(dr[source.Columns[i].ColumnName] is DBNull) && destination.Columns.Contains(source.Columns[i].ColumnName))
						newrow[source.Columns[i].ColumnName] = Convert.ChangeType(dr[source.Columns[i].ColumnName], destination.Columns[source.Columns[i].ColumnName].DataType);
				}

				destination.Rows.Add(newrow);
			}
			destination.EndLoadData();
		}
	}
}
