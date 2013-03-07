﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.MySql;
using Common.Tools;
using Common.Tools.Threading;
using MySql.Data.MySqlClient;
using log4net;

namespace Report.Data.Builder
{
	public struct OfferId
	{
		public uint SupplierId;
		public ulong RegionId;

		public OfferId(uint supplierId, ulong regionId)
		{
			SupplierId = supplierId;
			RegionId = regionId;
		}
	}

	public class OfferAggregates
	{
		public decimal Cost;
		public uint Quantity;
		public List<ulong> UsedCores;
		public List<string> UsedCodes;
		public uint? LastClientId;
		public decimal LastCost;
		public uint Count;
		public uint? LastClientForRatingId;
		public decimal SumClientRating;

		public OfferAggregates()
		{
			UsedCores = new List<ulong>();
			UsedCodes = new List<string>();
			LastClientForRatingId = null;
			LastClientId = null;
		}
	}

	public class ClientRating
	{
		public uint ClientId;
		public ulong RegionId;
		public decimal Value;

		public ClientRating(uint clientId, ulong regionId, decimal value)
		{
			ClientId = clientId;
			RegionId = regionId;
			Value = value;
		}

		public override bool Equals(object obj)
		{
			var rating = obj as ClientRating;
			if (rating == null)
				return false;
			return rating.ClientId == ClientId
				&& rating.RegionId == RegionId
				&& rating.Value == Value;
		}
	}

	public class Offer
	{
		public OfferId Id;

		public uint ProductId;
		public uint ProducerId;
		public decimal Cost;
		public uint Quantity;
		public bool Junk;
		public ulong CoreId;
		public string Code;
		public string CodeCr;
		public uint PriceCode;

		public Offer(OfferId id, uint productId, uint producerId, decimal cost, bool junk, uint quantity = 0, ulong coreId = 0, string code = "", string codeCr = "", uint priceCode = 0)
		{
			Id = id;
			ProductId = productId;
			ProducerId = producerId;
			Cost = cost;
			Junk = junk;
			Quantity = quantity;
			CoreId = coreId;
			Code = code ?? "";
			CodeCr = codeCr ?? "";
			PriceCode = priceCode;
		}
	}

	public class CostCalculator
	{
		private ILog log = LogManager.GetLogger(typeof(CostCalculator));

		public IEnumerable<uint> Clients()
		{
			var sql = @"
select c.Id
from Customers.Clients c
join Usersettings.RetClientsSet rcs on rcs.ClientCode = c.Id
where rcs.InvisibleOnFirm = 0
";
			return Db.Read<uint>(sql);
		}

		public Offer[] GetOffers(uint client)
		{
			if (log.IsDebugEnabled)
				log.DebugFormat("Начал загрузку предложений для клиента {0}", client);
			var sql = String.Format(@"
set @UserId = (select Id
from Customers.Users
where ClientId = ?client
limit 1);

call Customers.GetPrices(@UserId);

update Usersettings.prices p, usersettings.pricescosts pc, usersettings.pricesregionaldata prd
 set p.costcode = ifnull(prd.BaseCost, pc.CostCode)
 where p.costtype=1
 and p.pricecode = pc.pricecode
 and pc.BaseCost = 1
 and p.pricecode = prd.pricecode
 and prd.RegionCode = p.RegionCode
 and prd.enabled = 1;

select straight_join c0.ProductId, c0.CodeFirmCr, p.RegionCode, p.FirmCode, {0} as Cost, c0.Quantity, c0.Junk, c0.Id as CoreId, c0.Code, c0.CodeCr, c0.PriceCode
from Usersettings.Prices p
	join farm.core0 c0 on c0.PriceCode = p.PriceCode
		join farm.CoreCosts cc on cc.Core_Id = c0.Id and cc.PC_CostCode = p.CostCode
	join Catalogs.Products p on p.Id = c0.ProductId
	join Catalogs.Catalog c on c.Id = p.CatalogId
	join Catalogs.Assortment a on a.CatalogId = c.Id and a.ProducerId = c0.CodeFirmCr
where p.Actual = 1
;", QueryParts.CostSubQuery("c0", "cc", "p"));
			var watch = Stopwatch.StartNew();
			watch.Start();

			var data = Db.Read(sql,
				r => new Offer(new OfferId(r.GetUInt32("FirmCode"),
					r.GetUInt64("RegionCode")),
					r.GetUInt32("ProductId"),
					r.GetUInt32("CodeFirmCr"),
					r.GetDecimal("Cost"),
					r.GetBoolean("Junk"),
					SafeConvert.ToUInt32(r["Quantity"].ToString()),
					r.GetUInt64("CoreId"),
					r.GetString("Code"),
					r.GetString("CodeCr"),
					r.GetUInt32("PriceCode")),
				new { client })
				.ToArray();

			watch.Stop();
			if (log.IsDebugEnabled)
				log.DebugFormat("Загрузка предложений для клиента {0} заняла {1}с", client, watch.Elapsed.TotalSeconds);

			return data;
		}

		public Hashtable Calculate(IEnumerable<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> data)
		{
			var result = new Hashtable();
			var watch = new Stopwatch();
			watch.Start();

			foreach (var item in data) {
				watch.Stop();

				if (item.Item1.Count() == 0)
					continue;

				if (log.IsDebugEnabled)
					log.DebugFormat("Ожидание данных {0}с", watch.Elapsed.TotalSeconds);

				var client = item.Item1.First().ClientId;
				if (log.IsDebugEnabled)
					log.DebugFormat("Начал вычисление средних цен для клиента {0}", client);
				var rating = item.Item1.ToDictionary(r => r.RegionId, r => r.Value);

				foreach (var offer in item.Item2) {
					var costs = (Hashtable)result[offer.Id];
					if (costs == null) {
						costs = new Hashtable();
						result[offer.Id] = costs;
					}
					var costsKey = offer.ProductId + "|" + offer.ProducerId;
					var aggregates = (OfferAggregates)costs[costsKey];
					if (aggregates == null) {
						aggregates = new OfferAggregates();
						aggregates.LastClientId = client;
						costs[costsKey] = aggregates;
					}
					else if(aggregates.LastClientId != client && aggregates.Count > 0) {
						aggregates.Cost += aggregates.LastCost / aggregates.Count;
						aggregates.LastClientId = client;
						aggregates.LastCost = 0;
						aggregates.Count = 0;
					}

					if (!rating.ContainsKey(offer.Id.RegionId))
						continue;

					//если цена слишком большая значит это какая то лажа и ее нужно игнорировать
					if (CostThreshold > 0 && offer.Cost > CostThreshold)
						continue;

					var regionRating = rating[offer.Id.RegionId];
					if (!offer.Junk) {
						aggregates.LastCost = aggregates.LastCost + offer.Cost * regionRating;
						aggregates.Count++;
						if(aggregates.LastClientForRatingId != client) {
							aggregates.SumClientRating += regionRating;
							aggregates.LastClientForRatingId = client;
						}
					}
					// проверяем, что этот core мы еще не использовали при подсчете количества
					if(!aggregates.UsedCores.Contains(offer.CoreId)) {
						if(aggregates.UsedCodes.Any(s => s.Contains(offer.Code + "|" + offer.CodeCr))) {
							if(aggregates.UsedCodes.Any(s => s.Contains(offer.PriceCode.ToString() + "|" + offer.Code + "|" + offer.CodeCr))) {
								aggregates.Quantity += offer.Quantity;
							}
						}
						else {
							aggregates.UsedCodes.Add(offer.PriceCode.ToString() + "|" + offer.Code + "|" + offer.CodeCr);
							aggregates.Quantity += offer.Quantity;
						}
						
						aggregates.UsedCores.Add(offer.CoreId);
					}
#if DEBUG
					if (offer.ProductId == DebugProductId && offer.ProducerId == DebugProducerId && offer.Id.SupplierId == DebugSupplierId)
						Console.WriteLine("Average cost = {0}, cost = {3}, client = {1}, ratings = {2}", costs[costsKey], client, regionRating, offer.Cost);
#endif
				}
				if (log.IsDebugEnabled)
					log.DebugFormat("Закончил вычисление средних цен для клиента {0}", client);

				watch.Reset();
				watch.Start();
			}
			if (log.IsDebugEnabled)
				log.DebugFormat("Начал нормировку цен по рейтингу");
			foreach (OfferId key in result.Keys) {
				var costs = ((Hashtable)result[key]);
				foreach (string assortmentId in costs.Keys) {
					var aggregates = (OfferAggregates)costs[assortmentId];
					if(aggregates.LastClientId != null
						&& aggregates.Count > 0)
						aggregates.Cost += aggregates.LastCost / aggregates.Count;

					if(aggregates.SumClientRating < 1 && aggregates.SumClientRating > 0)
						aggregates.Cost *= 1 / aggregates.SumClientRating;
				}
			}
			if (log.IsDebugEnabled)
				log.DebugFormat("Закончил нормировку цен по рейтингу");
			return result;
		}

		public uint DebugProductId;
		public uint DebugProducerId;
		public uint DebugSupplierId;
		public decimal CostThreshold;

		public int Save(DateTime date, Hashtable hash)
		{
			var header = "insert into Reports.AverageCosts(Date, SupplierId, RegionId, ProductId, ProducerId, Cost, Quantity) values ";
			var page = 100;
			var totalCount = 0;
			With.Transaction(t => {
				var sql = new StringBuilder();
				sql.Append(header);
				var command = new MySqlCommand("", t.Connection);
				var index = 0;
				foreach (OfferId key in hash.Keys) {
					var costs = ((Hashtable)hash[key]);
					foreach (string costKey in costs.Keys) {
						var aggregates = (OfferAggregates)costs[costKey];
						if (aggregates.Cost == 0)
							continue;
						totalCount++;
						if (sql.Length > header.Length)
							sql.Append(", ");
						var separatorIndex = costKey.IndexOf("|");
						sql.AppendFormat("('{0}', {1}, {2}, {3}, {4}, {5}, {6})",
							date.ToString(MySqlConsts.MySQLDateFormat),
							key.SupplierId,
							key.RegionId,
							costKey.Substring(0, separatorIndex),
							costKey.Substring(separatorIndex + 1, costKey.Length - separatorIndex - 1),
							aggregates.Cost.ToString(CultureInfo.InvariantCulture),
							(aggregates.Quantity == 0 ? "null" : aggregates.Quantity.ToString()));

						index++;
						if (index >= page) {
							Apply(header, sql, command);
							index = 0;
						}
					}
				}

				if (sql.Length > header.Length)
					Apply(header, sql, command);
			});
			return totalCount;
		}

		private static void Apply(string header, StringBuilder sql, MySqlCommand command)
		{
			command.CommandText = sql.ToString();
			command.ExecuteNonQuery();
			sql.Clear();
			sql.Append(header);
		}

		public IEnumerable<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> Offers(IEnumerable<ClientRating> ratings, int count)
		{
			var clients = ratings.Select(r => r.ClientId).Distinct().ToList();
			return TaskLoader.ParallelLoader(clients, GetOffers, count)
				.Select(t => Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					ratings.Where(r => r.ClientId == t.Item1).ToArray(),
					t.Item2));
		}
	}
}