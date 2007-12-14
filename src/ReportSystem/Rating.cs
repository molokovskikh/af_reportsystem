using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Data;
using MySql.Data.MySqlClient;
using ICSharpCode.SharpZipLib.Zip;
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
     	public const string fromProperty = "FromDate";
		public const string toProperty = "ToDate";
		public const string junkProperty = "JunkState";
		public const string reportIntervalProperty = "ReportInterval";
		public const string byPreviousMonthProperty = "ByPreviousMonth";

		public int reportID;
		public int clientCode;
		public string reportCaption;
		public ArrayList allField;
		public ArrayList selectField;

		public DateTime dtFrom;
		public DateTime dtTo;
		public bool ByPreviousMonth;
		public int JunkState;
		private int _reportInterval;

		public RatingReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
		}

		public override void ReadReportParams()
		{
			JunkState = (int)getReportParam(junkProperty);
			ByPreviousMonth = (bool)getReportParam(byPreviousMonthProperty);
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

			allField = new ArrayList(9);
			selectField = new ArrayList(9);
			allField.Add(new RatingField("p.Id", "concat(cn.Name, ' ', catalogs.GetFullForm(p.Id)) as ProductName", "ProductName", "ProductName", "Наименование и форма выпуска"));
			allField.Add(new RatingField("c.Id", "concat(cn.Name, ' ', cf.Form) as CatalogName", "CatalogName", "FullName", "Наименование и форма выпуска"));
			allField.Add(new RatingField("cn.Id", "cn.Name as PosName", "PosName", "ShortName", "Наименование"));
			allField.Add(new RatingField("cfc.CodeFirmCr", "cfc.FirmCr as FirmCr", "FirmCr", "FirmCr", "Производитель"));
			allField.Add(new RatingField("rg.RegionCode", "rg.Region as RegionName", "RegionName", "Region", "Регион"));
			allField.Add(new RatingField("prov.FirmCode", "prov.ShortName as FirmShortName", "FirmShortName", "FirmCode", "Поставщик"));
			allField.Add(new RatingField("pd.PriceCode", "pd.PriceName as PriceName", "PriceName", "PriceCode", "Прайс-лист"));
			allField.Add(new RatingField("cd.FirmCode", "cd.ShortName as ClientShortName", "ClientShortName", "ClientCode", "Аптека"));

			foreach (RatingField rf in allField)
			{
				if (rf.LoadFromDB(this))
					selectField.Add(rf);
			}

			selectField.Sort(new RatingComparer());
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
  usersettings.clientsdata prov 
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
and rcs.ClientCode = oh.ClientCode
and rcs.InvisibleOnFirm = 0 
and rg.RegionCode = oh.RegionCode 
and pd.PriceCode = oh.PriceCode 
and prov.FirmCode = pd.FirmCode");

			foreach (RatingField rf in selectField)
			{
				if ((null != rf.equalValues) && (rf.equalValues.Length > 0))
					SelectCommand = String.Concat(SelectCommand, " and ", rf.GetEqualValues());
				if ((null != rf.nonEqualValues) && (rf.nonEqualValues.Length > 0))
					SelectCommand = String.Concat(SelectCommand, " and ", rf.GetNonEqualValues());
			}

			if (1 == JunkState)
				SelectCommand = String.Concat(SelectCommand, " and (ol.Junk = 0)");
			else
				if (2 == JunkState)
					SelectCommand = String.Concat(SelectCommand, " and (ol.Junk = 1)");

			SelectCommand = String.Concat(SelectCommand, String.Format(" and (oh.WriteTime > '{0}')", dtFrom.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand, String.Format(" and (oh.WriteTime < '{0}')", dtTo.ToString(MySQLDateFormat)));

			//Применяем группировку и сортировку
			List<string> GroupByList = new List<string>();
			List<string> OrderByList = new List<string>();
			foreach (RatingField rf in selectField)
				if (rf.visible)
				{
					GroupByList.Add(rf.primaryField);
					OrderByList.Add(rf.outputField);
				}
			SelectCommand = String.Concat(SelectCommand, " group by ", String.Join(",", GroupByList.ToArray()));
			SelectCommand = String.Concat(SelectCommand, " order by ", String.Join(",", OrderByList.ToArray()));
 
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
			dc.Caption = "Кол-во уникальных заказов";
			dc = res.Columns.Add("DistinctClientCode", typeof(System.Int32));
			dc.Caption = "Кол-во уникальных клиентов";

			DataRow newrow;
			try
			{
				res.BeginLoadData();
				foreach (DataRow dr in SelectTable.Rows)
				{
					newrow = res.NewRow();

					foreach (RatingField rf in selectField)
						if (rf.visible)
							newrow[rf.outputField] = dr[rf.outputField];
					newrow["Cost"] = Convert.ToDecimal(dr["Cost"]);
					newrow["PosOrder"] = Convert.ToInt32(dr["PosOrder"]);
					newrow["MinCost"] = Convert.ToDecimal(dr["MinCost"]);
					newrow["AvgCost"] = Convert.ToDecimal(dr["AvgCost"]);
					newrow["MaxCost"] = Convert.ToDecimal(dr["MaxCost"]);
					newrow["DistinctOrderId"] = Convert.ToInt32(dr["DistinctOrderId"]);
					newrow["DistinctClientCode"] = Convert.ToInt32(dr["DistinctClientCode"]);
					newrow["CostPercent"] = Decimal.Round(((decimal)newrow["Cost"] * 100) / Cost, 2);
					newrow["PosOrderPercent"] = Decimal.Round((Convert.ToDecimal(newrow["PosOrder"]) * 100) / Convert.ToDecimal(PosOrder), 2);

					res.Rows.Add(newrow);
				}
			}
			finally
			{
				res.EndLoadData();
			}

			res = res.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
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
							ws.Cells[1, i + 1] = res.Columns[i].Caption;
							((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).AutoFit();
						}

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count+1, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//Замораживаем некоторые колонки и столбцы
						((MSExcel.Range)ws.get_Range("A2", System.Reflection.Missing.Value)).Select();
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
