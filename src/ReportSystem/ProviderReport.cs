﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using System.Configuration;

namespace Inforoom.ReportSystem
{
	//Вспомогательный отчет, создаваемый по заказу поставщиков
	public abstract class ProviderReport : BaseReport
	{
		//Код клиента, необходимый для получения текущих прайс-листов и предложений, относительно этого клиента
		protected int _clientCode;
		protected int? _SupplierNoise;
		protected int? _userCode;
		protected bool _byBaseCosts; // строить отчет по базовым ценам
		protected bool _byWeightCosts; // строить отчет по взвешенным ценам
		//Список прайсов, для которых нужно вычислять по базовым ценам
		protected List<ulong> _prices;
		// Прайс-код, по которому строится отчет, для добавления в прайсы, для которых считаем по базовым ценам
		protected int _selfPrice = -1;
		//Список регионов, для которых нужно вычислять по базовым ценам
		protected List<ulong> _regions;

		protected ProviderReport() // конструктор для возможности тестирования
		{
		}

		public ProviderReport(ulong reportCode, string reportCaption, MySqlConnection connection, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, format, dsProperties)
		{
		}

		public override void GenerateReport(ExecuteArgs e)
		{
		}

		public override void ReadReportParams()
		{
			if (_reportParams.ContainsKey("SupplierNoise"))
				_SupplierNoise = (int)getReportParam("SupplierNoise");

			_byWeightCosts = reportParamExists("ByWeightCosts") ? (bool)getReportParam("ByWeightCosts") : false;
			if (_byWeightCosts) {
				_regions = (List<ulong>)getReportParam("RegionEqual");
			}
			// если отчет строится по базовым ценам, определяем список прайсов и регионов
			_byBaseCosts = reportParamExists("ByBaseCosts") ? (bool)getReportParam("ByBaseCosts") : false;
			if (_byBaseCosts) {
				if(reportParamExists("PriceCodeEqual"))
					_prices = (List<ulong>)getReportParam("PriceCodeEqual");
				else {
					_prices = null;
				}
				_regions = (List<ulong>)getReportParam("RegionEqual");
			}

			if (_reportParams.ContainsKey("UserCode")) {
				if (!String.IsNullOrEmpty(getReportParam("UserCode").ToString()))
					_userCode = (int)getReportParam("UserCode");
			}
		}

		public virtual List<ulong> GetClientWithSetFilter(List<ulong> RegionEqual, List<ulong> RegionNonEqual,
			List<ulong> PayerEqual, List<ulong> PayerNonEqual,
			List<ulong> Clients, List<ulong> ClientsNON, ulong? checkClientId, ExecuteArgs e)
		{
			var regionalWhere = "(";
			if (RegionEqual.Count != 0) {
				foreach (var region in RegionEqual) {
					regionalWhere += string.Format(" (fc.MaskRegion & {0}) = {0} OR ", region);
				}
			}
			if (RegionNonEqual.Count != 0) {
				foreach (var region in RegionNonEqual) {
					regionalWhere += string.Format(" (fc.MaskRegion & {0}) != {0} OR ", region);
				}
			}
			if (regionalWhere.Length != 1) {
				regionalWhere = regionalWhere.Substring(0, regionalWhere.Length - 3);
				regionalWhere = " AND " + regionalWhere;
				regionalWhere += ")";
			}
			else {
				regionalWhere = string.Empty;
			}
			var payerWhere = string.Empty;
			if (PayerEqual.Count != 0) {
				payerWhere += String.Format(" AND pc.PayerId IN ({0})", PayerEqual.Implode());
			}
			if (PayerNonEqual.Count != 0) {
				payerWhere += String.Format(" AND pc.PayerId NOT IN ({0})", PayerNonEqual.Implode());
			}
			var clientWhere = string.Empty;
			if (Clients.Count != 0) {
				clientWhere += String.Format(" AND fc.Id IN ({0})", Clients.Implode());
			}
			if (ClientsNON.Count != 0) {
				clientWhere += String.Format(" AND fc.Id NOT IN ({0})", ClientsNON.Implode());
			}
			var clientIdWhere = string.Empty;
			if (checkClientId != null) {
				clientIdWhere = String.Format(" AND fc.Id = {0}", checkClientId);
			}
			var where = string.Empty;
			if ((regionalWhere != string.Empty) || (payerWhere != string.Empty) || (clientWhere != string.Empty) || (clientIdWhere != string.Empty))
				where = regionalWhere + payerWhere + clientWhere + clientIdWhere;
			e.DataAdapter.SelectCommand.CommandText =
				string.Format(@"SELECT distinct fc.Id FROM Customers.Clients fc
							join billing.PayerClients pc on fc.Id = pc.ClientId
							join usersettings.RetClientsSet RCS on fc.id = RCS.ClientCode
							WHERE RCS.ServiceClient = 0 and RCS.InvisibleOnFirm = 0 and fc.Status = 1 {0}", where);

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			var result = new List<ulong>();
			while (reader.Read()) {
				result.Add(Convert.ToUInt64(reader["Id"].ToString()));
			}
			reader.Close();
			return result;
		}

		public virtual void NoisingCostInDataTable(DataTable data, string costFieldName, string supplierFieldName, int? supplier)
		{
			if (supplier != null) {
				var rand = new Random();
				foreach (DataRow row in data.Rows) {
					if (row.Field<uint?>(supplierFieldName) != supplier) {
						var randObj = (decimal)rand.NextDouble();
						row[costFieldName] = (1 + (randObj * (randObj > (decimal)0.5 ? 2 : -2)) / 100) * row.Field<decimal>(costFieldName);
					}
				}
			}
		}


		//Получили список действующих прайс-листов для интересующего клиента
		protected void InvokeGetActivePrices()
		{
			ExecuteArgs e = args;
			//удаление временных таблиц
			e.DataAdapter.SelectCommand.CommandText = "drop temporary table IF EXISTS Prices, ActivePrices, Core, MinCosts";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			if (_byBaseCosts)
				GetRegionsPrices(e); // заполняем временную таблицу для передачи списка ПЛ и регионов в хранимую процедуру

			GetBareActivePrices();

			List<ulong> allowedFirms = null;
			if (_reportParams.ContainsKey("FirmCodeEqual"))
				allowedFirms = (List<ulong>)_reportParams["FirmCodeEqual"];
			if (allowedFirms != null && allowedFirms.Count > 0) {
				e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
				e.DataAdapter.SelectCommand.CommandText = String.Format("delete from usersettings.ActivePrices where FirmCode not in ({0})", allowedFirms.Implode());
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
			}

			if (_reportParams.ContainsKey("IgnoredSuppliers")) {
				var suppliers = (List<ulong>)_reportParams["IgnoredSuppliers"];
				if (suppliers != null && suppliers.Count > 0) {
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from usersettings.ActivePrices where FirmCode in ({0})", suppliers.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			if (_reportParams.ContainsKey("PriceCodeValues")) {
				var PriceCodeValues = (List<ulong>)_reportParams["PriceCodeValues"];
				if (PriceCodeValues != null && PriceCodeValues.Count > 0) {
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where PriceCode not in ({0})", PriceCodeValues.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			if (_reportParams.ContainsKey("PriceCodeEqual")) {
				var PriceCodeValues = (List<ulong>)_reportParams["PriceCodeEqual"];
				if (PriceCodeValues != null && PriceCodeValues.Count > 0) {
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where PriceCode not in ({0})", PriceCodeValues.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			if (_reportParams.ContainsKey("PriceCodeNonValues")) {
				var PriceCodeNonValues = (List<ulong>)_reportParams["PriceCodeNonValues"];
				if (PriceCodeNonValues != null && PriceCodeNonValues.Count > 0) {
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where PriceCode in ({0})", PriceCodeNonValues.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			// В списке регионов только доступные клиенту регионы
			if (!_byBaseCosts && _reportParams.ContainsKey("RegionClientEqual")) {
				var RegionClientEqual = (List<ulong>)_reportParams["RegionClientEqual"];
				if (RegionClientEqual != null && RegionClientEqual.Count > 0) {
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where RegionCode not in ({0})", RegionClientEqual.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			//Добавляем в таблицу ActivePrices поле FirmName и заполняем его также, как раньше для отчетов
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;

			e.DataAdapter.SelectCommand.CommandText = @"
alter table ActivePrices add column FirmName varchar(100);
update
  ActivePrices, Customers.suppliers, farm.regions
set
  FirmName = concat(suppliers.Name, '(', ActivePrices.PriceName, ') - ', regions.Region)
where
	activeprices.FirmCode = suppliers.Id
and regions.RegionCode = activeprices.RegionCode";

			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		private void GetBareActivePrices()
		{
			var selectCommand = args.DataAdapter.SelectCommand;

			uint userId = 0;
			// Получаем для него все прайсы
			if (_byBaseCosts) {
				selectCommand.CommandText = "Customers.GetPricesWithBaseCosts";
				selectCommand.CommandType = CommandType.StoredProcedure;
				selectCommand.ExecuteNonQuery();
			}
			else {
				// Получаем пользователя
				userId = GetUserId();
				selectCommand.CommandText = "Customers.GetPrices";
				selectCommand.CommandType = CommandType.StoredProcedure;
				selectCommand.Parameters.Clear();
				selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
				selectCommand.ExecuteNonQuery();
			}

			// Включаем для него все прайсы
			selectCommand.CommandType = CommandType.Text;
			if (_userCode == null) { // если пользователь не выбран через интерфейс
				selectCommand.CommandText = "update usersettings.Prices set DisabledByClient = 0";
				selectCommand.ExecuteNonQuery();
			}

			// Получаем для пользователя активные (которыми теперь являются все) прайсы
			selectCommand.CommandText = "Customers.GetActivePrices";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			selectCommand.ExecuteNonQuery();
		}


		private uint GetUserId()
		{
			// Если пользователь не передан в качестве параметра - берем первого попавшегося
			if (_userCode == null) {
				var command = args.DataAdapter.SelectCommand;
				//Проверка существования и отключения клиента
				command.CommandText = "select * from Customers.Clients cl where cl.Id = " + _clientCode;
				command.CommandType = CommandType.Text;
				using (var reader = command.ExecuteReader()) {
					if (!reader.Read())
						throw new ReportException(String.Format("Невозможно найти клиента с кодом {0}.", _clientCode));
					if (Convert.ToByte(reader["Status"]) == 0)
						throw new ReportException(
							String.Format("Невозможно сформировать отчет по отключенному клиенту {0} ({1}).",
								reader["Name"], _clientCode));
				}
				command.CommandText = "select Id from Customers.Users where ClientId = " + _clientCode +
					" limit 1";
				return Convert.ToUInt32(command.ExecuteScalar());
			}
			return Convert.ToUInt32(_userCode.Value);
		}

		//Получили список предложений для интересующего клиента
		protected void GetOffers(int? noiseFirmCode = null)
		{
			InvokeGetActivePrices();
			InvokeGetOffers(noiseFirmCode);
			args.DataAdapter.SelectCommand.CommandType = CommandType.Text;
		}

		private void InvokeGetOffers(int? noiseFirmCode)
		{
			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.Parameters.Clear();

			if (_byBaseCosts)
				selectCommand.Parameters.AddWithValue("?UserIdParam", null);
			else
				selectCommand.Parameters.AddWithValue("?UserIdParam", GetUserId());

			selectCommand.Parameters.AddWithValue("?NoiseFirmCode", noiseFirmCode);
			selectCommand.CommandText = "Customers.GetOffersReports";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.ExecuteNonQuery();
		}

		/// <summary>
		/// Создает временную таблицу и заполняет ее данными из списков _prices и _regions (если отчет строится по базовым ценам)
		/// Данная таблица затем будет использоваться для ограничения выборки в хранимой процедуре GetPricesWithBaseCosts()
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private void GetRegionsPrices(ExecuteArgs e)
		{
			if(_prices == null) {
				decimal regionMask = 0;
				if (_regions != null)
					regionMask = _regions.Sum(r => Convert.ToDecimal(r));
				e.DataAdapter.SelectCommand.CommandText = "reports.GetPricesByRegionMaskByTypes";
				e.DataAdapter.SelectCommand.CommandType = CommandType.StoredProcedure;
				e.DataAdapter.SelectCommand.Parameters.AddWithValue("?inID", regionMask);
				e.DataAdapter.SelectCommand.Parameters.AddWithValue("?inTypes", "1,2");
				e.DataAdapter.SelectCommand.Parameters.AddWithValue("?inFilter", null);
				DataTable prices = new DataTable();
				e.DataAdapter.Fill(prices);
				_prices = new List<ulong>();
				foreach (DataRow row in prices.Rows) {
					ulong priceCode;
					if (ulong.TryParse(row["ID"].ToString(), out priceCode)) {
						_prices.Add(priceCode);
					}
				}

				if(_selfPrice > 0 && !_prices.Contains((ulong)_selfPrice))
					_prices.Add((ulong)_selfPrice);
			}
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
			e.DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS usersettings.TmpPricesRegions;
CREATE temporary table usersettings.TmpPricesRegions(
  PriceCode int(32) unsigned,
  RegionCode bigint unsigned
  ) engine=MEMORY;";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			e.DataAdapter.SelectCommand.Parameters.Clear();

			foreach (var price in _prices) {
				foreach (var region in _regions) {
					e.DataAdapter.SelectCommand.CommandText = @"
INSERT INTO usersettings.TmpPricesRegions(PriceCode, RegionCode) VALUES(?pricecode, ?regioncode);";
					e.DataAdapter.SelectCommand.Parameters.AddWithValue("?pricecode", price);
					e.DataAdapter.SelectCommand.Parameters.AddWithValue("?regioncode", region);
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
					e.DataAdapter.SelectCommand.Parameters.Clear();
				}
			}
		}

		private void GetRegions(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS usersettings.TmpRegions;
CREATE temporary table usersettings.TmpRegions(
  RegionCode bigint unsigned
  ) engine=MEMORY;";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(e.DataAdapter.SelectCommand);
			e.DataAdapter.SelectCommand.Parameters.Clear();
			foreach (var region in _regions) {
				e.DataAdapter.SelectCommand.CommandText = @"
INSERT INTO usersettings.TmpRegions(RegionCode) VALUES(?regioncode);";
				e.DataAdapter.SelectCommand.Parameters.AddWithValue("?regioncode", region);
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
				ProfileHelper.WriteLine(e.DataAdapter.SelectCommand);
				e.DataAdapter.SelectCommand.Parameters.Clear();
			}
		}

		public static string GetSuppliers(ExecuteArgs e)
		{
			var suppliers = new List<string>();

			e.DataAdapter.SelectCommand.CommandText = @"
select concat(supps.Name, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from Core cor
	join usersettings.PricesData pd on pd.PriceCode = cor.PriceCode
	join Customers.suppliers supps on supps.Id = pd.FirmCode
group by supps.Id
order by supps.Name";
			using (var reader = e.DataAdapter.SelectCommand.ExecuteReader()) {
				while (reader.Read())
					suppliers.Add(Convert.ToString(reader[0]));
			}
			return suppliers.Distinct().Implode();
		}

		public string GetIgnoredSuppliers(ExecuteArgs e)
		{
			if (!_reportParams.ContainsKey("IgnoredSuppliers"))
				return null;

			var supplierIds = (List<ulong>)_reportParams["IgnoredSuppliers"];
			if (supplierIds.Count == 0)
				return null;

			var suppliers = new List<string>();

			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select concat(supps.Name, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from usersettings.PricesData pd
	join Customers.suppliers supps on supps.Id = pd.FirmCode
where pd.PriceCode in ({0})
group by supps.Id
order by supps.Name", supplierIds.Implode());
			using (var reader = e.DataAdapter.SelectCommand.ExecuteReader()) {
				while (reader.Read())
					suppliers.Add(Convert.ToString(reader[0]));
			}
			return suppliers.Distinct().Implode();
		}

		public virtual List<Offer> GetOffers(int clientId, uint sourcePriceCode, uint? noiseSupplierId, bool allAssortment, bool byCatalog, bool withProducers)
		{
			_clientCode = Convert.ToInt32(clientId);

			InvokeGetActivePrices();

			var assortmentSupplierId = Convert.ToUInt32(
				MySqlHelper.ExecuteScalar(args.DataAdapter.SelectCommand.Connection,
					@"
select FirmCode
	from usersettings.pricesdata
where pricesdata.PriceCode = ?PriceCode
",
					new MySqlParameter("?PriceCode", sourcePriceCode)));
			//Заполняем код региона прайс-листа как домашний код региона клиента, относительно которого строится отчет
			var SourceRegionCode = Convert.ToUInt64(
				MySqlHelper.ExecuteScalar(args.DataAdapter.SelectCommand.Connection,
					@"
select RegionCode
	from Customers.Clients
where Id = ?ClientCode",
					new MySqlParameter("?ClientCode", _clientCode)));

			var enabledCost = MySqlHelper.ExecuteScalar(
				args.DataAdapter.SelectCommand.Connection,
				"select CostCode from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
				new MySqlParameter("?SourcePC", sourcePriceCode),
				new MySqlParameter("?SourceRegionCode", SourceRegionCode));
			if (enabledCost != null)
				MySqlHelper.ExecuteNonQuery(
					args.DataAdapter.SelectCommand.Connection,
					@"
drop temporary table IF EXISTS Usersettings.SourcePrice;
create temporary table Usersettings.SourcePrice engine=MEMORY
select * from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode;",
					new MySqlParameter("?SourcePC", sourcePriceCode),
					new MySqlParameter("?SourceRegionCode", SourceRegionCode));

			var joinText = allAssortment || sourcePriceCode == 0 ? " Left JOIN " : " JOIN ";

			string withWithoutPropertiesText;
			if (byCatalog)
				withWithoutPropertiesText = String.Format(@" if(C0.SynonymCode is not null, S.Synonym, {0}) ", GetCatalogProductNameSubquery("p.id"));
			else
				withWithoutPropertiesText = String.Format(@" if(C0.SynonymCode is not null, S.Synonym, {0}) ", GetProductNameSubquery("p.id"));

			var firmcr = withProducers ? " and ifnull(C0.CodeFirmCr,0) = ifnull(c00.CodeFirmCr,0) " : string.Empty;
			var producerId = withProducers ? " ifnull(c00.CodeFirmCr, 0) " : " 0 ";
			var producerName = withProducers ? " if(c0.SynonymFirmCrCode is not null, Sfc.Synonym , Prod.Name) " : " '-' ";

			var result = new List<Offer>();

			args.DataAdapter.SelectCommand.CommandText =
				string.Format(
					@"
select
	p.CatalogId,
	c00.ProductId,

	{0} as ProducerId,
	{1} as ProductName,
	{2} as ProducerName,

	c00.Id as CoreId,
	c00.Code,
	Prices.FirmCode as SupplierId,
	c00.PriceCode as PriceId,
	Prices.RegionCode as RegionId,
	c00.Quantity,
	if(if(round(cc.Cost * Prices.Upcost, 2) < c00.MinBoundCost, c00.MinBoundCost, round(cc.Cost * Prices.Upcost, 2)) > c00.MaxBoundCost,
	c00.MaxBoundCost, if(round(cc.Cost*Prices.UpCost,2) < c00.MinBoundCost, c00.MinBoundCost, round(cc.Cost * Prices.Upcost, 2))) as Cost,

	c0.Id as AssortmentCoreId,
	c0.Code as AssortmentCode,
	c0.CodeCr as AssortmentCodeCr,

	{9} as AssortmentSupplierId,
	c0.PriceCode as AssortmentPriceId,
	{10} as AssortmentRegionId,
	c0.Quantity as AssortmentQuantity,
	{7} as AssortmentCost
from
	Usersettings.ActivePrices Prices
	join farm.core0 c00 on c00.PriceCode = Prices.PriceCode
		join farm.CoreCosts cc on cc.Core_Id = c00.Id and cc.PC_CostCode = Prices.CostCode
	join catalogs.Products as p on p.id = c00.productid
	join Catalogs.Catalog as cg on p.catalogid = cg.id
	{3} farm.Core0 c0 on c0.productid = c00.productid {4} and C0.PriceCode = {5}
	{6}
	left join Catalogs.Producers Prod on c00.CodeFirmCr = Prod.Id
	left join farm.Synonym S on C0.SynonymCode = S.SynonymCode
	left join farm.SynonymFirmCr Sfc on C0.SynonymFirmCrCode = Sfc.SynonymFirmCrCode
	{8}
WHERE
  {11}
",
					producerId,
					withWithoutPropertiesText,
					producerName,
					joinText,
					firmcr,
					sourcePriceCode,
					(enabledCost != null)
						? @"
left join farm.CoreCosts cc0 on cc0.Core_Id = c0.Id and cc0.PC_CostCode = " + enabledCost + @"
left join Usersettings.SourcePrice c0Prices on c0Prices.CostCode = " + enabledCost
						: "",
					(enabledCost != null)
						? @"
if(cc0.Cost is null, 0,
if(if(round(cc0.Cost * c0Prices.Upcost, 2) < c0.MinBoundCost, c0.MinBoundCost, round(cc0.Cost * c0Prices.Upcost, 2)) > c0.MaxBoundCost,
	c0.MaxBoundCost, if(round(cc0.Cost*c0Prices.UpCost,2) < c0.MinBoundCost, c0.MinBoundCost, round(cc0.Cost * c0Prices.Upcost, 2)))
)"
						: " null ",
					@"",
					assortmentSupplierId,
					SourceRegionCode,
					sourcePriceCode == 0
						? " c00.Junk = 0 "
						: @"
	({1} (c0.PriceCode <> c00.PriceCode) or (Prices.RegionCode <> {0}) or (c0.Id = c00.Id))
and (c00.Junk = 0 or c0.Id = c00.Id)".Format(SourceRegionCode, allAssortment || sourcePriceCode == 0 ? "(c0.PriceCode is null) or" : string.Empty));

			Random random = null;
			if (noiseSupplierId.HasValue)
				random = new Random();

#if DEBUG
			Debug.WriteLine(args.DataAdapter.SelectCommand.CommandText);
#endif

			using (var reader = args.DataAdapter.SelectCommand.ExecuteReader()) {
				foreach (var row in reader.Cast<IDataRecord>()) {
					var offer = new Offer(row, noiseSupplierId, random);
					result.Add(offer);
				}
			}

			return result;
		}

		protected string GetSupplierName(int priceId)
		{
			string customerFirmName;
			var drPrice = MySqlHelper.ExecuteDataset(
				_conn,
				@"
select
  concat(suppliers.Name, '(', pricesdata.PriceName, ') - ', regions.Region) as FirmName,
  pricesdata.PriceCode,
  suppliers.HomeRegion
from
  usersettings.pricesdata,
  Customers.suppliers,
  farm.regions
where
	pricesdata.PriceCode = ?PriceCode
and suppliers.Id = pricesdata.FirmCode
and regions.RegionCode = suppliers.HomeRegion
limit 1", new MySqlParameter("?PriceCode", priceId))
				.Tables[0].AsEnumerable().FirstOrDefault();
			if (drPrice != null) {
				customerFirmName = drPrice["FirmName"].ToString();
			}
			else
				throw new ReportException(String.Format("Не найден прайс-лист с кодом {0}.", priceId));
			return customerFirmName;
		}


		protected void GetWeightCostOffers(ExecuteArgs e, int? noise = null, int? userId = null)
		{
			GetRegions(e);
			e.DataAdapter.SelectCommand.CommandText = "drop temporary table IF EXISTS Prices, ActivePrices, Core, MinCosts";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			selectCommand.Parameters.AddWithValue("?NoiseFirmCode", noise);
			selectCommand.Parameters.AddWithValue("?runDate", GetStatOffersDate());
			selectCommand.CommandText = "Customers.GetOffersReportsWeighted";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.ExecuteNonQuery();
			ProfileHelper.WriteLine(selectCommand);

			// Накладываем фильтры
			// В поле PriceCode храним идентификатор поставщика
			List<ulong> allowedFirms = null;
			if (_reportParams.ContainsKey("FirmCodeEqual"))
				allowedFirms = (List<ulong>)_reportParams["FirmCodeEqual"];
			if (allowedFirms != null && allowedFirms.Count > 0) {
				e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
				e.DataAdapter.SelectCommand.CommandText = String.Format("delete from usersettings.Core where PriceCode not in ({0})", allowedFirms.Implode());
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
				ProfileHelper.WriteLine(selectCommand);
			}

			if (_reportParams.ContainsKey("IgnoredSuppliers")) {
				var suppliers = (List<ulong>)_reportParams["IgnoredSuppliers"];
				if (suppliers != null && suppliers.Count > 0) {
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from usersettings.Core where PriceCode in ({0})", suppliers.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
					ProfileHelper.WriteLine(selectCommand);
				}
			}
		}

		protected DateTime GetStatOffersDate()
		{
			if (Interval) {
				return From;
			}
			return DateTime.Today.AddDays(-1);
		}
	}
}
