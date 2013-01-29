using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using Microsoft.Office.Interop.Excel;
using DataTable = System.Data.DataTable;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	public class OffersReportAsc : SpecReport
	{
		private int _reportType;
		private bool _calculateByCatalog;
		private bool _reportIsFull;
		private int _maxCostCount;

		private long _sourceRegionCode;
		private int _sourcePriceCode;
		private string _customerFirmName;

		public OffersReportAsc(ulong reportCode, string reportCaption, MySqlConnection connection, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, format, dsProperties)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам по возрастанию";
		}

		public override void ReadReportParams()
		{
			base.ReadBaseReportParams();
			_reportType = (int)getReportParam("ReportType");
			if (!_byBaseCosts && !_byWeightCosts)
				_clientCode = (int)getReportParam("ClientCode");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
			_priceCode = (int)getReportParam("PriceCode");
			_reportIsFull = (bool)getReportParam("ReportIsFull");
			_maxCostCount = (int)getReportParam("MaxCostCount");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("PreGetOffers");
			//Если прайс-лист равен 0, то он не установлен, поэтому берем прайс-лист относительно клиента, для которого делается отчет
			if (_priceCode == 0)
				throw new ReportException("Для специального отчета не указан параметр \"Прайс-лист\".");

			SourcePriceType = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
					@"
select
  pricesdata.PriceType
from
  usersettings.pricesdata
where
	pricesdata.PriceCode = ?PriceCode;",
					new MySqlParameter("?PriceCode", _priceCode)));

			SourcePriceType = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
					@"
select
	p.PriceType
from
	usersettings.pricesdata p
where
	p.PriceCode = ?PriceCode;",
					new MySqlParameter("?PriceCode", _priceCode)));

			// Если отчет строится по взвешенным ценам, то используем другой источник данных
			// Вместо идентификатора прайса используем идентификатор поставщика
			if(_byWeightCosts) {
				ProfileHelper.Next("PreGetOffers");
				SourceRegionCode = Convert.ToUInt64(
					MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
						@"select s.HomeRegion
	from usersettings.PricesData pd
	inner join Customers.suppliers s on pd.FirmCode = s.Id
	and pd.PriceCode = ?PriceCode;",
						new MySqlParameter("?PriceCode", _priceCode)));
				CustomerFirmName = GetSupplierName(_priceCode);
				SourcePC = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
						@"
select
  pricesdata.FirmCode
from
  usersettings.pricesdata
where
	pricesdata.PriceCode = ?PriceCode;",
					new MySqlParameter("?PriceCode", _priceCode)));

				ProfileHelper.Next("GetOffers");
				GetWeightCostOffers(e);
				if(!IsExistsPriceInCore(e, SourcePC, SourceRegionCode)) {
					ProfileHelper.Next("AdditionGetOffers");
					AddSourcePriceToWeightCore(e);
				}
				ProfileHelper.Next("GetCodes");
				GetWeightCostSource(e);
				ProfileHelper.Next("GetMinPrices");
				IsOffersReport = true;
				GetWeightMinPrice(e);
				ProfileHelper.Next("Calculate");
				Transform();
				return;
			}

			if (_byBaseCosts) {
				// Отчет готовится по базовым ценам
				//Заполняем код региона прайс-листа как домашний код поставщика этого прайс-листа
				_sourceRegionCode = Convert.ToInt64(
					MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
						@"select s.HomeRegion
	from usersettings.PricesData pd
	inner join Customers.suppliers s on pd.FirmCode = s.Id
	and pd.PriceCode = ?PriceCode;",
						new MySqlParameter("?PriceCode", _priceCode)));
			}
			else {
				// отчет готовится по клиенту
				//Заполняем код региона прайс-листа как домашний код региона клиента, относительно которого строится отчет
				_sourceRegionCode = Convert.ToInt64(
					MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
						@"select RegionCode
	from Customers.Clients
where Id = ?ClientCode",
						new MySqlParameter("?ClientCode", _clientCode)));
			}

			_sourcePriceCode = _priceCode;
			_customerFirmName = GetSupplierName(_priceCode);

			//Проверка актуальности прайс-листа
			var actualPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					@"
select
  pc.PriceCode
from
  usersettings.pricescosts pc,
  usersettings.priceitems pim,
  farm.formrules fr
where
	pc.PriceCode = ?SourcePC
and exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pc.PriceCode and prd.BaseCost=pc.CostCode limit 1)
and pim.Id = pc.PriceItemId
and fr.Id = pim.FormRuleId
and (to_days(now())-to_days(pim.PriceDate)) < fr.MaxOld",
					new MySqlParameter("?SourcePC", _sourcePriceCode)));
#if !DEBUG
			if (actualPrice == 0)
				throw new ReportException(String.Format("Прайс-лист {0} ({1}) не является актуальным.", _customerFirmName, _sourcePriceCode));
#endif
			CustomerFirmName = GetSupplierName(_priceCode);

			GetOffers(_SupplierNoise);

			//Получили предложения интересующего прайс-листа в отдельную таблицу
			GetSourceCodes(e);

			//Получили лучшие предложения из всех прайс-листов с учетом требований
			GetMinPrice(e);

			Transform();
		}

		protected void GetSourceCodes(ExecuteArgs e)
		{
			var enabledPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					"select PriceCode from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
					new MySqlParameter("?SourcePC", _sourcePriceCode),
					new MySqlParameter("?SourceRegionCode", _sourceRegionCode)));

			if (enabledPrice == 0 && _byBaseCosts) {
				enabledPrice = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						"select PriceCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
						new MySqlParameter("?SourcePC", _sourcePriceCode)));
				if (enabledPrice != 0) {
					_sourceRegionCode = Convert.ToInt32(
						MySqlHelper.ExecuteScalar(
							e.DataAdapter.SelectCommand.Connection,
							"select RegionCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
							new MySqlParameter("?SourcePC", _sourcePriceCode)));
				}
			}

			//Добавляем к таблице Core поле CatalogCode и заполняем его
			e.DataAdapter.SelectCommand.CommandText = "alter table Core add column CatalogCode int unsigned, add key CatalogCode(CatalogCode);";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			if (_calculateByCatalog)
				e.DataAdapter.SelectCommand.CommandText = "update Core, catalogs.products set Core.CatalogCode = products.CatalogId where products.Id = Core.ProductId;";
			else
				e.DataAdapter.SelectCommand.CommandText = "update Core set CatalogCode = ProductId;";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = @"
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
				e.DataAdapter.SelectCommand.CommandText += @"
INSERT INTO TmpSourceCodes
Select
  FarmCore.ID,
  FarmCore.PriceCode,
  ?SourceRegionCode as RegionCode,
  FarmCore.Code,
  NULL,";
				if (_calculateByCatalog)
					e.DataAdapter.SelectCommand.CommandText += "Products.CatalogId, ";
				else
					e.DataAdapter.SelectCommand.CommandText += "Products.Id, ";
				e.DataAdapter.SelectCommand.CommandText += @"
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
				e.DataAdapter.SelectCommand.CommandText += @"
INSERT INTO TmpSourceCodes
Select
  Core.ID,
  Core.PriceCode,
  Core.RegionCode,
  FarmCore.Code,
  Core.Cost,";
				if (_calculateByCatalog)
					e.DataAdapter.SelectCommand.CommandText += "Products.CatalogId, ";
				else
					e.DataAdapter.SelectCommand.CommandText += "Products.Id, ";
				e.DataAdapter.SelectCommand.CommandText += @"
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

			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", _sourcePriceCode);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", _sourceRegionCode);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = @"
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
			e.DataAdapter.Fill(_dsReport, "AllCoreT");

			e.DataAdapter.SelectCommand.CommandText = @"
select
  ActivePrices.PriceCode, ActivePrices.RegionCode, ActivePrices.PriceDate, ActivePrices.FirmName
from
  ActivePrices
where
  (ActivePrices.PriceCode <> ?SourcePC or ActivePrices.RegionCode <> ?SourceRegionCode)
order by ActivePrices.PositionCount DESC";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", _sourcePriceCode);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", _sourceRegionCode);
			e.DataAdapter.Fill(_dsReport, "Prices");
		}

		protected void GetMinPrice(ExecuteArgs e)
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
				sql += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetProductNameSubquery("AllPrices.ProductId"));
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
			//Если отчет с учетом производителя, то пересекаем с таблицой Producers
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
			e.DataAdapter.SelectCommand.CommandText = sql;
			e.DataAdapter.Fill(_dsReport, "MinCatalog");
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
			UseExcel.Workbook(fileName, wb => {
				var ws = (_Worksheet)wb.Worksheets["rep" + ReportCode.ToString()];
				var caption = ReportCaption.Substring(0, (ReportCaption.Length < MaxListName) ? ReportCaption.Length : MaxListName);
				ws.Name = caption;
				ws.Activate();
				var res = _dsReport.Tables["Results"];
				var columnCount = _dsReport.Tables["Results"].Columns.Count;
				var rowCount = _dsReport.Tables["Results"].Rows.Count;

				//Код
				((Range)ws.Columns[1, Type.Missing]).AutoFit();
				//Наименование
				((Range)ws.Cells[3, 2]).ColumnWidth = 40;
				//Производитель
				((Range)ws.Cells[3, 3]).ColumnWidth = 20;

				if(_byBaseCosts)
					reportCaptionPreffix += " по базовым ценам";
				else if(_byWeightCosts)
					reportCaptionPreffix += " по взвешенным ценам по данным на " + DateTime.Today.AddDays(-1).ToShortDateString();
				if (_reportType < 3)
					reportCaptionPreffix += " без учета производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();
				else
					reportCaptionPreffix += " с учетом производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();

				var tableBeginRowIndex = ExcelHelper.PutHeader(ws, 1, columnCount, reportCaptionPreffix);

				for (var i = 0; i < res.Columns.Count; i++)
					ws.Cells[tableBeginRowIndex, i + 1] = res.Columns[i].Caption;

				//рисуем границы на всю таблицу
				ws.get_Range(ws.Cells[tableBeginRowIndex, 1], ws.Cells[tableBeginRowIndex + rowCount, columnCount]).Borders.Weight = XlBorderWeight.xlThin;
				ws.get_Range(ws.Cells[tableBeginRowIndex, 1], ws.Cells[tableBeginRowIndex, columnCount]).Font.Bold = true;

				//Устанавливаем АвтоФильтр на все колонки
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[tableBeginRowIndex + rowCount, columnCount]].Select();
				((Range)wb.Application.Selection).AutoFilter(1, Missing.Value, XlAutoFilterOperator.xlAnd, Missing.Value, true);

				//Устанавливаем шрифт листа
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";
				ws.Activate();
			});
		}
	}
}