using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	//Комбинированный отчет прайс-листов
	public class CombReport : ProviderReport
	{
		/*
		 * ReportType 
		 *   1 - без учета производителя и без кол-ва
		 *   2 - без учета производителя и с кол-вом
		 *   3 - с учетом производителя и без кол-ва
		 *   4 - с учетом производителя и с кол-вом
		 * 
		 * ShowPercents
		 *   0 - показывать кол-во
		 *   1 - вместо кол-ва показывать проценты
		 * 
		 */

		private int _reportType;
		private int _showPercents;

		public CombReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
			_reportType = (int)_reportParams["ReportType"];
			_showPercents = (int)_reportParams["ShowPercents"];
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			GetActivePricesT(e);
			GetAllCoreT(e);

			e.DataAdapter.SelectCommand.CommandText = @"
select 
  catalog.FullCode as FullCode, 
  left(concat(Catalog.Name, ' ', Catalog.Form), 250) as Name, 
  AllCoreT.Cost as Cost,
  ActivePricesT.FirmName,
  -- round(if(basecost*ActivePricesT.UpCost<minboundcost, minboundcost, basecost*ActivePricesT.UpCost*exchange), 2) as Cost, 
  AllCoreT.Quantity, 
  AllCoreT.RegionCode, 
  AllCoreT.PriceCode, 
  left(farm.CatalogFirmCr.FirmCr, 250) as FirmCr, ";
			if (_reportType > 2)
			{
				e.DataAdapter.SelectCommand.CommandText += "AllCoreT.codefirmcr";
			}
			else
			{
				e.DataAdapter.SelectCommand.CommandText += "0";
			}
			e.DataAdapter.SelectCommand.CommandText += @"
As Cfc 
from 
  AllCoreT, 
  farm.catalog, 
  ActivePricesT, 
  farm.CatalogFirmCr 
where 
    catalog.fullcode = AllCoreT.fullcode 
and AllCoreT.pricecode = ActivePricesT.pricecode 
and AllCoreT.RegionCode = ActivePricesT.RegionCode 
and catalogfirmcr.codefirmcr = AllCoreT.codefirmcr 
order by catalog.FullCode, Cfc, PosCount DESC";
			e.DataAdapter.Fill(_dsReport, "Core");

			e.DataAdapter.SelectCommand.CommandText = @"
select   
  catalog.FullCode as FullCode, 
  left(concat(Catalog.Name, ' ', Catalog.Form), 250) as Name, 
  min(AllCoreT.Cost) as MinCost, ";
			if (_reportType > 2)
			{
				e.DataAdapter.SelectCommand.CommandText += "AllCoreT.codefirmcr as Cfc, left(farm.CatalogFirmCr.FirmCr, 250) as FirmCr ";
			}
			else
			{
				e.DataAdapter.SelectCommand.CommandText += "0 As Cfc, '-' as FirmCr ";
			}
			e.DataAdapter.SelectCommand.CommandText += @"
from 
  AllCoreT, 
  farm.catalog, 
  ActivePricesT, 
  farm.CatalogFirmCr 
where 
    catalog.fullcode = AllCoreT.fullcode 
and AllCoreT.pricecode = ActivePricesT.pricecode 
and AllCoreT.RegionCode = ActivePricesT.RegionCode 
and catalogfirmcr.codefirmcr = AllCoreT.codefirmcr 
group by catalog.FullCode, Cfc
order by catalog.FullCode, Cfc";
			e.DataAdapter.Fill(_dsReport, "Catalog");

			e.DataAdapter.SelectCommand.CommandText = @"select PriceCode, RegionCode, DateCurPrice, FirmName from ActivePricesT order by PosCount DESC";
			e.DataAdapter.Fill(_dsReport, "Prices");

			Calculate();
		}

		public override void ReportToFile(string FileName)
		{
			DataTableToExcel(_dsReport.Tables["Results"], FileName);
			FormatExcel(FileName);
		}

		protected void Calculate()
		{
			DataTable dtCore = _dsReport.Tables["Core"];
			DataTable dtPrices = _dsReport.Tables["Prices"];

			DataTable dtRes = new DataTable("Results");
			_dsReport.Tables.Add(dtRes);
			dtRes.Columns.Add("FullName");
			dtRes.Columns.Add("FirmCr");
			dtRes.Columns.Add("MinCost", typeof(decimal));
			dtRes.Columns.Add("LeaderName");
			int PriceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				dtRes.Columns.Add("Cost" + PriceIndex.ToString(), typeof(decimal));
				if (_showPercents == 0)
					dtRes.Columns.Add("Quantity" + PriceIndex.ToString());
				else
					dtRes.Columns.Add("Percents" + PriceIndex.ToString(), typeof(decimal));
				PriceIndex++;
			}

			DataRow newrow;
			DataRow[] drsMin;
			newrow = dtRes.NewRow();
			dtRes.Rows.Add(newrow);

			foreach (DataRow drCatalog in _dsReport.Tables["Catalog"].Rows)
			{
				newrow = dtRes.NewRow();
				newrow["FullName"] = drCatalog["Name"];
				newrow["FirmCr"] = drCatalog["FirmCr"];
				newrow["MinCost"] = Convert.ToDecimal(drCatalog["MinCost"]);

				drsMin = dtCore.Select(
					"FullCode = " + drCatalog["FullCode"].ToString() + 
					" and Cfc = " + drCatalog["Cfc"].ToString() + 
					" and Cost = " + ((decimal)drCatalog["MinCost"]).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
				if (drsMin.Length > 0)
					newrow["LeaderName"] = drsMin[0]["FirmName"];

				drsMin = dtCore.Select("FullCode = " + drCatalog["FullCode"].ToString() + "and Cfc = " + drCatalog["Cfc"].ToString());
				foreach (DataRow dtPos in drsMin)
				{
					DataRow dr = dtPrices.Select("PriceCode=" + dtPos["PriceCode"].ToString() + " and RegionCode = " + dtPos["RegionCode"].ToString())[0];
					PriceIndex = dtPrices.Rows.IndexOf(dr);
					newrow[4 + PriceIndex * 2] = dtPos["Cost"];
					if ((_reportType == 2) || (_reportType == 4))
					{
						if (_showPercents == 0)
							newrow[4 + PriceIndex * 2 + 1] = dtPos["Quantity"];
						else
						{
							decimal mincost = (decimal)newrow["MinCost"], pricecost = Convert.ToDecimal(dtPos["Cost"]);
							newrow[4 + PriceIndex * 2 + 1] = ((pricecost - mincost) * 100) / pricecost;
						}
					}
				}

				dtRes.Rows.Add(newrow);
			}
		}

		protected void FormatExcel(string FileName)
		{
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try
			{
				MSExcel.Workbook wb = exApp.Workbooks.Open(FileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try
				{
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

					try
					{
						ws.Name = _reportCaption;

						ws.Cells[2, 1] = "Наименование";
						((MSExcel.Range)ws.Cells[2, 1]).ColumnWidth = 20;
						ws.Cells[2, 2] = "Производитель";
						((MSExcel.Range)ws.Cells[2, 2]).ColumnWidth = 10;
						ws.Cells[2, 3] = "Мин. Цена";
						((MSExcel.Range)ws.Cells[2, 3]).ColumnWidth = 6;
						ws.Cells[2, 4] = "Лидер";
						((MSExcel.Range)ws.Cells[2, 4]).ColumnWidth = 9;
						((MSExcel.Range)ws.Cells[1, 1]).Clear();
						((MSExcel.Range)ws.Cells[1, 2]).Clear();
						((MSExcel.Range)ws.Cells[1, 3]).Clear();
						((MSExcel.Range)ws.Cells[1, 4]).Clear();

						int PriceIndex = 0;
						foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
						{
							//Устанавливаем название фирмы
							ws.Cells[1, 5 + PriceIndex * 2] = drPrice["FirmName"].ToString();
							((MSExcel.Range)ws.Cells[1, 5 + PriceIndex * 2]).ColumnWidth = 8;

							//Устанавливаем дату фирмы
							ws.Cells[1, 5 + PriceIndex * 2 + 1] = drPrice["DateCurPrice"].ToString();
							((MSExcel.Range)ws.Cells[1, 5 + PriceIndex * 2 + 1]).ColumnWidth = 4;

							ws.Cells[2, 5 + PriceIndex * 2] = "Цена";
							if (_showPercents == 0)
								ws.Cells[2, 5 + PriceIndex * 2 + 1] = "Кол-во";
							else
								ws.Cells[2, 5 + PriceIndex * 2 + 1] = "Разница в %";

							PriceIndex++;
						}

						ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;
						ws.get_Range("C2", "C" + (_dsReport.Tables["Results"].Rows.Count + 1).ToString()).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSeaGreen);
						ws.get_Range("D2", "D" + (_dsReport.Tables["Results"].Rows.Count + 1).ToString()).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSkyBlue);
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();
						((MSExcel.Range)ws.get_Range(ws.Cells[2, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count - 1, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);
						((MSExcel.Range)ws.get_Range("E3", System.Reflection.Missing.Value)).Select();
						exApp.ActiveWindow.FreezePanes = true;
						((MSExcel.Range)ws.get_Range("A1:D1", System.Reflection.Missing.Value)).Select();
						((MSExcel.Range)exApp.Selection).Merge(null);
						if (_reportType < 3)
							exApp.ActiveCell.FormulaR1C1 = "Комбинированный отчет без учета производителя создан " + DateTime.Now.ToString();
						else
							exApp.ActiveCell.FormulaR1C1 = "Комбинированный отчет с учетом производителя создан " + DateTime.Now.ToString();
					}
					finally
					{
						wb.Save();
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
