using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using Inforoom.ReportSystem.Filters;
using ExecuteTemplate;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	public class OrdersReport : BaseReport
	{
		protected const string fromProperty = "StartDate";
		protected const string toProperty = "EndDate";
		protected const string reportIntervalProperty = "ReportInterval";
		protected const string byPreviousMonthProperty = "ByPreviousMonth";

		protected List<FilterField> registredField;
		protected List<FilterField> selectedField;

		protected DateTime dtFrom;
		protected DateTime dtTo;

		protected bool ByPreviousMonth;
		protected int _reportInterval;


		//Фильтр, наложенный на рейтинговый отчет. Будет выводится на странице отчета
		protected List<string> filter;

		public OrdersReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary)
			: base(ReportCode, ReportCaption, Conn, Temporary)
		{
		}

		protected void FillFilterFields()
		{
			registredField = new List<FilterField>();
			registredField.Add(new FilterField("p.Id", "concat(cn.Name, ' ', catalogs.GetFullForm(p.Id)) as ProductName", "ProductName", "ProductName", "Наименование и форма выпуска", "catalogs.products p, catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf", "and c.Id = p.CatalogId and cn.id = c.NameId and cf.Id = c.FormId", 0, "В отчет включены следующие продукты", "Следующие продукты исключены из отчета", 40));
			registredField.Add(new FilterField("c.Id", "concat(cn.Name, ' ', cf.Form) as CatalogName", "CatalogName", "FullName", "Наименование и форма выпуска", "catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf", "and cn.id = c.NameId and cf.Id = c.FormId", 0, "В отчет включены следующие наименования", "Следующие наименования исключены из отчета", 40));
			registredField.Add(new FilterField("cn.Id", "cn.Name as PosName", "PosName", "ShortName", "Наименование", "catalogs.catalognames cn", null, 0, "В отчет включены следующие наименования", "Следующие наименования исключены из отчета", 40));
			registredField.Add(new FilterField("cfc.Id", "cfc.Name as FirmCr", "FirmCr", "FirmCr", "Производитель", "catalogs.Producers cfc", null, 1, "В отчет включены следующие производители", "Следующие производители исключены из отчета", 15));
			registredField.Add(new FilterField("rg.RegionCode", "rg.Region as RegionName", "RegionName", "Region", "Регион", "farm.regions rg", null, 2, "В отчет включены следующие регионы", "Следующие регионы исключены из отчета"));
			registredField.Add(new FilterField("prov.FirmCode", "concat(prov.ShortName, ' - ', rg.Region) as FirmShortName", "FirmShortName", "FirmCode", "Поставщик", "usersettings.clientsdata prov, farm.regions rg", "and prov.RegionCode = rg.RegionCode", 3, "В отчет включены следующие поставщики", "Следующие поставщики исключены из отчета", 10));
			registredField.Add(new FilterField("pd.PriceCode", "concat(prov.ShortName , ' (', pd.PriceName, ') - ', rg.Region) as PriceName", "PriceName", "PriceCode", "Прайс-лист", "usersettings.pricesdata pd, usersettings.clientsdata prov, farm.regions rg", "and prov.FirmCode = pd.FirmCode and prov.RegionCode = rg.RegionCode", 4, "В отчет включены следующие прайс-листы поставщиков", "Следующие прайс-листы поставщиков исключены из отчета", 10));
			registredField.Add(new FilterField("cd.FirmCode", "cd.ShortName as ClientShortName", "ClientShortName", "ClientCode", "Аптека", "usersettings.clientsdata cd", null, 5, "В отчет включены следующие аптеки", "Следующие аптеки исключены из отчета", 10));
			registredField.Add(new FilterField("payers.PayerId", "payers.ShortName as PayerName", "PayerName", "Payer", "Плательщик", "billing.payers", null, 6, "В отчет включены следующие плательщики", "Следующие плательщики исключены из отчета"));
		}

		public override void ReadReportParams()
		{
			FillFilterFields();
			filter = new List<string>();

			ByPreviousMonth = (bool)getReportParam(byPreviousMonthProperty);
			if (_parentIsTemporary)
			{
				dtFrom = ((DateTime)getReportParam(fromProperty)).Date;
				dtTo = (DateTime)getReportParam(toProperty);
				dtTo = dtTo.Date.AddDays(1);
			}
			else
				if (ByPreviousMonth)
				{
					dtTo = DateTime.Now;
					dtTo = dtTo.AddDays(-(dtTo.Day - 1)).Date;
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
			filter.Add(String.Format("Период дат: {0} - {1}", dtFrom.ToString("dd.MM.yyyy HH:mm:ss"), dtTo.ToString("dd.MM.yyyy HH:mm:ss")));

			selectedField = new List<FilterField>();
			foreach (FilterField rf in registredField)
			{
				if (rf.LoadFromDB(this))
					selectedField.Add(rf);
			}

			CheckAfterLoadFields();

			selectedField.Sort(delegate(FilterField x, FilterField y) { return (x.position - y.position); });
		}

		protected virtual void CheckAfterLoadFields()
		{
			if (!selectedField.Exists(delegate(FilterField x) { return x.visible; }))
				throw new Exception("Не выбраны поля для отображения в заголовке отчета.");
		}

		protected string GetValuesFromSQL(ExecuteArgs e, string SQL)
		{
			List<string> valuesList = new List<string>();
			e.DataAdapter.SelectCommand.CommandText = SQL;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			DataTable dtValues = new DataTable();
			e.DataAdapter.Fill(dtValues);
			foreach (DataRow dr in dtValues.Rows)
				valuesList.Add(dr[0].ToString());

			return String.Join(", ", valuesList.ToArray());
		}

		public override void GenerateReport(ExecuteArgs e)
		{
		}

		public override void ReportToFile(string FileName)
		{
			DataTableToExcel(_dsReport.Tables["Results"], FileName);
			FormatExcel(FileName);
		}

		protected void FormatExcel(string FileName)
		{
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				MSExcel.Workbook wb = exApp.Workbooks.Open(FileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try
				{
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

					try
					{
						ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);

						DataTable res = _dsReport.Tables["Results"];
						for (int i = 0; i < res.Columns.Count; i++)
						{
							ws.Cells[1, i + 1] = "";
							ws.Cells[1 + filter.Count, i + 1] = res.Columns[i].Caption;
							if (res.Columns[i].ExtendedProperties.ContainsKey("Width"))
								((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)res.Columns[i].ExtendedProperties["Width"]).Value;
							else
								((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).AutoFit();
							if (res.Columns[i].ExtendedProperties.ContainsKey("Color"))
								ws.get_Range(ws.Cells[1 + filter.Count, i + 1], ws.Cells[res.Rows.Count + 1, i + 1]).Interior.Color = System.Drawing.ColorTranslator.ToOle((System.Drawing.Color)res.Columns[i].ExtendedProperties["Color"]);
						}

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[1 + filter.Count, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[1 + filter.Count, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						for (int i = 0; i < filter.Count; i++)
							ws.Cells[1 + i, 1] = filter[i];

						PostProcessing(exApp, ws);
					}
					finally
					{
						wb.SaveAs(FileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
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
		}

		/// <summary>
		/// Дополнительные действия с форматированием отчета, специфичные для отчета
		/// </summary>
		/// <param name="exApp"></param>
		/// <param name="ws"></param>
		protected virtual void PostProcessing(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{ 
		}

	}
}
