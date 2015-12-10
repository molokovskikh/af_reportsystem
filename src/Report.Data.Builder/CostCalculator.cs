using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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

	public struct AggregateId
	{
		public uint ProductId;
		public uint ProducerId;

		public AggregateId(uint productId, uint producerId)
		{
			ProductId = productId;
			ProducerId = producerId;
		}
	}

	public class OfferAggregates
	{
		private Dictionary<uint, uint> quantityPerPrice = new Dictionary<uint, uint>();
		private decimal lastCost;
		private uint offerCount;
		private uint? lastClientForRatingId;
		private decimal sumClientRating;
		private uint? lastClientId;

		public decimal Cost;
		public uint Quantity;

		public OfferAggregates(uint client)
		{
			lastClientForRatingId = null;
			lastClientId = client;
		}

		public void Collect(Offer offer, decimal regionRating, uint client)
		{
			if (lastClientId != client) {
				CalculateQuantity();

				if (offerCount > 0)
					Cost += lastCost / offerCount;

				lastClientId = client;
				lastCost = 0;
				offerCount = 0;
			}

			//ученка не влияет на индекс цен но учитывается в остатках
			if (!offer.Junk) {
				lastCost = lastCost + offer.Cost * regionRating;
				offerCount++;
				if (lastClientForRatingId != client) {
					sumClientRating += regionRating;
					lastClientForRatingId = client;
				}
			}

			if (quantityPerPrice.ContainsKey(offer.PriceId)) {
				quantityPerPrice[offer.PriceId] += offer.Quantity;
			} else {
				quantityPerPrice.Add(offer.PriceId, offer.Quantity);
			}
		}

		public void Calculate()
		{
			CalculateQuantity();

			if (lastClientId != null && offerCount > 0)
				Cost += lastCost / offerCount;

			if (sumClientRating < 1 && sumClientRating > 0)
				Cost *= 1 / sumClientRating;
		}

		private void CalculateQuantity()
		{
			if (quantityPerPrice.Count == 0)
				return;

			var max = quantityPerPrice.Select(p => p.Value).DefaultIfEmpty().Max();
			if (max > Quantity)
				Quantity = max;

			quantityPerPrice.Clear();
		}

		public override string ToString()
		{
			return $"Cost = {Cost}, Rating = {sumClientRating}, LastCost = {lastCost}, OfferCount = {offerCount}";
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
		public uint PriceId;

		public Offer(OfferId id, uint productId, uint producerId, decimal cost, bool junk, uint quantity = 0, ulong coreId = 0, string code = "", string codeCr = "", uint priceId = 0)
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
			PriceId = priceId;
		}
	}

	public class CostCalculator
	{
		private ILog log = LogManager.GetLogger(typeof(CostCalculator));
		private CancellationToken token;

		public uint DebugProductId;
		public uint DebugProducerId;
		public uint DebugSupplierId;
		public decimal CostThreshold;

		public CostCalculator()
		{
			token = new CancellationToken();
		}

		public CostCalculator(CancellationToken token)
		{
			this.token = token;
		}

		public IEnumerable<uint> Clients()
		{
			var sql = @"
select c.Id
from Customers.Clients c
join Usersettings.RetClientsSet rcs on rcs.ClientCode = c.Id
where rcs.InvisibleOnFirm = 0 and rcs.ServiceClient = 0
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

delete p from Usersettings.Prices p
	join Usersettings.PricesData pd on pd.PriceCode = p.PriceCode
where pd.IsLocal = 1;

select c0.ProductId, c0.CodeFirmCr, p.RegionCode, p.FirmCode, {0} as Cost, c0.Quantity, c0.Junk, c0.Id as CoreId, c0.Code, c0.CodeCr, c0.PriceCode
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
				token.ThrowIfCancellationRequested();
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
					var aggregateId = new AggregateId(offer.ProductId, offer.ProducerId);
					var aggregates = (OfferAggregates)costs[aggregateId];
					if (aggregates == null) {
						aggregates = new OfferAggregates(client);
						costs[aggregateId] = aggregates;
					}

					if (!rating.ContainsKey(offer.Id.RegionId))
						continue;

					//если цена слишком большая значит это какая то лажа и ее нужно игнорировать
					if (CostThreshold > 0 && offer.Cost > CostThreshold)
						continue;

					var regionRating = rating[offer.Id.RegionId];
					aggregates.Collect(offer, regionRating, client);

#if DEBUG
					if (offer.ProductId == DebugProductId && offer.ProducerId == DebugProducerId && offer.Id.SupplierId == DebugSupplierId)
						Console.WriteLine("Average cost = {0}, cost = {3}, client = {1}, ratings = {2}", costs[aggregateId], client, regionRating, offer.Cost);
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
				foreach (AggregateId assortmentId in costs.Keys) {
					var aggregates = (OfferAggregates)costs[assortmentId];
					aggregates.Calculate();
#if DEBUG
					if (assortmentId.ProductId == DebugProductId && assortmentId.ProducerId == DebugProducerId && key.SupplierId == DebugSupplierId)
						Console.WriteLine("Average cost = {0}", aggregates);
#endif
				}
			}
			if (log.IsDebugEnabled)
				log.DebugFormat("Закончил нормировку цен по рейтингу");
			return result;
		}

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
					foreach (AggregateId aggregateId in costs.Keys) {
						token.ThrowIfCancellationRequested();
						var aggregates = (OfferAggregates)costs[aggregateId];
						if (aggregates.Cost == 0)
							continue;
						totalCount++;
						if (sql.Length > header.Length)
							sql.Append(", ");
						sql.AppendFormat("('{0}', {1}, {2}, {3}, {4}, {5}, {6})",
							date.ToString(MySqlConsts.MySQLDateFormat),
							key.SupplierId,
							key.RegionId,
							aggregateId.ProductId,
							aggregateId.ProducerId,
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
			return TaskLoader.ParallelLoader(ratings.GroupBy(r => r.ClientId).ToList(), g => GetOffers(g.Key), count)
				.Select(t => Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					t.Item1.ToArray(),
					t.Item2));
		}
	}
}