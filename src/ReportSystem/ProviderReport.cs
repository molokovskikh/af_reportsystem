using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Common.Tools;
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
		protected int? _SupplierNoise = null;
		//protected bool IsNewClient = false;
		protected int? _userCode = null;

		public ProviderReport(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, temporary, format, dsProperties)
		{ 
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			/*e.DataAdapter.SelectCommand.CommandText = "select * from future.Clients where Id = " + _clientCode;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			IsNewClient = reader.Read();
			reader.Close();*/
		}

		public override void ReadReportParams()
		{
			if (_reportParams.ContainsKey("SupplierNoise"))
				_SupplierNoise = (int)getReportParam("SupplierNoise");
		}

		public virtual List<ulong> GetClietnWithSetFilter(List<ulong> RegionEqual, List<ulong> RegionNonEqual,
														List<ulong> PayerEqual, List<ulong> PayerNonEqual,
														List<ulong> Clients, List<ulong> ClientsNON, ExecuteArgs e)
		{
			var regionalWhere = "(";
			if (RegionEqual.Count != 0)
			{
				foreach (var region in RegionEqual)
				{
					regionalWhere += string.Format(" (fc.MaskRegion & {0}) = {0} OR " , region);
				}
			}
			if (RegionNonEqual.Count != 0)
			{
				foreach (var region in RegionNonEqual)
				{
					regionalWhere += string.Format(" (fc.MaskRegion & {0}) != {0} OR " , region);
				}
			}
			if (regionalWhere.Length != 1)
			{
				regionalWhere = regionalWhere.Substring(0, regionalWhere.Length - 3);
				regionalWhere = " AND " + regionalWhere;
				regionalWhere += ")";
			}
			else
			{
				regionalWhere = string.Empty;
			}
			var payerWhere = string.Empty;
			if (PayerEqual.Count != 0)
			{				
				payerWhere += " AND pc.PayerId IN " + ConcatWhereIn(PayerEqual);
			}
			if (PayerNonEqual.Count !=0)
			{				
				payerWhere += " AND pc.PayerId NOT IN " + ConcatWhereIn(PayerNonEqual);
			}
			var clientWhere = string.Empty;
			if (Clients.Count != 0)
			{
				clientWhere += " AND fc.Id IN " + ConcatWhereIn(Clients);
			}
			if (ClientsNON.Count != 0)
			{
				clientWhere += " AND fc.Id NOT IN " + ConcatWhereIn(ClientsNON);
			}
			var where = string.Empty;
			if ((regionalWhere != string.Empty) || (payerWhere != string.Empty) || (clientWhere != string.Empty))
			where = regionalWhere + payerWhere + clientWhere;
			e.DataAdapter.SelectCommand.CommandText = 
			string.Format(@"SELECT distinct fc.Id FROM future.Clients fc
							join billing.PayerClients pc on fc.Id = pc.ClientId
							join usersettings.RetClientsSet RCS on fc.id = RCS.ClientCode
							WHERE RCS.ServiceClient = 0 and RCS.InvisibleOnFirm = 0 and fc.Status = 1 {0}", where);

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			var result = new List<ulong>();
			while (reader.Read())
			{
				result.Add(Convert.ToUInt64(reader["Id"].ToString()));
			}
			reader.Close();
			return result;
		}

		public virtual void NoisingCostInDataTable(DataTable data, string costFieldName, string supplierFieldName , int? supplier)
		{
			if (supplier != null)
			{
				var rand = new Random();
				foreach (DataRow row in data.Rows)
				{
					if (row.Field<uint?>(supplierFieldName) != supplier)
					{
						var randObj = (decimal)rand.NextDouble();
						row[costFieldName] = (1 + (randObj * (randObj > (decimal)0.5 ? 2 : -2)) / 100) * row.Field<decimal>(costFieldName);
					}
				}
			}
		}


		/// <summary>
		/// Метод по списку ID формарует строку для вставки в запрос вида: where t.item in (id1, id2, id3...)
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public static string ConcatWhereIn(List<ulong> items)
		{
			var result = "(";
			foreach (var item in items)
			{
				result += (item + ", ");
			}
			result = result.Substring(0, result.Length - 2);
			result += ")";
			return result;
		}

		protected void GetActivePrices()
		{
			GetActivePrices(args);
		}

		protected void GetOffers()
		{
			GetOffers(args, null);
		}

		//Получили список действующих прайс-листов для интересующего клиента
		protected void GetActivePrices(ExecuteArgs e)
		{
			//удаление временных таблиц
			e.DataAdapter.SelectCommand.CommandText = "drop temporary table IF EXISTS Prices, ActivePrices, Core, MinCosts";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			//if(IsNewClient)
				GetActivePricesNew();
			//else
				//GetActivePricesOld();

			List<ulong> allowedFirms = null;
			if (_reportParams.ContainsKey("FirmCodeEqual"))
				allowedFirms = (List<ulong>)_reportParams["FirmCodeEqual"];
			if(allowedFirms != null && allowedFirms.Count > 0)
			{
				e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
				e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where FirmCode not in ({0})", allowedFirms.Implode());
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
			}

			if (_reportParams.ContainsKey("IgnoredSuppliers"))
			{
				var suppliers = (List<ulong>)_reportParams["IgnoredSuppliers"];
				if (suppliers != null && suppliers.Count > 0)
				{
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where FirmCode in ({0})", suppliers.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			if (_reportParams.ContainsKey("PriceCodeValues"))
			{
				var PriceCodeValues = (List<ulong>)_reportParams["PriceCodeValues"];
				if (PriceCodeValues != null && PriceCodeValues.Count > 0)
				{
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where PriceCode not in ({0})", PriceCodeValues.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			if (_reportParams.ContainsKey("PriceCodeNonValues"))
			{
				var PriceCodeNonValues = (List<ulong>)_reportParams["PriceCodeNonValues"];
				if (PriceCodeNonValues != null && PriceCodeNonValues.Count > 0)
				{
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where PriceCode in ({0})", PriceCodeNonValues.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			//Добавляем в таблицу ActivePrices поле FirmName и заполняем его также, как раньше для отчетов
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
/*			e.DataAdapter.SelectCommand.CommandText = @"
alter table ActivePrices add column FirmName varchar(100);
update 
  ActivePrices, usersettings.clientsdata, farm.regions 
set 
  FirmName = concat(clientsdata.ShortName, '(', ActivePrices.PriceName, ') - ', regions.Region)
where 
    activeprices.FirmCode = clientsdata.FirmCode 
and regions.RegionCode = activeprices.RegionCode";*/

		    e.DataAdapter.SelectCommand.CommandText = @"
alter table ActivePrices add column FirmName varchar(100);
update 
  ActivePrices, future.suppliers, farm.regions 
set 
  FirmName = concat(suppliers.Name, '(', ActivePrices.PriceName, ') - ', regions.Region)
where 
    activeprices.FirmCode = suppliers.Id 
and regions.RegionCode = activeprices.RegionCode";

			e.DataAdapter.SelectCommand.ExecuteNonQuery();

		}

		protected void GetActivePricesNew()
		{// Небольшая магия, через любого пользователя получаем прайсы клиента

			// Получаем пользователя
			var userId = GetUserId();

			// Получаем для него все прайсы
			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.CommandText = "future.GetPrices";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			selectCommand.ExecuteNonQuery();

			// Включаем для него все прайсы
			selectCommand.CommandType = CommandType.Text;
			selectCommand.CommandText = "update Prices set DisabledByClient = 0";
			selectCommand.ExecuteNonQuery();

			// Получаем для пользователя активные (которыми теперь являются все) прайсы
			selectCommand.CommandText = "future.GetActivePrices";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			selectCommand.ExecuteNonQuery();
		}

		private uint GetUserId()
		{
			// Если пользователь не передан в качестве параметра - берем первого попавшегося
			if (_userCode == null)
			{
				args.DataAdapter.SelectCommand.CommandText = "select Id from future.Users where ClientId = " + _clientCode +
				                                             " limit 1";
				return Convert.ToUInt32(args.DataAdapter.SelectCommand.ExecuteScalar());
			}
			else
			{
				return Convert.ToUInt32(_userCode);
			}
		}

		/*private void GetActivePricesOld()
		{
			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.CommandText = "usersettings.GetActivePrices";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?ClientCodeParam", _clientCode);
			selectCommand.ExecuteNonQuery();
		}*/

		//Получили список предложений для интересующего клиента
		protected void ExecuterGetOffers(ExecuteArgs e, int? noiseFirmCode)
		{
			GetActivePrices(e);

			//if(IsNewClient)
				GetOffersNew(noiseFirmCode);
			//else
				//GetOffersOld();

			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
		}

		protected void GetOffers(ExecuteArgs e)
		{
			ExecuterGetOffers(e, null);
		}

		protected void GetOffers(ExecuteArgs e, int? noiseFirmCode)
		{
			ExecuterGetOffers(e, noiseFirmCode);
		}

		private void GetOffersNew(int? noiseFirmCode)
		{ // Небольшая магия, через любого пользователя получаем предложение для клиента

			// Получаем первого попавшегося пользователя
			var userId = GetUserId();

			//Проверка существования и отключения клиента
			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.CommandText =
				"select * from future.Clients cl where cl.Id = " + _clientCode;
			using (var reader = selectCommand.ExecuteReader())
			{
				if (!reader.Read())
					throw new ReportException(String.Format("Невозможно найти клиента с кодом {0}.", _clientCode));
				if (Convert.ToByte(reader["Status"]) == 0)
					throw new ReportException(String.Format("Невозможно сформировать отчет по отключенному клиенту {0} ({1}).", reader["Name"], _clientCode));
			}

			selectCommand.CommandText = "future.GetOffersReports";
			selectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			selectCommand.Parameters.AddWithValue("?NoiseFirmCode", noiseFirmCode);
			selectCommand.ExecuteNonQuery();
		}

		/*protected void GetOffersOld()
		{
			//Проверка существования и отключения клиента
			DataRow drClient = MySqlHelper.ExecuteDataRow(
				ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
				"select FirmCode, FirmStatus, ShortName from usersettings.clientsdata cd where cd.FirmCode = ?FirmCode",
				new MySqlParameter("?FirmCode", _clientCode));
			if (drClient == null)
				throw new ReportException(String.Format("Невозможно найти клиента с кодом {0}.", _clientCode));
			else
				if (Convert.ToByte(drClient["FirmStatus"]) == 0)
					throw new ReportException(String.Format("Невозможно сформировать отчет по отключенному клиенту {0} ({1}).", drClient["ShortName"], _clientCode));

			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.CommandText = "usersettings.GetOffers";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?ClientCodeParam", _clientCode);
			selectCommand.Parameters.AddWithValue("?FreshOnly", 0);
			selectCommand.ExecuteNonQuery();
		}*/

		public static string GetSuppliers(ExecuteArgs e)
		{
			var suppliers = new List<string>();
			/*e.DataAdapter.SelectCommand.CommandText = @"
select concat(cd.ShortName, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from Core cor
	join usersettings.PricesData pd on pd.PriceCode = cor.PriceCode
	join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
group by cd.FirmCode
order by cd.ShortName";*/
            e.DataAdapter.SelectCommand.CommandText = @"
select concat(supps.Name, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from Core cor
	join usersettings.PricesData pd on pd.PriceCode = cor.PriceCode
    join future.suppliers supps on supps.Id = pd.FirmCode
group by supps.Id
order by supps.Name";
			using(var reader = e.DataAdapter.SelectCommand.ExecuteReader())
			{
				while(reader.Read())
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
			/*e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select concat(cd.ShortName, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from usersettings.PricesData pd
	join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
where pd.PriceCode in ({0})
group by cd.FirmCode
order by cd.ShortName", supplierIds.Implode());*/
            e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select concat(supps.Name, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from usersettings.PricesData pd
    join future.suppliers supps on supps.Id = pd.FirmCode
where pd.PriceCode in ({0})
group by supps.Id
order by supps.Name", supplierIds.Implode());
			using(var reader = e.DataAdapter.SelectCommand.ExecuteReader())
			{
				while(reader.Read())
					suppliers.Add(Convert.ToString(reader[0]));
			}
			return suppliers.Distinct().Implode();
		}

		public List<Offer> GetOffers(int clientId, uint sourcePriceCode, uint? noiseSupplierId, bool allAssortment, bool byCatalog, bool withProducers)
		{
			_clientCode = Convert.ToInt32(clientId);

			/*args.DataAdapter.SelectCommand.CommandText = "select * from future.Clients where Id = " + _clientCode;
			using (var reader = args.DataAdapter.SelectCommand.ExecuteReader())
			{
				IsNewClient = reader.Read();
			}*/

			GetActivePrices(args);

			var assortmentSupplierId = Convert.ToUInt32(
				MySqlHelper.ExecuteScalar(args.DataAdapter.SelectCommand.Connection,
					@"
select FirmCode 
	from usersettings.pricesdata 
where pricesdata.PriceCode = ?PriceCode
",
					new MySqlParameter("?PriceCode", sourcePriceCode)));
			//Заполняем код региона прайс-листа как домашний код региона клиента, относительно которого строится отчет
			/*var SourceRegionCode = Convert.ToUInt64(
				MySqlHelper.ExecuteScalar(args.DataAdapter.SelectCommand.Connection,
					@"select RegionCode 
	from usersettings.clientsdata 
where FirmCode = ?ClientCode
and not exists(select 1 from future.Clients where Id = ?ClientCode)
union
select RegionCode
	from future.Clients
where Id = ?ClientCode",
					new MySqlParameter("?ClientCode", _clientCode)));*/
            var SourceRegionCode = Convert.ToUInt64(
                MySqlHelper.ExecuteScalar(args.DataAdapter.SelectCommand.Connection,
                    @"
select RegionCode
	from future.Clients
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
"
					, 
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



//            GetOffers();

//            args.DataAdapter.SelectCommand.CommandText =
//                @"
//select 
//	p.CatalogId,
//	c.ProductId,
//	ifnull(c.CodeFirmCr, 0) as ProducerId,
//	s.Synonym as ProductName,
//	sfc.Synonym as ProducerName,
//
//	c.Id as CoreId,
//	c.Code,
//	ap.FirmCode as SupplierId,
//	c.PriceCode as PriceId,
//	ap.RegionCode as RegionId,
//	c.Quantity,
//	Core.Cost,
//
//	null as AssortmentCoreId,
//	null as AssortmentCode,
//	null as AssortmentSupplierId,
//	null as AssortmentPriceId,
//	null as AssortmentRegionId,
//	null as Quantity,
//	null as AssortmentCost
//from
//	Core
//	inner join ActivePrices ap on ap.PriceCode = Core.PriceCode and ap.RegionCode = ap.RegionCode
//	inner join farm.Core0 c on c.Id = Core.Id
//	inner join catalogs.Products p on p.Id = c.ProductId
//	left join farm.Synonym s on s.SynonymCode = c.SynonymCode
//	left join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c.SynonymFirmCrCode
//";
			Random random = null;
			if (noiseSupplierId.HasValue)
				random = new Random();

			using (var reader = args.DataAdapter.SelectCommand.ExecuteReader())
			{
				foreach (var row in reader.Cast<IDataRecord>())
				{
					var offer = new Offer(row, noiseSupplierId, random);
					result.Add(offer);
				}

			}

			return result;
		}
	}
}
