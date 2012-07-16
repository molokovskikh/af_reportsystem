using System;
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
		public uint Count;

		public uint AvgQuantity
		{
			get
			{
				if (Count == 0)
					return 0;
				return Quantity/Count;
			}
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

		public uint AssortmentId;
		public decimal Cost;
		public uint Quantity;

		public Offer(OfferId id, uint assortmentId, decimal cost, uint quantity = 0)
		{
			Id = id;
			AssortmentId = assortmentId;
			Cost = cost;
			Quantity = quantity;
		}
	}

	public class CostCalculator
	{
		private ILog log = LogManager.GetLogger(typeof (CostCalculator));

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
			var sql = String.Format(@"
set @UserId = (select Id
from Customers.Users
where ClientId = ?client
limit 1);

call Customers.GetPrices(@UserId);

select straight_join a.Id, p.RegionCode, p.FirmCode, {0} as Cost, c0.Quantity
from Usersettings.Prices p
	join farm.core0 c0 on c0.PriceCode = p.PriceCode
		join farm.CoreCosts cc on cc.Core_Id = c0.Id and cc.PC_CostCode = p.CostCode
	join Catalogs.Products p on p.Id = c0.ProductId
	join Catalogs.Catalog c on c.Id = p.CatalogId
	join Catalogs.Assortment a on a.CatalogId = c.Id and a.ProducerId = c0.CodeFirmCr
where p.Actual = 1
and c0.Junk = 0
;", QueryParts.CostSubQuery("c0", "cc", "p"));
			var watch = Stopwatch.StartNew();
			watch.Start();

			var data = Db.Read(sql,
				r => new Offer(new OfferId(r.GetUInt32("FirmCode"),
					r.GetUInt64("RegionCode")),
					r.GetUInt32("Id"),
					r.GetDecimal("Cost"),
					SafeConvert.ToUInt32(r["Quantity"].ToString())),
				new {client})
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

			foreach (var item in data)
			{
				watch.Stop();

				if (item.Item1.Count() == 0)
					continue;

				if (log.IsDebugEnabled)
					log.DebugFormat("Ожидание данных {0}с", watch.Elapsed.TotalSeconds);

				var client = item.Item1.First().ClientId;
				if (log.IsDebugEnabled)
					log.DebugFormat("Начал вычисление средних цен для клиента {0}", client);
				var rating = item.Item1.ToDictionary(r => r.RegionId, r => r.Value);

				foreach (var offer in item.Item2)
				{
					var costs = (Hashtable) result[offer.Id];
					if (costs == null)
					{
						costs = new Hashtable();
						result[offer.Id] = costs;
					}

					var aggregates = (OfferAggregates)costs[offer.AssortmentId];
					if (aggregates == null) {
						aggregates = new OfferAggregates();
						costs[offer.AssortmentId] = aggregates;
					}

					if (!rating.ContainsKey(offer.Id.RegionId))
						continue;

					//если цена слишком большая значит это какая то лажа и ее нужно игнорировать
					if (CostThreshold > 0 && offer.Cost > CostThreshold)
						continue;

					var regionRating = rating[offer.Id.RegionId];
					aggregates.Cost = aggregates.Cost + offer.Cost*regionRating;
					aggregates.Quantity += offer.Quantity;
					aggregates.Count++;
#if DEBUG
					if (offer.AssortmentId == DebugAssortmentId && offer.Id.SupplierId == DebugSupplierId)
						Console.WriteLine("Average cost = {0}, cost = {3}, client = {1}, ratings = {2}", costs[offer.AssortmentId], client, regionRating, offer.Cost);
#endif
				}
				if (log.IsDebugEnabled)
					log.DebugFormat("Закончил вычисление средних цена для клиента {0}", client);

				watch.Reset();
				watch.Start();
			}
			return result;
		}

		public uint DebugAssortmentId;
		public uint DebugSupplierId;
		public decimal CostThreshold;

		public int Save(DateTime date, Hashtable hash)
		{
			var header = "insert into Reports.AverageCosts(Date, SupplierId, RegionId, AssortmentId, Cost, Quantity) values ";
			var page = 100;
			var totalCount = 0;
			With.Transaction(t => {
				var sql = new StringBuilder();
				sql.Append(header);
				var command = new MySqlCommand("", t.Connection);
				var index = 0;
				foreach (OfferId key in hash.Keys)
				{
					var costs = ((Hashtable) hash[key]);
					foreach (uint assortmentId in costs.Keys)
					{
						totalCount++;
						if (sql.Length > header.Length)
							sql.Append(", ");

						var aggregates = (OfferAggregates) costs[assortmentId];
						sql.AppendFormat("('{0}', {1}, {2}, {3}, {4}, {5})",
							date.ToString(MySqlConsts.MySQLDateFormat),
							key.SupplierId,
							key.RegionId,
							assortmentId,
							aggregates.Cost.ToString(CultureInfo.InvariantCulture),
							aggregates.AvgQuantity);

						index++;
						if (index >= page)
						{
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
