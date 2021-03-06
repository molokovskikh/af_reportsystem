﻿using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;

using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Configuration;
using Inforoom.ReportSystem.ByOffers;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem
{
	//Комбинированный отчет прайс-листов
	public class CombReport : BaseOffersReport
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

		public int ReportType = 1;
		public bool ShowPercents;
		//Рассчитывать отчет по каталогу (CatalogId, Name, Form), если не установлено, то расчет будет производится по продуктам (ProductId)
		public bool CalculateByCatalog;
		public bool Configured;

		protected string reportCaptionPreffix;

		protected string _clientsNames = "";
		protected string _suppliersNames = "";

		public CombReport(MySqlConnection Conn, DataSet dsProperties)
			: base(Conn, dsProperties)
		{
			reportCaptionPreffix = "Комбинированный отчет";
			DbfSupported = true;
		}

		public override void ReadReportParams()
		{
			if (Configured)
				return;
			base.ReadReportParams();

			ReportType = (int)GetReportParam("ReportType");
			ShowPercents = (bool)GetReportParam("ShowPercents");
			ClientCode = (int)GetReportParam("ClientCode");
			CalculateByCatalog = (bool)GetReportParam("CalculateByCatalog");
		}

		private void ByWeightProcessing()
		{
			DataAdapter.SelectCommand.CommandType = CommandType.Text;
			DataAdapter.SelectCommand.CommandText = "select ";
			DataAdapter.SelectCommand.CommandText += "catalog.Id as CatalogCode, ";

			DataAdapter.SelectCommand.CommandText += @"
  Core.Cost as Cost,
  concat(suppliers.Name, ' - ', regions.Region) as FirmName,
  Core.Quantity,
  Core.RegionCode,
  Core.PriceCode, ";
			if (ReportType > 2) {
				DataAdapter.SelectCommand.CommandText += "Core.ProducerId";
			}
			else {
				DataAdapter.SelectCommand.CommandText += "0";
			}
			DataAdapter.SelectCommand.CommandText += @"
As Cfc,
  0 as Junk
from
  Core,
  catalogs.Products,
  catalogs.catalog,
  catalogs.catalognames,
  catalogs.catalogforms,
  Customers.suppliers,
  farm.Regions
where
	Products.id = core.productid
and catalog.id = Products.catalogid
and catalognames.id = catalog.NameId
and catalogforms.id = catalog.FormId
and suppliers.Id = Core.PriceCode
and Regions.RegionCode = Core.RegionCode
order by CatalogCode, Cfc DESC";
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
			DataAdapter.Fill(_dsReport, "Core");

			DataAdapter.SelectCommand.CommandText = "select  ";
			DataAdapter.SelectCommand.CommandText += "catalog.Id as CatalogCode, left(catalog.Name, 250) as Name, ";

			DataAdapter.SelectCommand.CommandText += @"
  min(Core.Cost) as MinCost,
  avg(Core.Cost) as AvgCost,
  max(Core.Cost) as MaxCost, ";
			if (ReportType > 2) {
				DataAdapter.SelectCommand.CommandText += "Core.ProducerId as Cfc, left(Producers.Name, 250) as FirmCr, ";
			}
			else {
				DataAdapter.SelectCommand.CommandText += "0 As Cfc, '-' as FirmCr, ";
			}
			DataAdapter.SelectCommand.CommandText += @"
	m.Mnn
from
	(Core,
	catalogs.Products,
	catalogs.catalog)
	join Catalogs.CatalogNames cn on cn.Id = catalog.NameId
	left join Catalogs.Mnn m on m.Id = cn.MnnId";

			//Если отчет с учетом производителя, то пересекаем с таблицей Producers
			if (ReportType > 2)
				DataAdapter.SelectCommand.CommandText += @"
  left join catalogs.Producers on Producers.Id = Core.ProducerId ";

			DataAdapter.SelectCommand.CommandText += @"
where
	Products.id = core.productid
and catalog.id = Products.catalogid
";

			DataAdapter.SelectCommand.CommandText += @"
group by CatalogCode, Cfc
order by 2, 5";
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
			DataAdapter.Fill(_dsReport, "Catalog");
			DataAdapter.SelectCommand.CommandText = @"
select
 distinct Core.PriceCode, Core.RegionCode, '' as PriceDate, concat(suppliers.Name, ' - ', regions.Region) as FirmName
from
  usersettings.Core, Customers.suppliers, farm.regions
where
Core.PriceCode = suppliers.Id
and regions.RegionCode = Core.RegionCode
order by Core.Cost DESC";
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
			DataAdapter.Fill(_dsReport, "Prices");

			ProfileHelper.Next("Calculate");

			Calculate();
		}

		protected override void GenerateReport()
		{
			// Если отчет строится по взвешенным ценам, то используем другой источник данных
			// Вместо идентификатора прайса используем идентификатор поставщика
			if(_byWeightCosts) {
				ProfileHelper.Next("GetOffers");
				GetWeightCostOffers();
				ProfileHelper.Next("Processing1");
				ByWeightProcessing();
				ProfileHelper.End();
				return;
			}

			ProfileHelper.Next("Get Offers");
			GetOffers(_SupplierNoise);
			GroupActivePrices();
			ProfileHelper.Next("Processing1");
			DataAdapter.SelectCommand.CommandText = "select ";

			if (CalculateByCatalog)
				DataAdapter.SelectCommand.CommandText += "catalog.Id as CatalogCode, ";
			else
				DataAdapter.SelectCommand.CommandText += "products.Id as CatalogCode, ";

			DataAdapter.SelectCommand.CommandText += @"
  Core.Cost as Cost,
  ActivePrices.FirmName,
  FarmCore.Quantity,
  Core.RegionCode,
  Core.PriceCode, ";
			if (ReportType > 2) {
				DataAdapter.SelectCommand.CommandText += "FarmCore.codefirmcr";
			}
			else {
				DataAdapter.SelectCommand.CommandText += "0";
			}
			DataAdapter.SelectCommand.CommandText += @"
As Cfc,
  FarmCore.Junk
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
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
			DataAdapter.Fill(_dsReport, "Core");

			DataAdapter.SelectCommand.CommandText = "select  ";
			if (CalculateByCatalog)
				DataAdapter.SelectCommand.CommandText += "catalog.Id as CatalogCode, left(catalog.Name, 250) as Name, ";
			else
				DataAdapter.SelectCommand.CommandText += @"products.Id as CatalogCode, (select left(cast(concat(cn.Name, ' ', cf.Form, ' ', ifnull(group_concat(distinct pv.Value ORDER BY prop.PropertyName, pv.Value SEPARATOR ', '), '')) as CHAR), 250)
	from catalogs.Products as p
	join Catalogs.Catalog as c on p.catalogid = c.id
	JOIN Catalogs.CatalogNames cn on cn.id = c.nameid
	JOIN Catalogs.CatalogForms cf on cf.id = c.formid
	LEFT JOIN Catalogs.ProductProperties pp on pp.ProductId = p.Id
	LEFT JOIN Catalogs.PropertyValues pv on pv.id = pp.PropertyValueId
	LEFT JOIN Catalogs.Properties prop on prop.Id = pv.PropertyId
where p.id = core.productid) as Name, ";

			DataAdapter.SelectCommand.CommandText += @"
  min(Core.Cost) as MinCost,
  avg(Core.Cost) as AvgCost,
  max(Core.Cost) as MaxCost, ";
			if (ReportType > 2) {
				DataAdapter.SelectCommand.CommandText += "FarmCore.codefirmcr as Cfc, left(Producers.Name, 250) as FirmCr, ";
			}
			else {
				DataAdapter.SelectCommand.CommandText += "0 As Cfc, '-' as FirmCr, ";
			}
			DataAdapter.SelectCommand.CommandText += @"
	m.Mnn
from
	(Core,
	farm.core0 FarmCore,
	catalogs.products,
	catalogs.catalog,
	ActivePrices)
	join Catalogs.CatalogNames cn on cn.Id = catalog.NameId
	left join Catalogs.Mnn m on m.Id = cn.MnnId";

			//Если отчет с учетом производителя, то пересекаем с таблицей Producers
			if (ReportType > 2)
				DataAdapter.SelectCommand.CommandText += @"
  left join catalogs.Producers on Producers.Id = FarmCore.codefirmcr ";

			DataAdapter.SelectCommand.CommandText += @"
where
	FarmCore.id = Core.Id
and products.id = core.productid
and catalog.id = products.catalogid

and Core.pricecode = ActivePrices.pricecode
and Core.RegionCode = ActivePrices.RegionCode ";

			DataAdapter.SelectCommand.CommandText += @"
group by CatalogCode, Cfc
order by 2, 5";
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
			DataAdapter.Fill(_dsReport, "Catalog");
			DataAdapter.SelectCommand.CommandText = @"select PriceCode, RegionCode, PriceDate, FirmName from ActivePrices order by PositionCount DESC";
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
			DataAdapter.Fill(_dsReport, "Prices");

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

			var column = dtRes.Columns.Add("FullName");
			column.Caption = "Наименование";
			column.ExtendedProperties.Add("Width", 20);

			column = dtRes.Columns.Add("Mnn");
			column.Caption = "Мнн";
			column.ExtendedProperties.Add("Width", 20);

			column = dtRes.Columns.Add("FirmCr");
			column.Caption = "Производитель";
			column.ExtendedProperties.Add("Width", 10);

			column = dtRes.Columns.Add("MinCost", typeof(decimal));
			column.Caption = "Мин. цена";
			column.ExtendedProperties.Add("Width", 6);
			column.ExtendedProperties.Add("Color", System.Drawing.Color.LightSeaGreen);

			column = dtRes.Columns.Add("AvgCost", typeof(decimal));
			column.Caption = "Средняя цена";
			column.ExtendedProperties.Add("Width", 6);

			column = dtRes.Columns.Add("MaxCost", typeof(decimal));
			column.Caption = "Макс. цена";
			column.ExtendedProperties.Add("Width", 6);

			column = dtRes.Columns.Add("LeaderName");
			column.Caption = "Лидер";
			column.ExtendedProperties.Add("Width", 9);
			column.ExtendedProperties.Add("Color", System.Drawing.Color.LightSkyBlue);

			_dsReport.Tables.Add(dtRes);
			var firstColumnCount = dtRes.Columns.Count;

			var priceIndex = 0;


			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows) {
				if (Format == ReportFormats.DBF)
					dtRes.Columns.Add(drPrice["PriceCode"].ToString(), typeof(decimal));
				else
					dtRes.Columns.Add("Cost" + priceIndex, typeof(decimal));
				if (!ShowPercents) {
					if (Format == ReportFormats.DBF)
						dtRes.Columns.Add("Q" + drPrice["PriceCode"]);
					else
						dtRes.Columns.Add("Quantity" + priceIndex);
				}
				else if (Format == ReportFormats.DBF)
					dtRes.Columns.Add("P" + drPrice["PriceCode"], typeof(double));
				else
					dtRes.Columns.Add("Percents" + priceIndex, typeof(double));
				priceIndex++;
			}


			DataRow[] drsMin;
			DataRow newrow = dtRes.NewRow();
			dtRes.Rows.Add(newrow);

			foreach (DataRow drCatalog in _dsReport.Tables["Catalog"].Rows) {
				newrow = dtRes.NewRow();
				newrow["FullName"] = drCatalog["Name"];
				newrow["Mnn"] = drCatalog["Mnn"] == DBNull.Value ? "-" : drCatalog["Mnn"];
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
				foreach (var dtPos in drsMin) {
					var dr = dtPrices.Select("PriceCode=" + dtPos["PriceCode"] + " and RegionCode = " + dtPos["RegionCode"])[0];
					priceIndex = dtPrices.Rows.IndexOf(dr);

					//Если мы еще не установили значение у поставщика, то делаем это
					//раньше вставляли последнее значение, которое было максимальным
					if (newrow[firstColumnCount + priceIndex * 2] is DBNull && Convert.ToBoolean(dtPos["Junk"]) == false) {
						newrow[firstColumnCount + priceIndex * 2] = dtPos["Cost"];

						if (ReportType == 2 || ReportType == 4) {
							if (ShowPercents) {
								double mincost = Convert.ToDouble(newrow["MinCost"]), pricecost = Convert.ToDouble(dtPos["Cost"]);
								newrow[firstColumnCount + priceIndex * 2 + 1] = Math.Round(((pricecost - mincost) * 100) / pricecost, 0);
							}
						}
					}

					if (ReportType == 2 || ReportType == 4) {
						double quantity;
						double columnQuantity;
						if (!ShowPercents)
							if(newrow[firstColumnCount + priceIndex * 2 + 1] is DBNull || !double.TryParse(newrow[firstColumnCount + priceIndex * 2 + 1].ToString(), out columnQuantity))
								newrow[firstColumnCount + priceIndex * 2 + 1] = dtPos["Quantity"];
							else if(!(dtPos["Quantity"] is DBNull) && double.TryParse(dtPos["Quantity"].ToString(), out quantity))
								newrow[firstColumnCount + priceIndex * 2 + 1] = columnQuantity + quantity;
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

			ExcelHelper.Workbook(fileName, b => {
				var exApp = b.Application;
				var ws = (_Worksheet)b.Worksheets["rep" + ReportCode.ToString()];
				ws.Name = GetSheetName();

				var table = _dsReport.Tables["Results"];
				ExcelHelper.FormatHeader(ws, i + 2, table);

				var rowCount = table.Rows.Count;
				var columnCount = table.Columns.Count;
				var captionedColumnCount = table.Columns.Cast<DataColumn>().TakeWhile(c => !c.Caption.StartsWith("F")).Count();

				//Форматируем колонку "Лидер" и шапку для фирм
				FormatLeaderAndPrices(ws, captionedColumnCount + 1);

				//рисуем границы на всю таблицу
				ws.get_Range(ws.Cells[1, 1], ws.Cells[rowCount + 1, columnCount]).Borders.Weight = XlBorderWeight.xlThin;

				//Устанавливаем шрифт листа
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";
				ws.Activate();

				//Устанавливаем АвтоФильтр на все колонки
				ws.get_Range(ws.Cells[i + 2, 1], ws.Cells[rowCount, columnCount]).Select();
				((Range)exApp.Selection).AutoFilter(1, Missing.Value, XlAutoFilterOperator.xlAnd, Missing.Value, true);

				//Объединяем несколько ячеек, чтобы в них написать текст
				ws.get_Range(ws.Cells[1, 1], ws.Cells[1, captionedColumnCount]).Select();
				((Range)exApp.Selection).Merge(null);
				if(_byBaseCosts)
					reportCaptionPreffix += " по базовым ценам";
				else if(_byWeightCosts)
					reportCaptionPreffix += " по взвешенным ценам";
				if (ReportType < 3)
					exApp.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " без учета производителя создан " + DateTime.Now;
				else
					exApp.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " с учетом производителя создан " + DateTime.Now;

				// Выводим список выбранных аптек
				if (!String.IsNullOrEmpty(_clientsNames)) {
					ws.get_Range(ws.Cells[2, 1], ws.Cells[2, captionedColumnCount]).Select();
					((Range)exApp.Selection).Merge(null);

					exApp.ActiveCell.FormulaR1C1 = "Выбранные аптеки: " + _clientsNames;
				}

				// Выводим список участвовавших поставщиков
				if (!String.IsNullOrEmpty(_suppliersNames)) {
					var tmp = (i > 1) ? 3 : 2;
					ws.get_Range(
						String.Format("A{0}:K{1}", tmp, tmp + 3), Missing.Value).Select();
					((Range)exApp.Selection).Merge(null);

					exApp.ActiveCell.FormulaR1C1 = "Список поставщиков: " + _suppliersNames;
					exApp.ActiveCell.WrapText = true;
					exApp.ActiveCell.HorizontalAlignment = XlHAlign.xlHAlignLeft;
					exApp.ActiveCell.VerticalAlignment = XlVAlign.xlVAlignTop;
				}
			});
		}

		protected virtual void FormatLeaderAndPrices(_Worksheet ws, int beginColumn)
		{
			int priceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows) {
				//Устанавливаем название фирмы
				ws.Cells[1, beginColumn + priceIndex * 2] = drPrice["FirmName"].ToString();
				((Range)ws.Cells[1, beginColumn + priceIndex * 2]).ColumnWidth = 8;

				//Устанавливаем дату фирмы
				ws.Cells[1, beginColumn + priceIndex * 2 + 1] = drPrice["PriceDate"].ToString();
				((Range)ws.Cells[1, beginColumn + priceIndex * 2 + 1]).ColumnWidth = 4;

				ws.Cells[2, beginColumn + priceIndex * 2] = "Цена";
				if (!ShowPercents)
					ws.Cells[2, beginColumn + priceIndex * 2 + 1] = "Кол-во";
				else
					ws.Cells[2, beginColumn + priceIndex * 2 + 1] = "Разница в %";

				priceIndex++;
			}
		}

		private void GroupActivePrices()
		{
			DataAdapter.SelectCommand.CommandText = @"
drop temporary table if exists copy;

create temporary table copy engine=memory
select * from ActivePrices
group by priceCode;

delete from Usersettings.ActivePrices;

insert into Usersettings.ActivePrices
select * from copy;

drop temporary table copy;";
			DataAdapter.SelectCommand.ExecuteNonQuery();
		}
	}
}