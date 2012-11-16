using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Configuration;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem
{
	//Специальный отчет прайс-листов
	public class SpecReport : ProviderReport
	{
		//1 - без учета производителя и с колчеством
		//2 - без учета производителя и колчеством
		//3 - с учетом производителя и без колчеством
		//4 - с учетом производителя и с колчеством
		protected int _reportType;
		protected bool _showPercents;
		protected bool _reportIsFull;
		protected bool _reportSortedByPrice;
		//Расчитывать отчет по каталогу (CatalogId, Name, Form), если не установлено, то расчет будет производится по продуктам (ProductId)
		protected bool _calculateByCatalog;

		protected int SourcePC, FirmCode;
		protected ulong SourceRegionCode;
		protected int _priceCode;
		protected string CustomerFirmName;

		protected string reportCaptionPreffix;

		protected string _suppliers;
		protected string _ignoredSuppliers;

		protected string _clientsNames = "";

		protected bool WithoutAssortmentPrice;

		protected bool _showCodeCr;

		protected bool _codesWithoutProducer;
		//количество столбцпв до начала блоков прайс листов
		private int firstColumnCount;
		//количество столбцов в блоке прайс листа
		private int priceBlockSize;

		private int SourcePriceType;

		protected SpecReport() // конструктор для возможности тестирования
		{
		}

		public SpecReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
			reportCaptionPreffix = "Специальный отчет";
		}
		/// <summary>
		/// результаты отчета для тестов
		/// </summary>
		public DataSet DSResult
		{
			get { return _dsReport; }
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_reportType = (int)getReportParam("ReportType");
			_showPercents = (bool)getReportParam("ShowPercents");
			_reportIsFull = (bool)getReportParam("ReportIsFull");
			_reportSortedByPrice = (bool)getReportParam("ReportSortedByPrice");
			if (!_byBaseCosts)
				_clientCode = (int)getReportParam("ClientCode");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
			_priceCode = (int)getReportParam("PriceCode");
		}
		protected void GetWeightMinPrice(ExecuteArgs e)
		{
			string SqlCommandText = @"
select
  SourcePrice.ID,
  SourcePrice.Code,
  ifnull(AllPrices.CatalogCode, SourcePrice.CatalogCode) as CatalogCode, ";
			SqlCommandText += String.Format(@"
(
	select catalog.Name
	from catalogs.catalog
	where Catalog.Id = AllPrices.ProductId
) as FullName,
");
			/*if (_calculateByCatalog)
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetCatalogProductNameSubquery("AllPrices.ProductId"));
			else
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetProductNameSubquery("AllPrices.ProductId"));*/
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
  Cfc.Name as FirmCr,
  cfc.Id As Cfc ";

			if(SourcePriceType == (int)PriceType.Assortment)
				SqlCommandText += GetFromForWeightAssortmentPrice();
			else {
				SqlCommandText += @"
from
 (

reports.averagecosts AvgCost,
  Core AllPrices,
catalogs.assortment";


				//Если отчет полный, то интересуют все прайс-листы, если нет, то только SourcePC
				if (_reportIsFull) {
					if (_reportType <= 2)
						SqlCommandText += @"
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
					else
						SqlCommandText += @"

 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=assortment.ProducerId";
				}
				else
					SqlCommandText += @",
  TmpSourceCodes SourcePrice
 )";
				//Если отчет с учетом производителя, то пересекаем с таблицой Producers
				if (_reportType > 2)
					SqlCommandText += @"
  left join catalogs.Producers cfc on cfc.Id = assortment.ProducerId";

				SqlCommandText += @"
where
  assortment.Id = AvgCost.AssortmentId
and AvgCost.Id = AllPrices.Id
";

				SqlCommandText += @"
and (( ( (AllPrices.PriceCode <> SourcePrice.PriceCode) or (AllPrices.RegionCode <> SourcePrice.RegionCode) or (SourcePrice.id is null) ))
	  or ( (AllPrices.PriceCode = SourcePrice.PriceCode) and (AllPrices.RegionCode = SourcePrice.RegionCode) and (AllPrices.Id = SourcePrice.id) ) )";

				//Если отчет не полный, то выбираем только те, которые есть в SourcePC
				if (!_reportIsFull) {
					if (_reportType <= 2)
						SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode ";
					else
						SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=assortment.ProducerId and SourcePrice.CatalogCode=assortment.CatalogId";
				}
			}
			SqlCommandText += @"
group by AllPrices.CatalogCode, Cfc";
			if ((!_reportIsFull) && (_reportSortedByPrice))
				SqlCommandText += @"
order by SourcePrice.ID";
			else
				SqlCommandText += @"
order by FullName, FirmCr";
			e.DataAdapter.SelectCommand.CommandText = SqlCommandText;
			e.DataAdapter.Fill(_dsReport, "Catalog");

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
			var cnt = _dsReport.Tables["Catalog"].Rows.Count;
#endif
		}

		private string GetFromForWeightAssortmentPrice()
		{
			string result = @"from
catalogs.catalog
join Core AllPrices on catalog.Id = AllPrices.ProductId";
			if(!_reportIsFull)
				result += @"
right join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
			else {
				result += @"
left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
			}
			if (_reportType > 2)
				result += @"
	left join catalogs.assortment on assortment.catalogid=catalog.id
	left join catalogs.Producers cfc on cfc.Id = SourcePrice.CodeFirmCr
where assortment.ProducerId = SourcePrice.CodeFirmCr or SourcePrice.CodeFirmCr is null";

			return result;
		}

		public void GetWeightCostSource(ExecuteArgs e)
		{
			//Добавляем к таблице Core поле CatalogCode и заполняем его
			e.DataAdapter.SelectCommand.CommandText = "alter table Core add column CatalogCode int unsigned, add key CatalogCode(CatalogCode);";
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = "update Core set CatalogCode = ProductId;";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = @"
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

			e.DataAdapter.SelectCommand.CommandText += @"
INSERT INTO TmpSourceCodes
Select
  Core.ID,
  Core.PriceCode,
  Core.RegionCode,
  (SELECT GROUP_CONCAT(distinct code SEPARATOR ', ') FROM farm.core0 fc join catalogs.products cp on fc.ProductId=cp.Id
where PriceCode=?SourcePrice and cp.CatalogId = Core.ProductId) as Code,
  Core.Cost,";

			e.DataAdapter.SelectCommand.CommandText += "Core.ProductId, ";
			if(SourcePriceType == (int)PriceType.Assortment)
				e.DataAdapter.SelectCommand.CommandText += @"
Core.ProducerId
FROM
  Core
WHERE
Core.PriceCode = ?SourcePC
and Core.RegionCode = ?SourceRegionCode;";
			else
				e.DataAdapter.SelectCommand.CommandText += @"
  Assortment.ProducerId
FROM
  Core,
  reports.averagecosts,
  catalogs.assortment
WHERE
Core.Id=averagecosts.Id
and assortment.id = averagecosts.AssortmentId
and Core.PriceCode = ?SourcePC
and Core.RegionCode = ?SourceRegionCode;";

			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePrice", _priceCode);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
#if DEBUG
			e.DataAdapter.SelectCommand.CommandText = @"select * from TmpSourceCodes";
			e.DataAdapter.Fill(_dsReport, "TmpSourceCodes");
#endif
			e.DataAdapter.SelectCommand.CommandText = @"
select
  Core.Id,
  Core.CatalogCode,
  assortment.ProducerId as CodeFirmCr,
  Core.Cost,
  Core.PriceCode,
  Core.RegionCode,
  Core.Quantity,
  0 as Junk
from
  Core,
  reports.averagecosts,
  catalogs.assortment
where
Core.Id=averagecosts.Id
and assortment.id = averagecosts.AssortmentId";

			e.DataAdapter.Fill(_dsReport, "AllCoreT");

			e.DataAdapter.SelectCommand.CommandText = @"
select
 distinct Core.PriceCode, Core.RegionCode, '' as PriceDate, concat(suppliers.Name, ' - ', regions.Region) as FirmName, st.Position
from
  (usersettings.Core, Customers.suppliers, farm.regions)
left join (select pd.firmcode, SUM(pi.RowCount) as Position
FROM
    usersettings.PricesData pd
    JOIN usersettings.PricesCosts pc on pc.PriceCode = pd.PriceCode and pc.BaseCost = 1
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


			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			e.DataAdapter.Fill(_dsReport, "Prices");
		}

		public void AddSourcePriceToCore(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;

			e.DataAdapter.SelectCommand.CommandText = "alter table Core add column ProducerId int unsigned;";
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = @"
set @cnt= (select max(Id) from usersettings.Core);
insert into usersettings.Core
select ?SourcePC, ?SourceRegionCode, p.CatalogId,
if(if(round(cc.Cost * pd.Upcost, 2) < MinBoundCost, MinBoundCost, round(cc.Cost * pd.Upcost, 2)) > MaxBoundCost,
	MaxBoundCost, if(round(cc.Cost*pd.UpCost,2) < MinBoundCost, MinBoundCost, round(cc.Cost * pd.Upcost, 2))),
'',
@cnt:=@cnt+1,
c.Quantity,
c.CodeFirmCr
from
farm.core0 c
join usersettings.PricesData pd on c.PriceCode = pd.PriceCode
join usersettings.PricesCosts pc on pd.PriceCode = pc.PriceCode and pc.BaseCost = 1
left JOIN farm.CoreCosts cc on cc.Core_Id = c.Id and cc.PC_CostCode = pc.CostCode
join catalogs.products p on c.ProductId = p.Id
where
c.PriceCode = ?SourcePrice;";

			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePrice", _priceCode);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		public bool IsExistsPriceInCore(ExecuteArgs e, int priceCode, ulong region)
		{
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
			e.DataAdapter.SelectCommand.CommandText = @"
select count(*) from usersettings.Core
where regionCode = ?region and PriceCode = ?price;";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?region", region);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?price", priceCode);
			var count = e.DataAdapter.SelectCommand.ExecuteScalar();
			return int.Parse(count.ToString()) > 0;
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			base.GenerateReport(e);
			//Если прайс-лист равен 0, то он не установлен, поэтому берем прайс-лист относительно клиента, для которого делается отчет
			if (_priceCode == 0)
				throw new ReportException("Для специального отчета не указан параметр \"Прайс-лист\".");

			//Проверка актуальности прайс-листа
			int ActualPrice = Convert.ToInt32(
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
and pc.BaseCost = 1
and pim.Id = pc.PriceItemId
and fr.Id = pim.FormRuleId
and (to_days(now())-to_days(pim.PriceDate)) < fr.MaxOld",
					new MySqlParameter("?SourcePC", _priceCode)));
#if !DEBUG
			if (ActualPrice == 0)
				throw new ReportException(String.Format("Прайс-лист {0} ({1}) не является актуальным.", CustomerFirmName, SourcePC));
#endif

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
				if(SourcePriceType == (int)PriceType.Assortment || !IsExistsPriceInCore(e, SourcePC, SourceRegionCode)) {
					ProfileHelper.Next("AdditionGetOffers");
					AddSourcePriceToCore(e);
				}
				ProfileHelper.Next("GetCodes");
				GetWeightCostSource(e);
				ProfileHelper.Next("GetMinPrices");
				GetWeightMinPrice(e);
				ProfileHelper.Next("Calculate");
				Calculate();
				return;
			}

			ProfileHelper.Next("PreGetOffers");
			if (_byBaseCosts) {
				// Отчет готовится по базовым ценам
				//Заполняем код региона прайс-листа как домашний код поставщика этого прайс-листа
				SourceRegionCode = Convert.ToUInt64(
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
				SourceRegionCode = Convert.ToUInt64(
					MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
						@"select RegionCode
	from Customers.Clients
where Id = ?ClientCode",
						new MySqlParameter("?ClientCode", _clientCode)));
			}

			SourcePC = _priceCode;
			CustomerFirmName = GetSupplierName(_priceCode);

			ProfileHelper.Next("GetOffers");
			//Выбираем
			GetOffers(_SupplierNoise);
			ProfileHelper.Next("GetCodes");
			//Получили предложения интересующего прайс-листа в отдельную таблицу
			GetSourceCodes(e);
			ProfileHelper.Next("GetMinPrices");
			//Получили лучшие предложения из всех прайс-листов с учетом требований
			GetMinPrice(e);
			ProfileHelper.Next("Calculate");
			Calculate();
			ProfileHelper.End();

			DoCoreCheck();
		}

		private void DoCoreCheck()
		{
			args.DataAdapter.SelectCommand.CommandText = @"
select c.PriceCode
from Usersettings.Core c
left join farm.core0 c0 on c.Id = c0.Id
where c0.Id is null
group by c.pricecode";
			var data = new DataTable();
			args.DataAdapter.Fill(data);
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
			if(priceIndex != 0)
				priceBlockSize = (dtRes.Columns.Count - firstColumnCount) / priceIndex;
			var newrow = dtRes.NewRow();
			dtRes.Rows.Add(newrow);
			newrow = dtRes.NewRow();
			dtRes.Rows.Add(newrow);

			foreach (DataRow drCatalog in _dsReport.Tables["Catalog"].Rows) {
				newrow = dtRes.NewRow();
				newrow["FullName"] = drCatalog["FullName"];
				newrow["FirmCr"] = drCatalog["FirmCr"];
				if(drCatalog["MinCost"] != DBNull.Value) {
					newrow["MinCost"] = Convert.ToDecimal(drCatalog["MinCost"]);
					newrow["AvgCost"] = Convert.ToDecimal(drCatalog["AvgCost"]);
					newrow["MaxCost"] = Convert.ToDecimal(drCatalog["MaxCost"]);
				}

				//Если есть ID, то мы можем заполнить поле Code и, возможно, остальные поля   предложение SourcePC существует
				DataRow[] drsMin;
				if (!(drCatalog["ID"] is DBNull)) {
					newrow["Code"] = drCatalog["Code"];
					//Производим поиск предложения по данной позиции по интересующему прайс-листу
					var drsCore = dtCore.Select("ID = " + drCatalog["ID"], "Cost asc");
					if(drsCore.Length > 0) {
						drsMin = dtCore.Select("CatalogCode = '" + drsCore[0]["CatalogCode"] + "' and PriceCode = " + drsCore[0]["PriceCode"].ToString(), "Cost asc");
						//Если в Core предложений по данному SourcePC не существует, то прайс-лист асортиментный или не включен клиентом в обзор
						//В этом случае данные поля не заполняется и в сравнении такой прайс-лист не участвует
						if ((drsMin.Length > 0)) {
							foreach (DataRow dataRow in drsMin) {
								if(newrow["CustomerCost"] is DBNull && Convert.ToBoolean(dataRow["Junk"]) == false && dataRow["Cost"] != DBNull.Value) {
									newrow["CustomerCost"] = Convert.ToDecimal(dataRow["Cost"]);
								}
								double customerQuantity;
								double quantity;
								if(newrow["CustomerQuantity"] is DBNull || !double.TryParse(newrow["CustomerQuantity"].ToString(), out customerQuantity)) {
									newrow["CustomerQuantity"] = dataRow["Quantity"];
								}
								else if(double.TryParse(dataRow["Quantity"].ToString(), out quantity))
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
						newrow["DifferPercents"] = Math.Round((customerCost - minCost) / customerCost * 100, 0);
					}

					//Выбираем позиции с минимальной ценой, отличные от SourcePC
					if(!(drCatalog["MinCost"] is DBNull)) {
						drsMin = dtCore.Select(string.Format("CatalogCode = {0}{1} and Cost = {2}",
							drCatalog["CatalogCode"],
							GetProducerFilter(drCatalog),
							((decimal)drCatalog["MinCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat)));

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
					if(drCatalog["MinCost"] != DBNull.Value)
						minCostAdd = (decimal)drCatalog["MinCost"];
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
						if(customerCost != 0)
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
							if (percentColumn != null) {
								var mincost = Convert.ToDouble(newrow["MinCost"]);
								var pricecost = Convert.ToDouble(dtPos["Cost"]);
								try {
									if(pricecost > 0)
										newrow[percentColumn] = Math.Round(((pricecost - mincost) * 100) / pricecost, 0);
								}
								catch(Exception ex) {
									throw;
								}
							}
						}

						double quantity;
						double columnQuantity;
						var quantityColumn = dtRes.Columns["Quantity" + priceIndex];
						if (quantityColumn != null)
							if(newrow[quantityColumn] is DBNull || !double.TryParse(newrow[quantityColumn].ToString(), out columnQuantity))
								newrow[quantityColumn] = dtPos["Quantity"];
							else if(!(dtPos["Quantity"] is DBNull) && double.TryParse(dtPos["Quantity"].ToString(), out quantity))
								newrow[quantityColumn] = columnQuantity + quantity;
					}
				}

				dtRes.Rows.Add(newrow);
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

		protected void GetSourceCodes(ExecuteArgs e)
		{
			var EnabledPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					"select PriceCode from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
					new MySqlParameter("?SourcePC", SourcePC),
					new MySqlParameter("?SourceRegionCode", SourceRegionCode)));
			if (EnabledPrice == 0 && _byBaseCosts) {
				EnabledPrice = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						"select PriceCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
						new MySqlParameter("?SourcePC", SourcePC)));
				if (EnabledPrice != 0) {
					SourceRegionCode = Convert.ToUInt64(
						MySqlHelper.ExecuteScalar(
							e.DataAdapter.SelectCommand.Connection,
							"select RegionCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
							new MySqlParameter("?SourcePC", SourcePC)));
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
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

#if DEBUG
			e.DataAdapter.SelectCommand.CommandText = "select * from TmpSourceCodes";
			e.DataAdapter.Fill(_dsReport, "TmpSourceCodes");
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif

			e.DataAdapter.SelectCommand.CommandText = @"
select
  Core.Id,
  Core.CatalogCode,
  FarmCore.CodeFirmCr,
  Core.Cost,
  Core.PriceCode,
  Core.RegionCode,
  FarmCore.Quantity,
  FarmCore.Junk
from
  Core,
  farm.core0 FarmCore
where
  FarmCore.Id = core.id";

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif

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
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
			e.DataAdapter.Fill(_dsReport, "Prices");
		}

		protected void GetMinPrice(ExecuteArgs e)
		{
			string SqlCommand = @"insert into usersettings.Core
select";

			string SqlCommandText = @"
select
  SourcePrice.ID,
  SourcePrice.Code,
  ifnull(AllPrices.CatalogCode, SourcePrice.CatalogCode) as CatalogCode, ";
			if (_calculateByCatalog)
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetCatalogProductNameSubquery("AllPrices.ProductId"));
			else
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetProductNameSubquery("FarmCore.ProductId"));
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

			if(SourcePriceType == (int)PriceType.Assortment)
				SqlCommandText += GetFromForAssortmentPrice();
			else {
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
				//Если отчет с учетом производителя, то пересекаем с таблицой Producers
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
			}
			SqlCommandText += @"
group by SourcePrice.Code, CatalogCode, Cfc";
			if ((!_reportIsFull) && (_reportSortedByPrice))
				SqlCommandText += @"
order by SourcePrice.ID";
			else
				SqlCommandText += @"
order by FullName, FirmCr";
			e.DataAdapter.SelectCommand.CommandText = SqlCommandText;
			e.DataAdapter.Fill(_dsReport, "Catalog");

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
			var cnt = _dsReport.Tables["Catalog"].Rows.Count;
#endif
		}

		private string GetFromForAssortmentPrice()
		{
			string result = @"from
Core AllPrices
join farm.core0 FarmCore on FarmCore.Id = AllPrices.Id
left join catalogs.products on products.id = AllPrices.ProductId
";
			if(!_reportIsFull)
				result += "right join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode";
			else
				result += "left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode";

			if (_reportType > 2)
				result += @"
	left join catalogs.Producers cfc on cfc.Id = SourcePrice.codefirmcr";

			result += @"
left join farm.synonym s on s.SynonymCode = SourcePrice.SynonymCode
left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = SourcePrice.SynonymFirmCrCode";

			if (_reportType > 2)
				result += @"
where SourcePrice.codefirmcr=FarmCore.codefirmcr or SourcePrice.codefirmcr is null";
			return result;
		}

		protected override void FormatExcel(string fileName)
		{
			UseExcel.Workbook(fileName, wb => {
				var ws = (_Worksheet)wb.Worksheets["rep" + ReportCode.ToString()];

				ws.Name = ReportCaption.Substring(0, (ReportCaption.Length < MaxListName) ? ReportCaption.Length : MaxListName);
				ws.Activate();

				var result = _dsReport.Tables["Results"];
				//очищаем заголовки
				for (var i = 0; i < result.Columns.Count; i++)
					ws.Cells[1, i + 1] = "";

				var tableBeginRowIndex = 3;
				var rowCount = result.Rows.Count;
				var columnCount = result.Columns.Count;

				if (!String.IsNullOrEmpty(_clientsNames)) // Добавляем строку чтобы вставить выбранные аптеки
					tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, 12, String.Format("Выбранные аптеки: {0}", _clientsNames));
				if (!String.IsNullOrEmpty(_suppliers))
					tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, 12, String.Format("Список поставщиков: {0}", _suppliers));
				if (!String.IsNullOrEmpty(_ignoredSuppliers))
					tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, 12, String.Format("Игнорируемые поставщики: {0}", _ignoredSuppliers));

				var lastRowIndex = rowCount + tableBeginRowIndex;

				ExcelHelper.FormatHeader(ws, tableBeginRowIndex, result);

				//Форматирование заголовков прайс-листов
				FormatLeaderAndPrices(ws);

				//рисуем границы на всю таблицу
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[lastRowIndex, columnCount]].Borders.Weight = XlBorderWeight.xlThin;
				//Устанавливаем шрифт листа
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";

				//Устанавливаем АвтоФильтр на все колонки
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[lastRowIndex, columnCount]].Select();
				((Range)wb.Application.Selection).AutoFilter(1, Missing.Value, XlAutoFilterOperator.xlAnd, Missing.Value, true);

				//Замораживаем некоторые колонки и столбцы
				ws.Range["L4", Missing.Value].Select();
				wb.Application.ActiveWindow.FreezePanes = true;

				//Объединяем несколько ячеек, чтобы в них написать текст
				ws.Range["A1:K2", Missing.Value].Select();
				((Range)wb.Application.Selection).Merge(null);
				if(_byBaseCosts)
					reportCaptionPreffix += " по базовым ценам";
				else if(_byWeightCosts)
					reportCaptionPreffix += " по взвешенным ценам по данным на " + DateTime.Today.AddDays(-1).ToShortDateString();
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
			});
		}

		protected virtual void FormatLeaderAndPrices(_Worksheet ws)
		{
			var columnPrefix = firstColumnCount + 1;
			var priceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows) {
				var columnIndex = columnPrefix + priceIndex * priceBlockSize;
				//Устанавливаем название фирмы
				ws.Cells[1, columnIndex] = drPrice["FirmName"].ToString();
				//Устанавливаем дату фирмы
				ws.Cells[2, columnIndex] = drPrice["PriceDate"].ToString();
				priceIndex++;
			}
		}
	}
}