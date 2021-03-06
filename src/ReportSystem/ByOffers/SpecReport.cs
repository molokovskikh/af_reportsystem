﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Common.MySql;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using System.Data;
using Common.Models;
using Inforoom.ReportSystem.ByOffers;
using DataTable = System.Data.DataTable;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.ReportSystem
{
	//Специальный отчет прайс-листов
	public class SpecReport : BaseOffersReport
	{
		//1 - без учета производителя и с количеством
		//2 - без учета производителя и количеством
		//3 - с учетом производителя и без количеством
		//4 - с учетом производителя и с количеством
		protected int _reportType;
		protected bool _showPercents;
		protected bool _reportIsFull;
		protected bool _reportSortedByPrice;
		//Рассчитывать отчет по каталогу (CatalogId, Name, Form), если не установлено, то расчет будет производится по продуктам (ProductId)
		protected bool _calculateByCatalog;

		protected uint SourcePC, FirmCode;
		protected ulong SourceRegionCode;
		protected uint _priceCode;
		protected string CustomerFirmName;

		protected string reportCaptionPreffix;

		protected string _suppliers;
		protected string _ignoredSuppliers;
		protected string _suppliers2;

		protected string _clientsNames = "";

		protected bool WithoutAssortmentPrice;

		protected bool _showCodeCr;

		protected bool _codesWithoutProducer;
		//количество столбцов до начала блоков прайс листов
		protected int firstColumnCount;
		//количество столбцов в блоке прайс листа
		private int priceBlockSize;

		protected int SourcePriceType;

		protected bool IsOffersReport = false;

		protected bool HideAllExcept4 = false;

		protected SpecReport() // конструктор для возможности тестирования
		{
		}

		public SpecReport(MySqlConnection connection, DataSet dsProperties)
			: base(connection, dsProperties)
		{
			reportCaptionPreffix = "Специальный отчет";
		}
		/// <summary>
		/// результаты отчета для тестов
		/// </summary>
		public DataSet DSResult => _dsReport;

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_reportType = (int)GetReportParam("ReportType");
			_showPercents = (bool)GetReportParam("ShowPercents");
			_reportIsFull = (bool)GetReportParam("ReportIsFull");
			_reportSortedByPrice = (bool)GetReportParam("ReportSortedByPrice");
			if (!_byBaseCosts)
				ClientCode = (int)GetReportParam("ClientCode");
			_calculateByCatalog = (bool)GetReportParam("CalculateByCatalog");
			_priceCode = Convert.ToUInt32(GetReportParam("PriceCode"));
			_selfPrice = (int)_priceCode;
			if (_reportParams.ContainsKey("HideAllExcept4"))
				HideAllExcept4 = (bool)GetReportParam("HideAllExcept4");
		}

		protected void ReadBaseReportParams()
		{
			base.ReadReportParams();
		}

		public string GetShortSuppliers()
		{
			var suppliers = new List<string>();

			DataAdapter.SelectCommand.CommandText = @"
select
	concat(supps.Name, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from
	usersettings.ActivePrices p
	join usersettings.PricesData pd on pd.PriceCode = p.PriceCode
	join Customers.suppliers supps on supps.Id = pd.FirmCode
group by supps.Id
order by supps.Name";
			using (var reader = DataAdapter.SelectCommand.ExecuteReader()) {
				while (reader.Read())
					suppliers.Add(Convert.ToString(reader[0]));
			}
			return suppliers.Distinct().Implode();
		}

		public string GetSuppliersName(List<ulong> ids)
		{
			if (ids == null || !ids.Any())
				return null;

			var suppliers = new List<string>();

			DataAdapter.SelectCommand.CommandText = string.Format(@"
select concat(s.Name, ' - ', rg.Region) as SupplierName
from customers.suppliers s
left join farm.regions rg on rg.RegionCode = s.HomeRegion
where s.Id in ({0})", ids.Implode());
			using (var reader = DataAdapter.SelectCommand.ExecuteReader())
			{
				while (reader.Read())
					suppliers.Add(Convert.ToString(reader[0]));
			}
			return suppliers.Distinct().Implode();
		}

		protected void GetWeightMinPrice()
		{
			string SqlCommandText = @"
select
  SourcePrice.ID,
  SourcePrice.Code,
  ifnull(AllPrices.CatalogCode, SourcePrice.CatalogCode) as CatalogCode,
(
	select catalog.Name
	from catalogs.catalog
	where Catalog.Id = AllPrices.ProductId
) as FullName,
";
			if (IsOffersReport) {
				SqlCommandText += @"
AllPrices.Cost,
AllPrices.Quantity,";
			}
			else {
				SqlCommandText += @"
  min(AllPrices.cost) As MinCost, -- здесь должна быть минимальная цена
  avg(AllPrices.cost) As AvgCost, -- здесь должна быть средняя цена
  max(AllPrices.cost) As MaxCost, -- здесь должна быть минимальная цена";
			}

			//Если отчет без учета производителя, то код не учитываем и выводим "-"
			if (_reportType <= 2)
				SqlCommandText += @"
  '-' as FirmCr,
  0 As Cfc ";
			else
				SqlCommandText += @"
  Cfc.Name as FirmCr,
  cfc.Id As Cfc ";

			SqlCommandText += @"
from
 (

  Core AllPrices
";


			//Если отчет полный, то интересуют все прайс-листы, если нет, то только SourcePC
			if (_reportIsFull) {
				if (_reportType <= 2)
					SqlCommandText += @"
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"

 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=AllPrices.ProducerId";
			}
			else
				SqlCommandText += @",
  TmpSourceCodes SourcePrice
 )";
			//Если отчет с учетом производителя, то пересекаем с таблицей Producers
			if (_reportType > 2)
				SqlCommandText += @"
  left join catalogs.Producers cfc on cfc.Id = AllPrices.ProducerId";

			SqlCommandText += @"
where
";

			SqlCommandText += @"
 (( ( (AllPrices.PriceCode <> SourcePrice.PriceCode) or (AllPrices.RegionCode <> SourcePrice.RegionCode) or (SourcePrice.id is null) ))
	  or ( (AllPrices.PriceCode = SourcePrice.PriceCode) and (AllPrices.RegionCode = SourcePrice.RegionCode) and (AllPrices.Id = SourcePrice.id) ) )";

			//Если отчет не полный, то выбираем только те, которые есть в SourcePC
			if (!_reportIsFull) {
				if (_reportType <= 2)
					SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode and (SourcePrice.codefirmcr=AllPrices.ProducerId or
(SourcePrice.codefirmcr is null and AllPrices.ProducerId is not null)) and SourcePrice.CatalogCode=AllPrices.ProductId";
			}
			if (!IsOffersReport)
				SqlCommandText += @"
group by AllPrices.CatalogCode, Cfc";
			SqlCommandText += @"
order by FullName, FirmCr";
			DataAdapter.SelectCommand.CommandText = SqlCommandText;
			DataAdapter.Fill(_dsReport, "MinCatalog");

#if DEBUG
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
#endif
		}

		protected void GetWeightCatalog()
		{
			string SqlCommandText = @"
select
  SourcePrice.ID,
  SourcePrice.Code,
  ifnull(AllPrices.CatalogCode, SourcePrice.CatalogCode) as CatalogCode,
  c0.Id as CoreCode, ";
			SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", QueryParts.GetFullFormSubquery("c0.ProductId", true));

			var coreJoin = "";
			//Если отчет без учета производителя, то код не учитываем и выводим "-"
			if (_reportType <= 2) {
				SqlCommandText += @"
  '-' as FirmCr,
  0 As Cfc ";
			} else {
				SqlCommandText += @"
  Cfc.Name as FirmCr,
  cfc.Id As Cfc ";
				coreJoin = "and c0.CodeFirmCr <=> AllPrices.ProducerId";
			}

			SqlCommandText += $@"
from Core AllPrices
join catalogs.products pr on pr.id = AllPrices.ProductId
join catalogs.catalog ctl on ctl.id = pr.CatalogId
right join farm.Core0 c0 on c0.productid = pr.id and c0.pricecode = ?SourcePrice {coreJoin}
left join farm.synonym s on s.SynonymCode = c0.SynonymCode
	left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = c0.SynonymFirmCrCode
";
			if (_reportIsFull) {
				if (_reportType <= 2)
					SqlCommandText += @"
left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=AllPrices.ProducerId";
			} else {
				if (_reportType <= 2)
					SqlCommandText += @"
join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode
	and (SourcePrice.codefirmcr=AllPrices.ProducerId or (SourcePrice.codefirmcr is null and AllPrices.ProducerId is not null))
	and SourcePrice.CatalogCode=AllPrices.ProductId";
			}

			//Если отчет с учетом производителя, то пересекаем с таблицей Producers
			if (_reportType > 2)
				SqlCommandText += @"
left join catalogs.Producers cfc on cfc.Id = AllPrices.ProducerId";

			SqlCommandText += @"
where (( ( (AllPrices.PriceCode <> SourcePrice.PriceCode) or (AllPrices.RegionCode <> SourcePrice.RegionCode) or (SourcePrice.id is null) ))
	  or ( (AllPrices.PriceCode = SourcePrice.PriceCode) and (AllPrices.RegionCode = SourcePrice.RegionCode) and (AllPrices.Id = SourcePrice.id) ) )";

			if (!IsOffersReport)
				SqlCommandText += @"
group by FullName, AllPrices.CatalogCode, Cfc";
			if ((!_reportIsFull) && (_reportSortedByPrice))
				SqlCommandText += @"
order by CoreCode";
			else
				SqlCommandText += @"
order by FullName, FirmCr";
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePrice", _priceCode);
			DataAdapter.SelectCommand.CommandText = SqlCommandText;
			DataAdapter.Fill(_dsReport, "Catalog");

#if DEBUG
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
#endif
		}

		public void GetWeightCostSource()
		{
			//Добавляем к таблице Core поле CatalogCode и заполняем его
			DataAdapter.SelectCommand.CommandText = "alter table Core add column CatalogCode int unsigned, add key CatalogCode(CatalogCode);";
			DataAdapter.SelectCommand.CommandType = CommandType.Text;
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);

			DataAdapter.SelectCommand.CommandText = "update Core set CatalogCode = ProductId;";
			DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);

			DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS TmpSourceCodes;
CREATE temporary table TmpSourceCodes(
  ID bigint unsigned,
  PriceCode int(32) unsigned,
  RegionCode bigint unsigned,
  Code char(20),
  BaseCost decimal(8,2) unsigned,
  CatalogCode int(32) unsigned,
  CodeFirmCr int(32) unsigned,
  key ID(ID),
  key CatalogCode(CatalogCode),
  key CodeFirmCr(CodeFirmCr)
) engine = MEMORY PACK_KEYS = 0;";

			DataAdapter.SelectCommand.CommandText += @"
INSERT INTO TmpSourceCodes
Select
  Core.ID,
  Core.PriceCode,
  Core.RegionCode,
  (SELECT GROUP_CONCAT(distinct code SEPARATOR ', ') FROM farm.core0 fc join catalogs.products cp on fc.ProductId=cp.Id
where PriceCode=?SourcePrice and cp.CatalogId = Core.ProductId) as Code,
  Core.Cost,";

			DataAdapter.SelectCommand.CommandText += "Core.ProductId, ";
			DataAdapter.SelectCommand.CommandText += @"
Core.ProducerId
FROM
  Core
WHERE
Core.PriceCode = ?SourcePC
and Core.RegionCode = ?SourceRegionCode;";

			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePrice", _priceCode);
			DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
#if DEBUG
			DataAdapter.SelectCommand.CommandText = @"select * from TmpSourceCodes";
			DataAdapter.Fill(_dsReport, "TmpSourceCodes");
#endif
			DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS CoreCopy;
create temporary table CoreCopy engine memory
select * from core;

select
  Core.Id,
  Core.CatalogCode,
  Core.ProducerId as CodeFirmCr,
  Core.Cost,
  Core.PriceCode,
  Core.RegionCode,
  Core.Quantity,
  0 as Junk,
  '' as Code
from
  Core;";

			DataAdapter.Fill(_dsReport, "AllCoreT");

			DataAdapter.SelectCommand.CommandText = @"
select
 distinct Core.PriceCode, Core.RegionCode, '' as PriceDate, concat(suppliers.Name, ' - ', regions.Region) as FirmName, st.Position
from
  (usersettings.Core, Customers.suppliers, farm.regions)
left join (select pd.firmcode, SUM(pi.RowCount) as Position
FROM
    usersettings.PricesData pd
    JOIN usersettings.PricesCosts pc on pc.PriceCode = pd.PriceCode and exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pd.PriceCode and prd.BaseCost=pc.CostCode limit 1)
    JOIN usersettings.PriceItems pi on pi.Id = pc.PriceItemId
WHERE exists (select * from usersettings.PricesRegionalData prd, usersettings.TmpRegions TPR
    where prd.pricecode = pd.pricecode AND prd.RegionCode = TPR.RegionCode AND prd.enabled = 1)
    AND pd.agencyenabled = 1
    AND pd.enabled = 1
    AND pd.pricetype <> 1
group by pd.firmcode) st on suppliers.id = st.firmcode
where
Core.PriceCode = suppliers.Id
and (Core.PriceCode <> ?SourcePC or Core.RegionCode <> ?SourceRegionCode)
and regions.RegionCode = Core.RegionCode
order by st.Position DESC";


			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
			DataAdapter.Fill(_dsReport, "Prices");
		}

		public void AddSourcePriceToWeightCore()
		{
			DataAdapter.SelectCommand.CommandType = CommandType.Text;

			DataAdapter.SelectCommand.CommandText = @"alter table Core add column CoreNew int unsigned DEFAULT 0;";
			DataAdapter.SelectCommand.CommandType = CommandType.Text;
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);

			DataAdapter.SelectCommand.CommandText = @"
set @cnt= (select max(Id) from usersettings.Core);
insert into usersettings.Core
select distinct ?SourcePC, ?SourceRegionCode, c.ProductId,
if(if(round(cc.Cost * round((1 + pd.UpCost / 100) * (1 + ifnull(prd.UpCost, 0) / 100), 5), 2) < MinBoundCost,
MinBoundCost, round(cc.Cost * round((1 + pd.UpCost / 100) * (1 + ifnull(prd.UpCost, 0) / 100), 5), 2)) > MaxBoundCost,
	MaxBoundCost, if(round(cc.Cost*round((1 + pd.UpCost / 100) * (1 + ifnull(prd.UpCost, 0) / 100), 5),2) < MinBoundCost,
MinBoundCost, round(cc.Cost * round((1 + pd.UpCost / 100) * (1 + ifnull(prd.UpCost, 0) / 100), 5), 2))),
'',
@cnt:=@cnt+1,
c.Quantity,
c.CodeFirmCr,
1
from
farm.core0 c
join usersettings.PricesData pd on c.PriceCode = pd.PriceCode
join usersettings.PricesCosts pc on pd.PriceCode = pc.PriceCode and exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pd.PriceCode and prd.BaseCost=pc.CostCode limit 1)
left JOIN farm.CoreCosts cc on cc.Core_Id = c.Id and cc.PC_CostCode = pc.CostCode
left JOIN usersettings.PricesRegionalData prd ON prd.pricecode = pd.pricecode AND prd.RegionCode = ?SourceRegionCode
where
c.PriceCode = ?SourcePrice;";

			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePrice", _priceCode);
			DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
		}

		public bool IsExistsPriceInCore(uint priceCode, ulong region)
		{
			return IsExistsPriceInCore((int)priceCode, region);
		}

		public bool IsExistsPriceInCore(int priceCode, ulong region)
		{
			DataAdapter.SelectCommand.CommandType = CommandType.Text;
			DataAdapter.SelectCommand.CommandText = @"
select count(*) from usersettings.Core
where regionCode = ?region and PriceCode = ?price;";
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?region", region);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?price", priceCode);
			var count = DataAdapter.SelectCommand.ExecuteScalar();
			return int.Parse(count.ToString()) > 0;
		}

		protected override void GenerateReport()
		{
			//Если прайс-лист равен 0, то он не установлен, поэтому берем прайс-лист относительно клиента, для которого делается отчет
			if (_priceCode == 0)
				throw new ReportException("Для специального отчета не указан параметр \"Прайс-лист\".");

			CustomerFirmName = GetSupplierName(_priceCode);
			var price = Session.Load<PriceList>(_priceCode);
			SourcePC = price.Supplier.Id;

			CheckPriceActual(_priceCode);
			SourcePriceType = price.PriceType;

			// Если отчет строится по взвешенным ценам, то используем другой источник данных
			// Вместо идентификатора прайса используем идентификатор поставщика
			if(_byWeightCosts) {
				ProfileHelper.Next("PreGetOffers");
				SourceRegionCode = price.Supplier.HomeRegion.Id;

				ProfileHelper.Next("GetOffers");
				GetWeightCostOffers();
				if(SourcePriceType == (int)PriceType.Assortment || !IsExistsPriceInCore(SourcePC, SourceRegionCode)) {
					ProfileHelper.Next("AdditionGetOffers");
					AddSourcePriceToWeightCore();
					SourcePriceType = (int)PriceType.Assortment;
				}
				ProfileHelper.Next("GetCodes");
				GetWeightCostSource();
				ProfileHelper.Next("GetMinPrices");
				GetWeightMinPrice();
				ProfileHelper.Next("GetCatalog");
				GetWeightCatalog();
				ProfileHelper.Next("Calculate");
				Calculate();
				return;
			}

			ProfileHelper.Next("PreGetOffers");
			if (_byBaseCosts) {
				// Отчет готовится по базовым ценам
				//Заполняем код региона прайс-листа как домашний код поставщика этого прайс-листа
				SourceRegionCode = price.Supplier.HomeRegion.Id;
			}
			else {
				// отчет готовится по клиенту
				//Заполняем код региона прайс-листа как домашний код региона клиента, относительно которого строится отчет
				SourceRegionCode = Convert.ToUInt64(
					MySqlHelper.ExecuteScalar(Connection,
						@"select RegionCode
	from Customers.Clients
where Id = ?ClientCode",
						new MySqlParameter("?ClientCode", ClientCode)));
			}

			SourcePC = _priceCode;

			ProfileHelper.Next("GetOffers");
			//Выбираем
			GetOffers(_SupplierNoise);
			if(_byBaseCosts && !IsExistsPriceInCore(_priceCode, SourceRegionCode)) {
				ProfileHelper.Next("AdditionGetOffers");
				AddSourcePriceToCore();
			}
			ProfileHelper.Next("GetCodes");
			//Получили предложения интересующего прайс-листа в отдельную таблицу
			GetSourceCodes();
			ProfileHelper.Next("GetMinPrices");
			//Получили лучшие предложения из всех прайс-листов с учетом требований
			GetMinPrice();
			// Получили список позиций для вывода в отчет
			GetCatalog();
			ProfileHelper.Next("Calculate");
			Calculate();
			ProfileHelper.End();

			DoCoreCheck();
		}

		private void AddSourcePriceToCore()
		{
			DataAdapter.SelectCommand.CommandText = @"
INSERT
INTO	Usersettings.Core
SELECT distinct
	straight_join
	?SourcePrice,
	?SourceRegionCode,
	c.ProductId,
	if(if(round(cc.Cost * round((1 + pd.UpCost / 100) * (1 + ifnull(prd.UpCost, 0) / 100), 5), 2) < MinBoundCost,
MinBoundCost, round(cc.Cost * round((1 + pd.UpCost / 100) * (1 + ifnull(prd.UpCost, 0) / 100), 5), 2)) > MaxBoundCost,
	MaxBoundCost, if(round(cc.Cost*round((1 + pd.UpCost / 100) * (1 + ifnull(prd.UpCost, 0) / 100), 5),2) < MinBoundCost,
MinBoundCost, round(cc.Cost * round((1 + pd.UpCost / 100) * (1 + ifnull(prd.UpCost, 0) / 100), 5), 2))),
	'',
	c.id
FROM farm.core0 c
join usersettings.PricesData pd on c.PriceCode = pd.PriceCode
join usersettings.PricesCosts pc on pd.PriceCode = pc.PriceCode and exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pd.PriceCode and prd.BaseCost=pc.CostCode limit 1)
left JOIN usersettings.PricesRegionalData prd ON prd.pricecode = pd.pricecode AND prd.RegionCode = ?SourceRegionCode
left JOIN farm.CoreCosts cc on cc.Core_Id = c.Id and cc.PC_CostCode = pc.CostCode
where
	c.PriceCode = ?SourcePrice;

Delete from Usersettings.Core where Cost < 0.01;";

			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePrice", _priceCode);
			DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		private void DoCoreCheck()
		{
			DataAdapter.SelectCommand.CommandText = @"
select c.PriceCode
from Usersettings.Core c
left join farm.core0 c0 on c.Id = c0.Id
where c0.Id is null
group by c.pricecode";
			var data = new DataTable();
			DataAdapter.Fill(data);
			if (data.Rows.Count > 0) {
				Logger.DebugFormat("Отчет {1}, Прайс листы {0} обновились для них не будет предложений",
					data.Rows.Cast<DataRow>().Select(r => Convert.ToUInt32(r["PriceCode"])).Implode(),
					ReportCode);
			}
		}

		protected virtual void Calculate()
		{
			//todo: посмотреть почему здесь используется таблицы AllCoreT и Prices
			var dtCore = _dsReport.Tables["AllCoreT"];
			var dtPrices = _dsReport.Tables["Prices"];

			var dtRes = new DataTable("Results");
			_dsReport.Tables.Add(dtRes);

			var column = dtRes.Columns.Add("Code");
			column.Caption = "Код";

			column = dtRes.Columns.Add("FullName");
			column.ExtendedProperties.Add("Width", 20);
			column.Caption = "Наименование";

			column = dtRes.Columns.Add("FirmCr");
			column.Caption = "Производитель";
			column.ExtendedProperties.Add("Width", 10);

			column = dtRes.Columns.Add("CustomerCost", typeof(decimal));
			column.Caption = CustomerFirmName;
			column.ExtendedProperties.Add("Width", 6);

			column = dtRes.Columns.Add("CustomerQuantity");
			column.Caption = "Количество";
			column.ExtendedProperties.Add("Width", 4);

			column = dtRes.Columns.Add("MinCost", typeof(decimal));
			column.Caption = "Мин. цена";
			column.ExtendedProperties.Add("Width", 6);
			column.ExtendedProperties.Add("Color", Color.LightSeaGreen);

			column = dtRes.Columns.Add("LeaderName");
			column.Caption = "Лидер";
			column.ExtendedProperties.Add("Width", 9);
			column.ExtendedProperties.Add("Color", Color.LightSkyBlue);

			dtRes.Columns.Add("Differ", typeof(decimal));
			dtRes.Columns["Differ"].Caption = "Разница";

			column = dtRes.Columns.Add("DifferPercents", typeof(decimal));
			column.Caption = "% разницы";
			column.ExtendedProperties.Add("AsDecimal", "");

			column = dtRes.Columns.Add("AvgCost", typeof(decimal));
			column.Caption = "Средняя цена";
			column.ExtendedProperties.Add("Width", 6);

			column = dtRes.Columns.Add("MaxCost", typeof(decimal));
			column.Caption = "Макс. цена";
			column.ExtendedProperties.Add("Width", 6);

			firstColumnCount = dtRes.Columns.Count;

			var priceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows) {
				column = dtRes.Columns.Add("Cost" + priceIndex.ToString(), typeof(decimal));
				column.Caption = "Цена";
				column.ExtendedProperties.Add("Width", 6);

				if (ShowQuantity) {
					column = dtRes.Columns.Add("Quantity" + priceIndex.ToString());
					column.Caption = "Кол-во";
					column.ExtendedProperties.Add("Width", 4);
				}
				if (_showPercents) {
					column = dtRes.Columns.Add("Percents" + priceIndex.ToString(), typeof(decimal));
					column.Caption = "% разницы";
					column.ExtendedProperties.Add("AsDecimal", "");
				}
				priceIndex++;
			}
			if (priceIndex != 0)
				priceBlockSize = (dtRes.Columns.Count - firstColumnCount) / priceIndex;
			if (!HideHeader) {
				var newrow = dtRes.NewRow();
				dtRes.Rows.Add(newrow);

				newrow = dtRes.NewRow();
				dtRes.Rows.Add(newrow);
			}

			foreach (DataRow drCatalog in _dsReport.Tables["Catalog"].Rows) {
				var newrow = dtRes.NewRow();
				newrow["FullName"] = drCatalog["FullName"];
				newrow["FirmCr"] = drCatalog["FirmCr"];
				var drCatalog1 = new DataRow[0];

				if (!_byWeightCosts) {
					if (drCatalog["Cfc"] == DBNull.Value)
						drCatalog1 = _dsReport.Tables["MinCatalog"].Select("Code = '" + drCatalog["Code"] +
							"' and CatalogCode = '" + drCatalog["CatalogCode"] + "'" + " and Cfc is null ");
					else
						drCatalog1 = _dsReport.Tables["MinCatalog"].Select("Code = '" + drCatalog["Code"] +
							"' and CatalogCode = '" + drCatalog["CatalogCode"] + "'" + " and Cfc = '" + drCatalog["Cfc"].ToString() + "'");
				}
				else if (drCatalog["Cfc"] == DBNull.Value)
					drCatalog1 = _dsReport.Tables["MinCatalog"].Select("CatalogCode = '" + drCatalog["CatalogCode"] + "'" + " and Cfc is null ");
				else
					drCatalog1 = _dsReport.Tables["MinCatalog"].Select("CatalogCode = '" + drCatalog["CatalogCode"] + "'" + " and Cfc = '" + drCatalog["Cfc"].ToString() + "'");

				if (drCatalog1.Length > 0 && drCatalog1[0]["MinCost"] != DBNull.Value) {
					newrow["MinCost"] = Convert.ToDecimal(drCatalog1[0]["MinCost"]);
					newrow["AvgCost"] = Convert.ToDecimal(drCatalog1[0]["AvgCost"]);
					newrow["MaxCost"] = Convert.ToDecimal(drCatalog1[0]["MaxCost"]);
				}

				//Если есть ID, то мы можем заполнить поле Code и, возможно, остальные поля   предложение SourcePC существует
				DataRow[] drsMin = new DataRow[1];
				if (!(drCatalog["ID"] is DBNull)) {
					newrow["Code"] = drCatalog["Code"];
					//Производим поиск предложения по данной позиции по интересующему прайс-листу
					var drsCore = dtCore.Select("ID = " + drCatalog["ID"], "Cost asc");
					if (drsCore.Length > 0) {
						drsMin = dtCore.Select("CatalogCode = '" + drsCore[0]["CatalogCode"] + "' and PriceCode = "
							+ drsCore[0]["PriceCode"].ToString() + " and Code = '" + drsCore[0]["Code"] + "'", "Cost asc");

						//Если в Core предложений по данному SourcePC не существует, то прайс-лист ассортиментный или не включен клиентом в обзор
						//В этом случае данные поля не заполняется и в сравнении такой прайс-лист не участвует
						if ((drsMin.Length > 0)) {
							foreach (DataRow dataRow in drsMin) {
								if (newrow["CustomerCost"] is DBNull && Convert.ToBoolean(dataRow["Junk"]) == false && dataRow["Cost"] != DBNull.Value) {
									newrow["CustomerCost"] = Convert.ToDecimal(dataRow["Cost"]);
								}
								double customerQuantity;
								double quantity;
								if (newrow["CustomerQuantity"] is DBNull || !double.TryParse(newrow["CustomerQuantity"].ToString(), out customerQuantity)) {
									newrow["CustomerQuantity"] = dataRow["Quantity"];
								}
								else if (double.TryParse(dataRow["Quantity"].ToString(), out quantity))
									newrow["CustomerQuantity"] = quantity + customerQuantity;
							}
							if (newrow["CustomerCost"].Equals(newrow["MinCost"]) && !String.IsNullOrEmpty(newrow["MinCost"].ToString()))
								newrow["LeaderName"] = "+";
						}
					}
				}

				//Если имя лидера неустановлено, то выставляем имя лидера
				if (newrow["LeaderName"] is DBNull) {
					//Устанавливаем разность между ценой SourcePC и минимальной ценой
					if (!(newrow["CustomerCost"] is DBNull)) {
						var minCost = (decimal)newrow["MinCost"];
						var customerCost = (decimal)newrow["CustomerCost"];
						newrow["Differ"] = customerCost - minCost;
						if (customerCost != 0)
							newrow["DifferPercents"] = Math.Round((customerCost - minCost) / customerCost * 100, 0);
					}

					//Выбираем позиции с минимальной ценой, отличные от SourcePC
					if (!(drCatalog1.Length == 0 || drCatalog1[0]["MinCost"] is DBNull)) {
						drsMin = dtCore.Select(string.Format("CatalogCode = {0}{1} and Cost = {2}",
							drCatalog["CatalogCode"],
							GetProducerFilter(drCatalog),
							((decimal)drCatalog1[0]["MinCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat)));

						if (drsMin.Length > 0) {
							var leaderNames = new List<string>();
							foreach (var drmin in drsMin) {
								var drs = dtPrices.Select(
									"PriceCode=" + drmin["PriceCode"] +
										" and RegionCode = " + drmin["RegionCode"]);
								if (drs.Length > 0)
									if (!leaderNames.Contains(drs[0]["FirmName"].ToString()))
										leaderNames.Add(drs[0]["FirmName"].ToString());
							}
							newrow["LeaderName"] = String.Join("; ", leaderNames.ToArray());
						}
						if (String.IsNullOrEmpty(newrow["LeaderName"].ToString()) && !String.IsNullOrEmpty(newrow["MinCost"].ToString()))
							newrow["LeaderName"] = "+";
					}
				}
				else {
					//Ищем первую цену, которая будет больше минимальной цены
					decimal minCostAdd = 0;
					if (drCatalog1.Length > 0 && drCatalog1[0]["MinCost"] != DBNull.Value)
						minCostAdd = (decimal)drCatalog1[0]["MinCost"];
					drsMin = dtCore.Select(
						"CatalogCode = " + drCatalog["CatalogCode"] +
							" and PriceCode <> " + SourcePC +
							GetProducerFilter(drCatalog) +
							" and Cost > " + minCostAdd.ToString(CultureInfo.InvariantCulture.NumberFormat),
						"Cost asc");

					if (drsMin.Length > 0) {
						var customerCost = Convert.ToDecimal(newrow["CustomerCost"]);
						var cost = Convert.ToDecimal(drsMin[0]["Cost"]);
						newrow["Differ"] = customerCost - cost;
						if (customerCost != 0)
							newrow["DifferPercents"] = Math.Round((customerCost - cost) / customerCost * 100, 0);
					}
				}

				//Выбираем позиции и сортируем по возрастанию цен для того, чтобы по каждому прайс-листы выбрать минимальную цену по одному и тому же CatalogCode
				drsMin = dtCore.Select("CatalogCode = " + drCatalog["CatalogCode"] + GetProducerFilter(drCatalog), "Cost asc");
				foreach (var dtPos in drsMin) {
					var dr = dtPrices.Select("PriceCode=" + dtPos["PriceCode"] + " and RegionCode = " + dtPos["RegionCode"]);
					//Проверка на случай получения прайса SourcePC, т.к. этот прайс не будет в dtPrices
					if (dr.Length > 0) {
						priceIndex = dtPrices.Rows.IndexOf(dr[0]);

						//Если мы еще не установили значение у поставщика, то делаем это
						//раньше вставляли последнее значение, которое было максимальным
						if (newrow["Cost" + priceIndex] is DBNull && Convert.ToBoolean(dtPos["Junk"]) == false) {
							newrow["Cost" + priceIndex] = dtPos["Cost"];

							var percentColumn = dtRes.Columns["Percents" + priceIndex];
							if (percentColumn != null && newrow["MinCost"] != DBNull.Value) {
								var mincost = Convert.ToDouble(newrow["MinCost"]);
								var pricecost = Convert.ToDouble(dtPos["Cost"]);
								if (pricecost > 0)
									newrow[percentColumn] = Math.Round(((pricecost - mincost) * 100) / pricecost, 0);
							}
						}

						double quantity;
						double columnQuantity;
						var quantityColumn = dtRes.Columns["Quantity" + priceIndex];
						if (quantityColumn != null)
							if (newrow[quantityColumn] is DBNull || !double.TryParse(newrow[quantityColumn].ToString(), out columnQuantity))
								newrow[quantityColumn] = dtPos["Quantity"];
							else if (!(dtPos["Quantity"] is DBNull) && double.TryParse(dtPos["Quantity"].ToString(), out quantity))
								newrow[quantityColumn] = columnQuantity + quantity;
					}
				}

				dtRes.Rows.Add(newrow);
			}

			if (HideAllExcept4) {
				dtRes.Columns.Remove("CustomerCost");
				dtRes.Columns.Remove("CustomerQuantity");
				dtRes.Columns.Remove("MinCost");
				dtRes.Columns.Remove("LeaderName");
				dtRes.Columns.Remove("Differ");
				dtRes.Columns.Remove("DifferPercents");
				dtRes.Columns.Remove("AvgCost");
				dtRes.Columns.Remove("MaxCost");
				firstColumnCount = firstColumnCount - 8;
			}
		}

		private bool ShowQuantity
		{
			get { return (_reportType == 2) || (_reportType == 4); }
		}

		private string GetProducerFilter(DataRow drCatalog)
		{
			if (_reportType <= 2)
				return "";
			if (drCatalog["Cfc"] == DBNull.Value)
				if(SourcePriceType == (int)PriceType.Assortment)
					return "";
				else
					return " and CodeFirmCr is null";
			return " and CodeFirmCr = " + drCatalog["Cfc"];
		}

		protected void GetSourceCodes()
		{
			var EnabledPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					DataAdapter.SelectCommand.Connection,
					"select PriceCode from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
					new MySqlParameter("?SourcePC", SourcePC),
					new MySqlParameter("?SourceRegionCode", SourceRegionCode)));
			if (EnabledPrice == 0 && _byBaseCosts) {
				EnabledPrice = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
						DataAdapter.SelectCommand.Connection,
						"select PriceCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
						new MySqlParameter("?SourcePC", SourcePC)));
				if (EnabledPrice != 0) {
					SourceRegionCode = Convert.ToUInt64(
						MySqlHelper.ExecuteScalar(
							DataAdapter.SelectCommand.Connection,
							"select RegionCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
							new MySqlParameter("?SourcePC", SourcePC)));
				}
			}

			//Добавляем к таблице Core поле CatalogCode и заполняем его
			DataAdapter.SelectCommand.CommandText = "alter table Core add column CatalogCode int unsigned, add key CatalogCode(CatalogCode);";
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
			if (_calculateByCatalog)
				DataAdapter.SelectCommand.CommandText = "update Core, catalogs.products set Core.CatalogCode = products.CatalogId where products.Id = Core.ProductId;";
			else
				DataAdapter.SelectCommand.CommandText = "update Core set CatalogCode = ProductId;";
			DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);

			DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS TmpSourceCodes;
CREATE temporary table TmpSourceCodes(
  ID bigint unsigned,
  PriceCode int(32) unsigned,
  RegionCode bigint unsigned,
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
  key SynonymCode(SynonymCode)
) engine = MEMORY PACK_KEYS = 0;";

			if (EnabledPrice == 0) {
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
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);

#if DEBUG
			DataAdapter.SelectCommand.CommandText = "select * from TmpSourceCodes";
			DataAdapter.Fill(_dsReport, "TmpSourceCodes");
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
			ProfileHelper.WriteLine(String.Format("TmpSourceCodes count {0}", _dsReport.Tables["TmpSourceCodes"].Rows.Count));
#endif

			DataAdapter.SelectCommand.CommandText = @"
select
  Core.Id,
  Core.CatalogCode,
  FarmCore.CodeFirmCr,
  Core.Cost,
  Core.PriceCode,
  Core.RegionCode,
  FarmCore.Quantity,
  FarmCore.Junk,
  FarmCore.Code
from
  Core,
  farm.core0 FarmCore
where
  FarmCore.Id = core.id";

#if DEBUG
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
#endif

			//todo: изменить заполнение в другую таблицу
			DataAdapter.Fill(_dsReport, "AllCoreT");
			ProfileHelper.WriteLine(String.Format("Result rows count {0}", _dsReport.Tables["AllCoreT"].Rows.Count));

			DataAdapter.SelectCommand.CommandText = @"
select
  ActivePrices.PriceCode, ActivePrices.RegionCode, ActivePrices.PriceDate, ActivePrices.FirmName
from
  ActivePrices
where
  (ActivePrices.PriceCode <> ?SourcePC or ActivePrices.RegionCode <> ?SourceRegionCode)
order by ActivePrices.PositionCount DESC";
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
#if DEBUG
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
#endif
			DataAdapter.Fill(_dsReport, "Prices");
		}

		protected void GetMinPrice()
		{
			string SqlCommandText = @"
select
  SourcePrice.ID,
  ifnull(SourcePrice.Code,'') as Code,
  ifnull(AllPrices.CatalogCode, SourcePrice.CatalogCode) as CatalogCode, ";
			if (_calculateByCatalog)
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetCatalogProductNameSubquery("AllPrices.ProductId"));
			else
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", QueryParts.GetFullFormSubquery("FarmCore.ProductId", true));
			SqlCommandText += @"
  min(AllPrices.cost) As MinCost, -- здесь должна быть минимальная цена
  avg(AllPrices.cost) As AvgCost, -- здесь должна быть средняя цена
  max(AllPrices.cost) As MaxCost, -- здесь должна быть минимальная цена";

			//Если отчет без учета производителя, то код не учитываем и выводим "-"
			if (_reportType <= 2)
				SqlCommandText += @"
  '-' as FirmCr,
  0 As Cfc ";
			else
				SqlCommandText += @"
  ifnull(sfc.Synonym, Cfc.Name) as FirmCr,
  cfc.Id As Cfc ";

			SqlCommandText += @"
from
 (
  catalogs.products,
  farm.core0 FarmCore,";

			//Если отчет полный, то интересуют все прайс-листы, если нет, то только SourcePC
			if (_reportIsFull) {
				if (_reportType <= 2)
					SqlCommandText += @"
  Core AllPrices
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
  Core AllPrices
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=FarmCore.codefirmcr";
			}
			else
				SqlCommandText += @"
  Core AllPrices,
  TmpSourceCodes SourcePrice
 )";
			//Если отчет с учетом производителя, то пересекаем с таблицей Producers
			if (_reportType > 2)
				SqlCommandText += @"
  left join catalogs.Producers cfc on cfc.Id = FarmCore.codefirmcr";

			SqlCommandText += @"
  left join farm.synonym s on s.SynonymCode = SourcePrice.SynonymCode
  left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = SourcePrice.SynonymFirmCrCode
where
  products.id = AllPrices.ProductId
  and FarmCore.Id = AllPrices.Id";

			SqlCommandText += @"
and (( ( (AllPrices.PriceCode <> SourcePrice.PriceCode) or (AllPrices.RegionCode <> SourcePrice.RegionCode) or (SourcePrice.id is null) ) and (FarmCore.Junk =0) and (FarmCore.Await=0) )
	  or ( (AllPrices.PriceCode = SourcePrice.PriceCode) and (AllPrices.RegionCode = SourcePrice.RegionCode) and (AllPrices.Id = SourcePrice.id) ) )";

			//Если отчет не полный, то выбираем только те, которые есть в SourcePC
			if (!_reportIsFull) {
				if (_reportType <= 2)
					SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=FarmCore.codefirmcr ";
			}

			SqlCommandText += @"
group by SourcePrice.Code, CatalogCode, Cfc";
			if ((!_reportIsFull) && (_reportSortedByPrice))
				SqlCommandText += @"
order by SourcePrice.ID";
			else
				SqlCommandText += @"
order by FullName, FirmCr";
			DataAdapter.SelectCommand.CommandText = SqlCommandText;
			DataAdapter.Fill(_dsReport, "MinCatalog");

#if DEBUG
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
#endif
		}

		protected void GetCatalog()
		{
			string SqlCommandText = @"
select
  SourcePrice.ID,
  ifnull(SourcePrice.Code,'') as Code,
  ifnull(AllPrices.CatalogCode, SourcePrice.CatalogCode) as CatalogCode, ";
			if (_calculateByCatalog)
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetCatalogProductNameSubquery("AllPrices.ProductId"));
			else
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", QueryParts.GetFullFormSubquery("FarmCore.ProductId", true));

			//Если отчет без учета производителя, то код не учитываем и выводим "-"
			if (_reportType <= 2)
				SqlCommandText += @"
  '-' as FirmCr,
  0 As Cfc ";
			else
				SqlCommandText += @"
  ifnull(sfc.Synonym, Cfc.Name) as FirmCr,
  cfc.Id As Cfc ";

			SqlCommandText += @"
from
 (
  catalogs.products,
  farm.core0 FarmCore,";

			//Если отчет полный, то интересуют все прайс-листы, если нет, то только SourcePC
			if (_reportIsFull) {
				if (_reportType <= 2)
					SqlCommandText += @"
  Core AllPrices
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
  Core AllPrices
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=FarmCore.codefirmcr";
			}
			else
				SqlCommandText += @"
  Core AllPrices,
  TmpSourceCodes SourcePrice
 )";
			//Если отчет с учетом производителя, то пересекаем с таблицей Producers
			if (_reportType > 2)
				SqlCommandText += @"
  left join catalogs.Producers cfc on cfc.Id = FarmCore.codefirmcr";

			SqlCommandText += @"
  left join farm.synonym s on s.SynonymCode = SourcePrice.SynonymCode
  left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = SourcePrice.SynonymFirmCrCode
where
  products.id = AllPrices.ProductId
  and FarmCore.Id = AllPrices.Id";

			SqlCommandText += @"
and (( ( (AllPrices.PriceCode <> SourcePrice.PriceCode) or (AllPrices.RegionCode <> SourcePrice.RegionCode) or (SourcePrice.id is null) ) and (FarmCore.Junk =0) and (FarmCore.Await=0) )
	  or ( (AllPrices.PriceCode = SourcePrice.PriceCode) and (AllPrices.RegionCode = SourcePrice.RegionCode) and (AllPrices.Id = SourcePrice.id) ) )";

			//Если отчет не полный, то выбираем только те, которые есть в SourcePC
			if (!_reportIsFull) {
				if (_reportType <= 2)
					SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=FarmCore.codefirmcr ";
			}

			SqlCommandText += @"
group by SourcePrice.Code, FullName, CatalogCode, Cfc";
			if ((!_reportIsFull) && (_reportSortedByPrice))
				SqlCommandText += @"
order by SourcePrice.ID";
			else
				SqlCommandText += @"
order by FullName, FirmCr";
			DataAdapter.SelectCommand.CommandText = SqlCommandText;
			DataAdapter.Fill(_dsReport, "Catalog");

#if DEBUG
			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
			var cnt = _dsReport.Tables["Catalog"].Rows.Count;
			ProfileHelper.WriteLine($"catalog count {cnt}");
#endif
		}

		protected override void FormatExcel(string fileName)
		{
			ExcelHelper.Workbook(fileName, wb => {
				var ws = (_Worksheet)wb.Worksheets["rep" + ReportCode.ToString()];

				ws.Name = GetSheetName();
				ws.Activate();

				var result = _dsReport.Tables["Results"];
				//очищаем заголовки
				for (var i = 0; i < result.Columns.Count; i++)
					ws.Cells[1, i + 1] = "";

				var tableBeginRowIndex = 1;
				var rowCount = result.Rows.Count;
				var columnCount = result.Columns.Count;

				if (!HideHeader) {
					tableBeginRowIndex = 3;
					if (!String.IsNullOrEmpty(_clientsNames)) // Добавляем строку чтобы вставить выбранные аптеки
						tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, columnCount, $"Выбранные аптеки: {_clientsNames}");
					if (!String.IsNullOrEmpty(_suppliers))
						tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, columnCount, $"Список поставщиков: {_suppliers}");
					if (!String.IsNullOrEmpty(_ignoredSuppliers))
						tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, columnCount, $"Игнорируемые поставщики: {_ignoredSuppliers}");
					if (!String.IsNullOrEmpty(_suppliers2))
						tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, columnCount, $"В отчете размещены позиции, минимальные цены по которым принадлежат поставщикам: {_suppliers2}");

					ExcelHelper.FormatHeader(ws, tableBeginRowIndex, result);
					//Форматирование заголовков прайс-листов
					FormatLeaderAndPrices(ws);
				}
				var lastRowIndex = rowCount + tableBeginRowIndex;

				//рисуем границы на всю таблицу
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[lastRowIndex, columnCount]].Borders.Weight = XlBorderWeight.xlThin;
				//Устанавливаем шрифт листа
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";

				//Устанавливаем АвтоФильтр на все колонки
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[lastRowIndex, columnCount]].Select();
				((Range)wb.Application.Selection).AutoFilter(1, Missing.Value, XlAutoFilterOperator.xlAnd, Missing.Value, true);

				//Замораживаем некоторые колонки и столбцы
				ws.Activate();
				ws.Application.ActiveWindow.SplitColumn = firstColumnCount;
				ws.Application.ActiveWindow.FreezePanes = true;

				if (!HideHeader) {
					//Объединяем несколько ячеек, чтобы в них написать текст
					ws.Range[ws.Cells[1, 1], ws.Cells[2, firstColumnCount]].Select();
					((Range)wb.Application.Selection).Merge(null);
					if(_byBaseCosts)
						reportCaptionPreffix += " по базовым ценам";
					else if(_byWeightCosts)
						reportCaptionPreffix += " по взвешенным ценам по данным на " + GetStatOffersDate().ToShortDateString();
					if (!WithoutAssortmentPrice) {
						if (_reportType < 3)
							wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " без учета производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();
						else
							wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " с учетом производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();
					}
					else if (_reportType < 3)
						wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " без учета производителя создан " + DateTime.Now.ToString();
					else
						wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " с учетом производителя создан " + DateTime.Now.ToString();
				}
			});
		}

		protected virtual void FormatLeaderAndPrices(_Worksheet ws)
		{
			var columnPrefix = firstColumnCount + 1;
			var priceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows) {
				var columnIndex = columnPrefix + priceIndex * priceBlockSize;
				if(columnIndex < 255) {
					//Устанавливаем название фирмы
					ws.Cells[1, columnIndex] = drPrice["FirmName"].ToString();
					//Устанавливаем дату фирмы
					ws.Cells[2, columnIndex] = drPrice["PriceDate"].ToString();
				}
				priceIndex++;
			}
		}
	}
}