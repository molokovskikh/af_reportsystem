using System;
using System.Globalization;
using Inforoom.ReportSystem.Helpers;
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

		protected int _reportType;
		protected bool _showPercents;
		//Расчитывать отчет по каталогу (CatalogId, Name, Form), если не установлено, то расчет будет производится по продуктам (ProductId)
		protected bool _calculateByCatalog;

		protected string reportCaptionPreffix;

		protected string _clientsNames = "";
		protected string _suppliersNames = "";

		public CombReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			reportCaptionPreffix = "Комбинированный отчет";
			DbfSupported = true;
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_reportType = (int)getReportParam("ReportType");
			_showPercents = (bool)getReportParam("ShowPercents");
			_clientCode = (int)getReportParam("ClientCode");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			base.GenerateReport(e);

			ProfileHelper.Next("Get Offers");
			GetOffers(_SupplierNoise);
			ProfileHelper.Next("Processing1");
			e.DataAdapter.SelectCommand.CommandText = "select " ;

			if (_calculateByCatalog)
				e.DataAdapter.SelectCommand.CommandText += "catalog.Id as CatalogCode, ";
			else
				e.DataAdapter.SelectCommand.CommandText += "products.Id as CatalogCode, ";

			e.DataAdapter.SelectCommand.CommandText += @"
  Core.Cost as Cost,
  ActivePrices.FirmName,
  FarmCore.Quantity, 
  Core.RegionCode, 
  Core.PriceCode, ";
			if (_reportType > 2)
			{
				e.DataAdapter.SelectCommand.CommandText += "FarmCore.codefirmcr";
			}
			else
			{
				e.DataAdapter.SelectCommand.CommandText += "0";
			}
			e.DataAdapter.SelectCommand.CommandText += @"
As Cfc 
from 
  Core, 
  farm.core0 FarmCore,
  catalogs.products,
  catalogs.catalog,
  catalogs.catalognames,
  catalogs.catalogforms,
  ActivePrices
where 
    FarmCore.id = Core.Id
and products.id = core.productid
and catalog.id = products.catalogid
and catalognames.id = catalog.NameId
and catalogforms.id = catalog.FormId
and Core.pricecode = ActivePrices.pricecode 
and Core.RegionCode = ActivePrices.RegionCode 
order by CatalogCode, Cfc, PositionCount DESC";
			ProfileHelper.WriteLine(e.DataAdapter.SelectCommand.CommandText);
			e.DataAdapter.Fill(_dsReport, "Core");

			e.DataAdapter.SelectCommand.CommandText = "select  ";   
			if (_calculateByCatalog)
				e.DataAdapter.SelectCommand.CommandText += "catalog.Id as CatalogCode, left(catalog.Name, 250) as Name, ";
			else
				e.DataAdapter.SelectCommand.CommandText += @"products.Id as CatalogCode, (select left(cast(concat(cn.Name, ' ', cf.Form, ' ', ifnull(group_concat(distinct pv.Value ORDER BY prop.PropertyName, pv.Value SEPARATOR ', '), '')) as CHAR), 250)
     from catalogs.Products as p
     join Catalogs.Catalog as c on p.catalogid = c.id
     JOIN Catalogs.CatalogNames cn on cn.id = c.nameid
     JOIN Catalogs.CatalogForms cf on cf.id = c.formid
     LEFT JOIN Catalogs.ProductProperties pp on pp.ProductId = p.Id
     LEFT JOIN Catalogs.PropertyValues pv on pv.id = pp.PropertyValueId
     LEFT JOIN Catalogs.Properties prop on prop.Id = pv.PropertyId
where p.id = core.productid) as Name, ";

			e.DataAdapter.SelectCommand.CommandText += @"
  min(Core.Cost) as MinCost, 
  avg(Core.Cost) as AvgCost, 
  max(Core.Cost) as MaxCost, ";
			if (_reportType > 2)
			{
				e.DataAdapter.SelectCommand.CommandText += "FarmCore.codefirmcr as Cfc, left(Producers.Name, 250) as FirmCr ";
			}
			else
			{
				e.DataAdapter.SelectCommand.CommandText += "0 As Cfc, '-' as FirmCr ";
			}
			e.DataAdapter.SelectCommand.CommandText += @"
from 
  (Core,
  farm.core0 FarmCore,
  catalogs.products,
  catalogs.catalog,

  ActivePrices)";

			//Если отчет с учетом производителя, то пересекаем с таблицой Producers
			if (_reportType > 2)
				e.DataAdapter.SelectCommand.CommandText += @"
  left join catalogs.Producers on Producers.Id = FarmCore.codefirmcr ";
 
			e.DataAdapter.SelectCommand.CommandText += @"
where 
    FarmCore.id = Core.Id
and products.id = core.productid
and catalog.id = products.catalogid

and Core.pricecode = ActivePrices.pricecode 
and Core.RegionCode = ActivePrices.RegionCode ";

			e.DataAdapter.SelectCommand.CommandText += @"
group by CatalogCode, Cfc
order by 2, 5";
			ProfileHelper.WriteLine(e.DataAdapter.SelectCommand.CommandText);
			e.DataAdapter.Fill(_dsReport, "Catalog");
			e.DataAdapter.SelectCommand.CommandText = @"select PriceCode, RegionCode, PriceDate, FirmName from ActivePrices order by PositionCount DESC";
			ProfileHelper.WriteLine(e.DataAdapter.SelectCommand.CommandText);
			e.DataAdapter.Fill(_dsReport, "Prices");

			ProfileHelper.Next("Calculate");

			Calculate();
			ProfileHelper.End();
		}

		protected virtual void Calculate()
		{
			//Кол-во первых фиксированных колонок
			var dtCore = _dsReport.Tables["Core"];
			var dtPrices = _dsReport.Tables["Prices"];

			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("FullName");
			dtRes.Columns.Add("FirmCr");
			dtRes.Columns.Add("MinCost", typeof(decimal));
			dtRes.Columns.Add("AvgCost", typeof(decimal));
			dtRes.Columns.Add("MaxCost", typeof(decimal));
			dtRes.Columns.Add("LeaderName");
			_dsReport.Tables.Add(dtRes);
			var firstColumnCount = dtRes.Columns.Count;

			var priceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				dtRes.Columns.Add("Cost" + priceIndex, typeof(decimal));
				if (!_showPercents)
					dtRes.Columns.Add("Quantity" + priceIndex);
				else
					dtRes.Columns.Add("Percents" + priceIndex, typeof(double));
				priceIndex++;
			}

			DataRow[] drsMin;
			DataRow newrow = dtRes.NewRow();
			dtRes.Rows.Add(newrow);

			foreach (DataRow drCatalog in _dsReport.Tables["Catalog"].Rows)
			{
				newrow = dtRes.NewRow();
				newrow["FullName"] = drCatalog["Name"];
				newrow["FirmCr"] = drCatalog["FirmCr"];
				newrow["MinCost"] = Convert.ToDecimal(drCatalog["MinCost"]);
				newrow["AvgCost"] = Convert.ToDecimal(drCatalog["AvgCost"]);
				newrow["MaxCost"] = Convert.ToDecimal(drCatalog["MaxCost"]);

				var producerFilter = "Cfc = " + drCatalog["Cfc"];
				if (drCatalog["Cfc"] == DBNull.Value)
					producerFilter = "cfc is null";

				drsMin = dtCore.Select(string.Format("CatalogCode = {0} and {1} and Cost = {2}",
					drCatalog["CatalogCode"],
					producerFilter, 
					((decimal)drCatalog["MinCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat)));

				if (drsMin.Length > 0)
					newrow["LeaderName"] = drsMin[0]["FirmName"];

				//Выбираем позиции и сортируем по возрастанию цен
				drsMin = dtCore.Select(String.Format("CatalogCode = {0} and {1}", drCatalog["CatalogCode"], producerFilter), "Cost asc");
				foreach (var dtPos in drsMin)
				{
					var dr = dtPrices.Select("PriceCode=" + dtPos["PriceCode"] + " and RegionCode = " + dtPos["RegionCode"])[0];
					priceIndex = dtPrices.Rows.IndexOf(dr);

					//Если мы еще не установили значение у поставщика, то делаем это
					//раньше вставляли последнее значение, которое было максимальным
					if (newrow[firstColumnCount + priceIndex * 2] is DBNull)
					{
						newrow[firstColumnCount + priceIndex * 2] = dtPos["Cost"];
						if (_reportType == 2 || _reportType == 4)
						{
							if (!_showPercents)
								newrow[firstColumnCount + priceIndex * 2 + 1] = dtPos["Quantity"];
							else
							{
								double mincost = Convert.ToDouble(newrow["MinCost"]), pricecost = Convert.ToDouble(dtPos["Cost"]);
								newrow[firstColumnCount + priceIndex * 2 + 1] = Math.Round(((pricecost - mincost) * 100) / pricecost, 0);
							}
						}
					}
				}
				dtRes.Rows.Add(newrow);
			}
		}

		protected override void FormatExcel(string fileName)
		{
			int i = 0;
			if (!String.IsNullOrEmpty(_clientsNames)) // Добавляем строку чтобы вставить выбранные аптеки
				i++;
			if (!String.IsNullOrEmpty(_suppliersNames))
				i += 4;

			ProfileHelper.Next("FormatExcel");
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				MSExcel.Workbook wb = exApp.Workbooks.Open(fileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try
				{
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

					try
					{
						ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);

						//Форматируем заголовок отчета
						ws.Cells[i+2, 1] = "Наименование";
						((MSExcel.Range)ws.Cells[i+2, 1]).ColumnWidth = 20;
						ws.Cells[i+2, 2] = "Производитель";
						((MSExcel.Range)ws.Cells[i+2, 2]).ColumnWidth = 10;
						ws.Cells[i+2, 3] = "Мин. цена";
						((MSExcel.Range)ws.Cells[i+2, 3]).ColumnWidth = 6;
						((MSExcel.Range)ws.Cells[1, 1]).Clear();
						((MSExcel.Range)ws.Cells[1, 2]).Clear();
						((MSExcel.Range)ws.Cells[1, 3]).Clear();
						
						//Форматируем колонку "Лидер" и шапку для фирм
						FormatLeaderAndPrices(ws);

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;
						//Устанавливаем цвет колонки "Мин Цена"
						ws.get_Range("C" + (i + 2), "C" + (_dsReport.Tables["Results"].Rows.Count + 1).ToString()).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSeaGreen);

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[i+2, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//Замораживаем некоторые колонки и столбцы
						if (!(this is CombShortReport))
						{
							((MSExcel.Range)ws.get_Range("G" + (3 + i), System.Reflection.Missing.Value)).Select();
							exApp.ActiveWindow.FreezePanes = true;
						}

						//Объединяем несколько ячеек, чтобы в них написать текст
						((MSExcel.Range)ws.get_Range("A1:F1", System.Reflection.Missing.Value)).Select();
						((MSExcel.Range)exApp.Selection).Merge(null);

						if (_reportType < 3)
							exApp.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " без учета производителя создан " + DateTime.Now;
						else
							exApp.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " с учетом производителя создан " + DateTime.Now;

						// Выводим список выбранных аптек
						if (!String.IsNullOrEmpty(_clientsNames))
						{
							((MSExcel.Range)ws.get_Range("A2:F2", System.Reflection.Missing.Value)).Select();
							((MSExcel.Range)exApp.Selection).Merge(null);

							exApp.ActiveCell.FormulaR1C1 = "Выбранные аптеки: " + _clientsNames;
						}

						// Выводим список участвовавших поставщиков
						if (!String.IsNullOrEmpty(_suppliersNames))
						{
							var tmp = (i > 1) ? 3 : 2;
							((MSExcel.Range)ws.get_Range(
								String.Format("A{0}:K{1}", tmp, tmp+3), System.Reflection.Missing.Value)).Select();
							((MSExcel.Range)exApp.Selection).Merge(null);

							exApp.ActiveCell.FormulaR1C1 = "Список поставщиков: " + _suppliersNames;
							exApp.ActiveCell.WrapText = true;
							exApp.ActiveCell.HorizontalAlignment = MSExcel.XlHAlign.xlHAlignLeft;
							exApp.ActiveCell.VerticalAlignment = MSExcel.XlVAlign.xlVAlignTop;
						}
					}
					finally
					{
						wb.SaveAs(fileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, MSExcel.XlSaveConflictResolution.xlLocalSessionChanges, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
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

		protected virtual void FormatLeaderAndPrices(MSExcel._Worksheet ws)
		{
			int ColumnPrefix = 7;

			ws.Cells[2, 4] = "Средняя цена";
			((MSExcel.Range)ws.Cells[2, 4]).ColumnWidth = 6;
			((MSExcel.Range)ws.Cells[1, 4]).Clear();
			ws.Cells[2, 5] = "Макс. цена";
			((MSExcel.Range)ws.Cells[2, 5]).ColumnWidth = 6;
			((MSExcel.Range)ws.Cells[1, 5]).Clear();
			ws.Cells[2, 6] = "Лидер";
			((MSExcel.Range)ws.Cells[2, 6]).ColumnWidth = 9;
			((MSExcel.Range)ws.Cells[1, 6]).Clear();

			int PriceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				//Устанавливаем название фирмы
				ws.Cells[1, ColumnPrefix + PriceIndex * 2] = drPrice["FirmName"].ToString();
				((MSExcel.Range)ws.Cells[1, ColumnPrefix + PriceIndex * 2]).ColumnWidth = 8;

				//Устанавливаем дату фирмы
				ws.Cells[1, ColumnPrefix + PriceIndex * 2 + 1] = drPrice["PriceDate"].ToString();
				((MSExcel.Range)ws.Cells[1, ColumnPrefix + PriceIndex * 2 + 1]).ColumnWidth = 4;

				ws.Cells[2, ColumnPrefix + PriceIndex * 2] = "Цена";
				if (!_showPercents)
					ws.Cells[2, ColumnPrefix + PriceIndex * 2 + 1] = "Кол-во";
				else
					ws.Cells[2, ColumnPrefix + PriceIndex * 2 + 1] = "Разница в %";

				PriceIndex++;
			}
			//Устанавливаем цвет колонки "Лидер"
			ws.get_Range("F2", "F" + (_dsReport.Tables["Results"].Rows.Count + 1).ToString()).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSkyBlue);
		}
	}
}
