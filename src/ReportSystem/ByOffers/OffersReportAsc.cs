﻿using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using Common.Models;
using Common.MySql;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.ReportSystem
{
	public class OffersReportAsc : SpecReport
	{
		private int _reportType;
		private bool _calculateByCatalog;
		private bool _reportIsFull;
		private int _maxCostCount;

		private ulong _sourceRegionCode;
		private uint _sourcePriceCode;

		[Description("Минимальное количество конкурентов")]
		public int MinSupplierCount;

		public OffersReportAsc()
		{
			MinSupplierCount = 3;
		}

		public OffersReportAsc(MySqlConnection connection, DataSet dsProperties)
			: base(connection, dsProperties)
		{
			MinSupplierCount = 3;
			reportCaptionPreffix = "Отчет по минимальным ценам по возрастанию";
		}

		public override void ReadReportParams()
		{
			base.ReadBaseReportParams();
			_reportType = (int)GetReportParam("ReportType");
			if (!_byBaseCosts && !_byWeightCosts)
				ClientCode = (int)GetReportParam("ClientCode");
			_calculateByCatalog = (bool)GetReportParam("CalculateByCatalog");
			_priceCode = Convert.ToUInt32(GetReportParam("PriceCode"));
			_reportIsFull = (bool)GetReportParam("ReportIsFull");
			_maxCostCount = (int)GetReportParam("MaxCostCount");
		}

		protected override void GenerateReport()
		{
			ProfileHelper.Next("PreGetOffers");
			//Если прайс-лист равен 0, то он не установлен, поэтому берем прайс-лист относительно клиента, для которого делается отчет
			if (_priceCode == 0)
				throw new ReportException("Для специального отчета не указан параметр \"Прайс-лист\".");

			var price = Session.Load<PriceList>(_priceCode);
			SourcePriceType = price.PriceType;
			CustomerFirmName = GetSupplierName(_priceCode);

			// Если отчет строится по взвешенным ценам, то используем другой источник данных
			// Вместо идентификатора прайса используем идентификатор поставщика
			if(_byWeightCosts) {
				ProfileHelper.Next("PreGetOffers");
				SourceRegionCode = price.Supplier.HomeRegion.Id;
				SourcePC = price.Supplier.Id;

				ProfileHelper.Next("GetOffers");
				GetWeightCostOffers();

				if(!IsExistsPriceInCore(SourcePC, SourceRegionCode)) {
					ProfileHelper.Next("AdditionGetOffers");
					AddSourcePriceToWeightCore();
				}
				ProfileHelper.Next("GetCodes");
				GetWeightCostSource();
				ProfileHelper.Next("GetMinPrices");
				IsOffersReport = true;
				GetWeightMinPrice();
				ProfileHelper.Next("Calculate");
				Transform();
			}
			else {
				if (_byBaseCosts) {
					// Отчет готовится по базовым ценам
					//Заполняем код региона прайс-листа как домашний код поставщика этого прайс-листа
					_sourceRegionCode = Session.Load<PriceList>(_priceCode).Supplier.HomeRegion.Id;
				}
				else {
					// отчет готовится по клиенту
					//Заполняем код региона прайс-листа как домашний код региона клиента, относительно которого строится отчет
					_sourceRegionCode = Session.Load<Client>((uint)ClientCode).RegionCode;
				}

				_sourcePriceCode = _priceCode;

				//Проверка актуальности прайс-листа
				CheckPriceActual(_sourcePriceCode);

				GetOffers(_SupplierNoise);
				CheckSupplierCount(MinSupplierCount);

				_suppliers = GetShortSuppliers();
				_ignoredSuppliers = GetIgnoredSuppliers();
				//Получили предложения интересующего прайс-листа в отдельную таблицу
				GetSourceCodes();
				//Получили лучшие предложения из всех прайс-листов с учетом требований
				GetMinPrice();
				Transform();
			}
		}

		protected void GetSourceCodes()
		{
			var enabledPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					DataAdapter.SelectCommand.Connection,
					"select PriceCode from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
					new MySqlParameter("?SourcePC", _sourcePriceCode),
					new MySqlParameter("?SourceRegionCode", _sourceRegionCode)));

			if (enabledPrice == 0 && _byBaseCosts) {
				enabledPrice = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
						DataAdapter.SelectCommand.Connection,
						"select PriceCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
						new MySqlParameter("?SourcePC", _sourcePriceCode)));
				if (enabledPrice != 0) {
					_sourceRegionCode = Convert.ToUInt64(
						MySqlHelper.ExecuteScalar(
							DataAdapter.SelectCommand.Connection,
							"select RegionCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
							new MySqlParameter("?SourcePC", _sourcePriceCode)));
				}
			}

			//Добавляем к таблице Core поле CatalogCode и заполняем его
			DataAdapter.SelectCommand.CommandText = "alter table Core add column CatalogCode int unsigned, add key CatalogCode(CatalogCode);";
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.ExecuteNonQuery();
			if (_calculateByCatalog)
				DataAdapter.SelectCommand.CommandText = "update Core, catalogs.products set Core.CatalogCode = products.CatalogId where products.Id = Core.ProductId;";
			else
				DataAdapter.SelectCommand.CommandText = "update Core set CatalogCode = ProductId;";
			DataAdapter.SelectCommand.ExecuteNonQuery();

			DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS TmpSourceCodes;
CREATE temporary table TmpSourceCodes(
  ID int(32) unsigned,
  PriceCode int(32) unsigned,
  RegionCode int(32) unsigned,
  Code char(20),
  BaseCost decimal(8,2) unsigned,
  CatalogCode int(32) unsigned,
  CodeFirmCr int(32) unsigned,
  SynonymCode int(32) unsigned,
  SynonymFirmCrCode int(32) unsigned,
  key ID(ID),
  key CatalogCode(CatalogCode),
  key CodeFirmCr(CodeFirmCr),
  key SynonymFirmCrCode(SynonymFirmCrCode),
  key SynonymCode(SynonymCode))engine=MEMORY PACK_KEYS = 0;";

			if (enabledPrice == 0) {
				//Если прайс-лист не включен клиентом или прайс-лист ассортиментный, то добавляем его в таблицу источников TmpSourceCodes, но с ценами NULL
				DataAdapter.SelectCommand.CommandText += @"
INSERT INTO TmpSourceCodes
Select
  FarmCore.ID,
  FarmCore.PriceCode,
  ?SourceRegionCode as RegionCode,
  FarmCore.Code,
  NULL,";
				if (_calculateByCatalog)
					DataAdapter.SelectCommand.CommandText += "Products.CatalogId, ";
				else
					DataAdapter.SelectCommand.CommandText += "Products.Id, ";
				DataAdapter.SelectCommand.CommandText += @"
  FarmCore.CodeFirmCr,
  FarmCore.SynonymCode,
  FarmCore.SynonymFirmCrCode
FROM
  (
  farm.core0 FarmCore,
  catalogs.products
  )
  left join farm.corecosts cc on cc.Core_Id = FarmCore.id and cc.PC_CostCode = FarmCore.PriceCode
WHERE
	FarmCore.PriceCode = ?SourcePC
and products.id = FarmCore.ProductId;";
			}
			else {
				DataAdapter.SelectCommand.CommandText += @"
INSERT INTO TmpSourceCodes
Select
  Core.ID,
  Core.PriceCode,
  Core.RegionCode,
  FarmCore.Code,
  Core.Cost,";
				if (_calculateByCatalog)
					DataAdapter.SelectCommand.CommandText += "Products.CatalogId, ";
				else
					DataAdapter.SelectCommand.CommandText += "Products.Id, ";
				DataAdapter.SelectCommand.CommandText += @"
  FarmCore.CodeFirmCr,
  FarmCore.SynonymCode,
  FarmCore.SynonymFirmCrCode
FROM
  Core,
  farm.core0 FarmCore,
  catalogs.products
WHERE
	Core.PriceCode = ?SourcePC
and FarmCore.id = Core.Id
and products.id = Core.ProductId
and Core.RegionCode = ?SourceRegionCode;";
			}

			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", _sourcePriceCode);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", _sourceRegionCode);
			DataAdapter.SelectCommand.ExecuteNonQuery();

			DataAdapter.SelectCommand.CommandText = @"
select
  Core.Id,
  Core.CatalogCode,
  FarmCore.CodeFirmCr,
  Core.Cost,
  Core.PriceCode,
  Core.RegionCode,
  FarmCore.Quantity
from
  Core,
  farm.core0 FarmCore
where
  FarmCore.Id = core.id";

			//todo: изменить заполнение в другую таблицу
			DataAdapter.Fill(_dsReport, "AllCoreT");

			DataAdapter.SelectCommand.CommandText = @"
select
  ActivePrices.PriceCode, ActivePrices.RegionCode, ActivePrices.PriceDate, ActivePrices.FirmName
from
  ActivePrices
where
  (ActivePrices.PriceCode <> ?SourcePC or ActivePrices.RegionCode <> ?SourceRegionCode)
order by ActivePrices.PositionCount DESC";
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", _sourcePriceCode);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", _sourceRegionCode);
			DataAdapter.Fill(_dsReport, "Prices");
		}

		protected void GetMinPrice()
		{
			var sql = @"
select
  SourcePrice.ID,
  SourcePrice.Code,
  AllPrices.CatalogCode,
  AllPrices.Cost,";
			if (_reportType == 2 || _reportType == 4)
				sql += @" FarmCore.Quantity,";

			if (_calculateByCatalog)
				sql += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetCatalogProductNameSubquery("AllPrices.ProductId"));
			else
				sql += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", QueryParts.GetFullFormSubquery("AllPrices.ProductId", true));
			//Если отчет без учета производителя, то код не учитываем и выводим "-"
			if (_reportType <= 2)
				sql += @"
  '-' as FirmCr,
  0 As Cfc ";
			else
				sql += @"
  ifnull(sfc.Synonym, Cfc.Name) as FirmCr,
  cfc.Id As Cfc";

			sql += @"
from
 (
  catalogs.products,
  farm.core0 FarmCore,";

			//Если отчет полный, то интересуют все прайс-листы, если нет, то только SourcePC
			if (_reportIsFull) {
				if (_reportType <= 2)
					sql += @"
  Core AllPrices
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					sql += @"
  Core AllPrices
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=FarmCore.codefirmcr";
			}
			else
				sql += @"
  Core AllPrices,
  TmpSourceCodes SourcePrice
 )";
			//Если отчет с учетом производителя, то пересекаем с таблицей Producers
			if (_reportType > 2)
				sql += @"
  left join catalogs.Producers cfc on cfc.Id = FarmCore.codefirmcr";

			sql += @"
  left join farm.synonym s on s.SynonymCode = SourcePrice.SynonymCode
  left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = SourcePrice.SynonymFirmCrCode
where
  products.id = AllPrices.ProductId
  and FarmCore.Id = AllPrices.Id";

			sql += @"
  and (( ( (AllPrices.PriceCode <> SourcePrice.PriceCode) or (AllPrices.RegionCode <> SourcePrice.RegionCode) or (SourcePrice.id is null) ) and (FarmCore.Junk =0) and (FarmCore.Await=0) )
	  or ( (AllPrices.PriceCode = SourcePrice.PriceCode) and (AllPrices.RegionCode = SourcePrice.RegionCode) and (AllPrices.Id = SourcePrice.id) ) )";

			//Если отчет не полный, то выбираем только те, которые есть в SourcePC
			if (!_reportIsFull) {
				if (_reportType <= 2)
					sql += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					sql += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=FarmCore.codefirmcr ";
			}
			sql += @"
order by FullName, FirmCr";
			DataAdapter.SelectCommand.CommandText = sql;
			DataAdapter.Fill(_dsReport, "MinCatalog");
		}

		private void Transform()
		{
			var data = _dsReport.Tables["MinCatalog"];
			var groupedRows = data.Rows
				.Cast<DataRow>()
				.GroupBy(r => r["Code"].ToString() + "\t" + r["CatalogCode"].ToString() + "\t" + r["Cfc"].ToString());
			var result = new DataTable("Results");

			result.Columns.Add(new DataColumn("SupplierId") {
				Caption = "Код"
			});
			result.Columns.Add(new DataColumn("Product") {
				Caption = "Наименование"
			});
			result.Columns.Add(new DataColumn("Producer") {
				Caption = "Производитель"
			});
			for (var i = 1; i <= _maxCostCount; i++) {
				result.Columns.Add(new DataColumn("Cost" + i, typeof(decimal)) {
					Caption = "Цена " + i
				});
				if (_reportType == 2 || _reportType == 4)
					result.Columns.Add(new DataColumn("Quantity" + i, typeof(int)) {
						Caption = "Количество " + i
					});
			}
			foreach (var group in groupedRows) {
				var resultRow = result.NewRow();
				var first = group.OrderBy(r => Convert.ToDecimal(r["Cost"] == DBNull.Value ? decimal.MaxValue : r["Cost"]))
					.First();
				resultRow["SupplierId"] = first["Code"];
				resultRow["Product"] = first["FullName"];
				resultRow["Producer"] = first["FirmCr"];
				var index = 1;
				foreach (var row in group.OrderBy(r => Convert.ToDecimal(r["Cost"] == DBNull.Value ? decimal.MaxValue : r["Cost"]))) {
					if (index > _maxCostCount)
						break;
					if(row["Cost"] != DBNull.Value) {
						resultRow["Cost" + index] = row["Cost"];
						if (_reportType == 2 || _reportType == 4)
							resultRow["Quantity" + index] = String.IsNullOrEmpty(row["Quantity"].ToString()) ? DBNull.Value : row["Quantity"];
					}
					index++;
				}
				result.Rows.Add(resultRow);
			}
			_dsReport.Tables.Add(result);
		}

		public override bool DbfSupported
		{
			get { return true; }
		}

		protected override void FormatExcel(string fileName)
		{
			ExcelHelper.Workbook(fileName, wb => {
				var ws = (MSExcel._Worksheet)wb.Worksheets["rep" + ReportCode.ToString()];
				ws.Name = GetSheetName();
				ws.Activate();
				var res = _dsReport.Tables["Results"];
				var columnCount = _dsReport.Tables["Results"].Columns.Count;
				var rowCount = _dsReport.Tables["Results"].Rows.Count;

				//Код
				((MSExcel.Range)ws.Columns[1, Type.Missing]).AutoFit();
				//Наименование
				((MSExcel.Range)ws.Cells[3, 2]).ColumnWidth = 40;
				//Производитель
				((MSExcel.Range)ws.Cells[3, 3]).ColumnWidth = 20;

				if(_byBaseCosts)
					reportCaptionPreffix += " по базовым ценам";
				else if(_byWeightCosts)
					reportCaptionPreffix += " по взвешенным ценам по данным на " + GetStatOffersDate().ToShortDateString();
				if (_reportType < 3)
					reportCaptionPreffix += " без учета производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();
				else
					reportCaptionPreffix += " с учетом производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();

				var tableBeginRowIndex = ExcelHelper.PutHeader(ws, 1, columnCount, reportCaptionPreffix);

				if (!String.IsNullOrEmpty(_suppliers))
					tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, columnCount, String.Format("Список поставщиков: {0}", _suppliers));
				if (!String.IsNullOrEmpty(_ignoredSuppliers))
					tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, columnCount, String.Format("Игнорируемые поставщики: {0}", _ignoredSuppliers));

				for (var i = 0; i < res.Columns.Count; i++)
					ws.Cells[tableBeginRowIndex, i + 1] = res.Columns[i].Caption;

				//рисуем границы на всю таблицу
				ws.get_Range(ws.Cells[tableBeginRowIndex, 1], ws.Cells[tableBeginRowIndex + rowCount, columnCount]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;
				ws.get_Range(ws.Cells[tableBeginRowIndex, 1], ws.Cells[tableBeginRowIndex, columnCount]).Font.Bold = true;

				//Устанавливаем АвтоФильтр на все колонки
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[tableBeginRowIndex + rowCount, columnCount]].Select();
				((MSExcel.Range)wb.Application.Selection).AutoFilter(1, Missing.Value, MSExcel.XlAutoFilterOperator.xlAnd, Missing.Value, true);

				//Устанавливаем шрифт листа
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";
				ws.Activate();
			});
		}
	}
}