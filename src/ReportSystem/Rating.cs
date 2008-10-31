using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Data;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using Inforoom.ReportSystem.RatingReports;
using ExecuteTemplate;
using System.Collections.Generic;

namespace Inforoom.ReportSystem
{
	/// <summary>
	/// Summary description for RatingReport.
	/// </summary>
	public class RatingReport : BaseReport
	{
		private const string fromProperty = "StartDate";
		private const string toProperty = "EndDate";
		private const string junkProperty = "JunkState";
		private const string reportIntervalProperty = "ReportInterval";
		private const string byPreviousMonthProperty = "ByPreviousMonth";

		protected List<RatingField> allField;
		private List<RatingField> selectField;

		private DateTime dtFrom;
		private DateTime dtTo;
		private bool ByPreviousMonth;
		private int JunkState;
		private int _reportInterval;

		//Фильтр, наложенный на рейтинговый отчет. Будет выводится на странице отчета
		protected List<string> filter;

		public RatingReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary)
			: base(ReportCode, ReportCaption, Conn, Temporary)
		{
		}

		protected void FillRatingFields()
		{
			allField = new List<RatingField>();
			allField.Add(new RatingField("p.Id", "concat(cn.Name, ' ', catalogs.GetFullForm(p.Id)) as ProductName", "ProductName", "ProductName", "Наименование и форма выпуска", "catalogs.products p, catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf", "and c.Id = p.CatalogId and cn.id = c.NameId and cf.Id = c.FormId", 0, "В отчет включены следующие продукты", "Следующие продукты исключены из отчета", 40));
			allField.Add(new RatingField("c.Id", "concat(cn.Name, ' ', cf.Form) as CatalogName", "CatalogName", "FullName", "Наименование и форма выпуска", "catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf", "and cn.id = c.NameId and cf.Id = c.FormId", 0, "В отчет включены следующие наименования", "Следующие наименования исключены из отчета", 40));
			allField.Add(new RatingField("cn.Id", "cn.Name as PosName", "PosName", "ShortName", "Наименование", "catalogs.catalognames cn", null, 0, "В отчет включены следующие наименования", "Следующие наименования исключены из отчета", 40));
			allField.Add(new RatingField("cfc.CodeFirmCr", "cfc.FirmCr as FirmCr", "FirmCr", "FirmCr", "Производитель", "farm.CatalogFirmCr cfc", null, 1, "В отчет включены следующие производители", "Следующие производители исключены из отчета", 15));
			allField.Add(new RatingField("rg.RegionCode", "rg.Region as RegionName", "RegionName", "Region", "Регион", "farm.regions rg", null, 2, "В отчет включены следующие регионы", "Следующие регионы исключены из отчета"));
			allField.Add(new RatingField("prov.FirmCode", "concat(prov.ShortName, ' - ', rg.Region) as FirmShortName", "FirmShortName", "FirmCode", "Поставщик", "usersettings.clientsdata prov, farm.regions rg", "and prov.RegionCode = rg.RegionCode", 3, "В отчет включены следующие поставщики", "Следующие поставщики исключены из отчета", 10));
			allField.Add(new RatingField("pd.PriceCode", "concat(prov.ShortName , ' (', pd.PriceName, ') - ', rg.Region) as PriceName", "PriceName", "PriceCode", "Прайс-лист", "usersettings.pricesdata pd, usersettings.clientsdata prov, farm.regions rg", "and prov.FirmCode = pd.FirmCode and prov.RegionCode = rg.RegionCode", 4, "В отчет включены следующие прайс-листы поставщиков", "Следующие прайс-листы поставщиков исключены из отчета", 10));
			allField.Add(new RatingField("cd.FirmCode", "cd.ShortName as ClientShortName", "ClientShortName", "ClientCode", "Аптека", "usersettings.clientsdata cd", null, 5, "В отчет включены следующие аптеки", "Следующие аптеки исключены из отчета", 10));
			allField.Add(new RatingField("payers.PayerId", "payers.ShortName as PayerName", "PayerName", "Payer", "Плательщик", "billing.payers", null, 6, "В отчет включены следующие плательщики", "Следующие плательщики исключены из отчета"));
		}

		public override void ReadReportParams()
		{
			FillRatingFields();
			filter = new List<string>();
			JunkState = (int)getReportParam(junkProperty);
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

			selectField = new List<RatingField>();
			foreach (RatingField rf in allField)
			{
				if (rf.LoadFromDB(this))
					selectField.Add(rf);
			}

			if (!selectField.Exists(delegate(RatingField x) { return x.visible; }))
				throw new Exception("Не выбраны поля для отображения в заголовке отчета.");

			selectField.Sort(delegate(RatingField x, RatingField y) { return (x.position - y.position); });
		}

    	public override void GenerateReport(ExecuteArgs e)
		{
			string SelectCommand = "select ";
			foreach (RatingField rf in selectField)
				if (rf.visible)
					SelectCommand = String.Concat(SelectCommand, rf.primaryField, ", ", rf.viewField, ", ");

			SelectCommand = String.Concat(SelectCommand, @"
Sum(ol.cost*ol.Quantity) as Cost, 
Sum(ol.Quantity) as PosOrder, 
Min(ol.Cost) as MinCost,
Avg(ol.Cost) as AvgCost,
Max(ol.Cost) as MaxCost,
Count(distinct oh.RowId) as DistinctOrderId,
Count(distinct oh.ClientCode) as DistinctClientCode ");
			SelectCommand = String.Concat(
				SelectCommand, @"
from 
  orders.OrdersHead oh, 
  orders.OrdersList ol,
  catalogs.products p,
  catalogs.catalog c,
  catalogs.catalognames cn,
  catalogs.catalogforms cf, 
  farm.CatalogFirmCr cfc, 
  usersettings.clientsdata cd,
  usersettings.retclientsset rcs, 
  farm.regions rg, 
  usersettings.pricesdata pd, 
  usersettings.clientsdata prov,
  billing.payers
where 
    ol.OrderID = oh.RowID 
and oh.deleted = 0
and oh.processed = 1
and p.Id = ol.ProductId
and c.Id = p.CatalogId
and cn.id = c.NameId
and cf.Id = c.FormId
and cfc.CodeFirmCr = if(ol.CodeFirmCr is not null, ol.CodeFirmCr, 1) 
and cd.FirmCode = oh.ClientCode
and cd.BillingCode <> 921
and payers.PayerId = cd.BillingCode
and rcs.ClientCode = oh.ClientCode
and rcs.InvisibleOnFirm < 2 
and rg.RegionCode = oh.RegionCode 
and pd.PriceCode = oh.PriceCode 
and prov.FirmCode = pd.FirmCode");

			foreach (RatingField rf in selectField)
			{
				if ((rf.equalValues != null) && (rf.equalValues.Count > 0))
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetEqualValues());
					filter.Add(String.Format("{0}: {1}", rf.equalValuesCaption, GetValuesFromSQL(e, rf.GetEqualValuesSQL())));
				}
				if ((rf.nonEqualValues != null) && (rf.nonEqualValues.Count > 0))
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetNonEqualValues());
					filter.Add(String.Format("{0}: {1}", rf.nonEqualValuesCaption, GetValuesFromSQL(e, rf.GetNonEqualValuesSQL())));
				}
			}

			if (1 == JunkState)
				SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and (ol.Junk = 0)");
			else
				if (2 == JunkState)
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and (ol.Junk = 1)");

			SelectCommand = String.Concat(SelectCommand, String.Format(Environment.NewLine + "and (oh.WriteTime > '{0}')", dtFrom.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand, String.Format(Environment.NewLine + "and (oh.WriteTime < '{0}')", dtTo.ToString(MySQLDateFormat)));

			//Применяем группировку и сортировку
			List<string> GroupByList = new List<string>();
			List<string> OrderByList = new List<string>();
			foreach (RatingField rf in selectField)
				if (rf.visible)
				{
					GroupByList.Add(rf.primaryField);
					OrderByList.Add(rf.outputField);
				}
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "group by ", String.Join(",", GroupByList.ToArray()));
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "order by ", String.Join(",", OrderByList.ToArray()));
 
#if DEBUG
			Debug.WriteLine(SelectCommand);
#endif

			DataTable SelectTable = new DataTable();

			e.DataAdapter.SelectCommand.CommandText = SelectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(SelectTable);

			decimal Cost = 0m;
			int PosOrder = 0;
			foreach (DataRow dr in SelectTable.Rows)
			{
				Cost += Convert.ToDecimal(dr["Cost"]);
				PosOrder += Convert.ToInt32(dr["PosOrder"]);
			}

			System.Data.DataTable res = new System.Data.DataTable();
			DataColumn dc;
			foreach (RatingField rf in selectField)
			{
				if (rf.visible)
				{
					dc = res.Columns.Add(rf.outputField, SelectTable.Columns[rf.outputField].DataType);
					dc.Caption = rf.outputCaption;
					if (rf.width.HasValue)
						dc.ExtendedProperties.Add("Width", rf.width);
				}
			}
			dc = res.Columns.Add("Cost", typeof(System.Decimal));
			dc.Caption = "Сумма";
			dc = res.Columns.Add("CostPercent", typeof(System.Double));
			dc.Caption = "Доля рынка в %";
			dc = res.Columns.Add("PosOrder", typeof(System.Int32));
			dc.Caption = "Заказ";
			dc = res.Columns.Add("PosOrderPercent", typeof(System.Double));
			dc.Caption = "Доля от общего заказа в %";
			dc = res.Columns.Add("MinCost", typeof(System.Decimal));
			dc.Caption = "Минимальная цена";
			dc = res.Columns.Add("AvgCost", typeof(System.Decimal));
			dc.Caption = "Средняя цена";
			dc = res.Columns.Add("MaxCost", typeof(System.Decimal));
			dc.Caption = "Максимальная цена";
			dc = res.Columns.Add("DistinctOrderId", typeof(System.Int32));
			dc.Caption = "Кол-во заявок по препарату";
			dc = res.Columns.Add("DistinctClientCode", typeof(System.Int32));
			dc.Caption = "Кол-во клиентов, заказавших препарат";

			DataRow newrow;
			try
			{
				int visbleCount = selectField.FindAll(delegate(RatingField x) { return x.visible; }).Count;
				res.BeginLoadData();
				foreach (DataRow dr in SelectTable.Rows)
				{
					newrow = res.NewRow();

					foreach (RatingField rf in selectField)
						if (rf.visible)
							newrow[rf.outputField] = dr[rf.outputField];

					for (int i = (visbleCount * 2); i < SelectTable.Columns.Count; i++)
					{
						if (!(dr[SelectTable.Columns[i].ColumnName] is DBNull))
							newrow[SelectTable.Columns[i].ColumnName] = Convert.ChangeType(dr[SelectTable.Columns[i].ColumnName], res.Columns[SelectTable.Columns[i].ColumnName].DataType);
					}

					newrow["CostPercent"] = Decimal.Round(((decimal)newrow["Cost"] * 100) / Cost, 2);
					newrow["PosOrderPercent"] = Decimal.Round((Convert.ToDecimal(newrow["PosOrder"]) * 100) / Convert.ToDecimal(PosOrder), 2);

					res.Rows.Add(newrow);
				}
			}
			finally
			{
				res.EndLoadData();
			}

			//Добавляем несколько пустых строк, чтобы потом вывести в них значение фильтра в Excel
			for (int i = 0; i < filter.Count; i++)
				res.Rows.InsertAt(res.NewRow(), 0);

			res = res.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
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

						//Замораживаем некоторые колонки и столбцы
						((MSExcel.Range)ws.get_Range("A" + (2 + filter.Count).ToString(), System.Reflection.Missing.Value)).Select();
						exApp.ActiveWindow.FreezePanes = true;
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

	}
}
