using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common.MySql;
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

	public class AvgCost
	{
		public OfferId Id;

		public uint AssortmentId;
		public DateTime Date;
		public decimal Cost;
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

		public Offer(OfferId id, uint assortmentId, decimal cost)
		{
			Id = id;
			AssortmentId = assortmentId;
			Cost = cost;
		}
	}

	public class CostCalculator
	{
		private ILog log = LogManager.GetLogger(typeof (CostCalculator));

		public IEnumerable<uint> Clients()
		{
			var sql = @"
select c.Id
from Future.Clients c
join Usersettings.RetClientsSet rcs on rcs.ClientCode = c.Id
where rcs.InvisibleOnFirm = 0
";
			return Db.Read<uint>(sql);
		}

		public Offer[] GetOffers(uint client)
		{
			var sql = String.Format(@"
set @UserId = (select Id
from Future.Users
where ClientId = ?client
limit 1);

call Future.GetPrices(@UserId);

select straight_join a.Id, p.RegionCode, p.FirmCode, {0} as Cost
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
				r => new Offer(new OfferId(r.GetUInt32("FirmCode"), r.GetUInt64("RegionCode")), r.GetUInt32("Id"), r.GetDecimal("Cost")),
				new {client})
				.ToArray();

			watch.Stop();
			if (log.IsDebugEnabled)
				log.DebugFormat("�������� ����������� ��� ������� {0} ������ {1}�", client, watch.Elapsed.TotalSeconds);

			return data;
		}

		public Hashtable Calculate(IEnumerable<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> data)
		{
			var result = new Hashtable();
			foreach (var item in data)
			{
				if (item.Item1.Count() == 0)
					continue;

				var client = item.Item1.First().ClientId;
				if (log.IsDebugEnabled)
					log.DebugFormat("Client {0} calculation begin", client);
				var rating = item.Item1.ToDictionary(r => r.RegionId, r => r.Value);

				foreach (var offer in item.Item2)
				{
					var costs = (Hashtable) result[offer.Id];
					if (costs == null)
					{
						costs = new Hashtable();
						result[offer.Id] = costs;
					}

					decimal cost = 0;
					var value = costs[offer.AssortmentId];
					if (value != null)
						cost = (decimal) value;

					costs[offer.AssortmentId] = cost + offer.Cost*rating[offer.Id.RegionId];
				}
				if (log.IsDebugEnabled)
					log.DebugFormat("Client {0} calculated", client);
			}
			return result;
		}

		public void Save(DateTime date, Hashtable hash)
		{
			var sql = "insert into Reports.AverageCosts(Date, SupplierId, RegionId, AssortmentId, Cost) value (?Date, ?SupplierId, ?RegionId, ?AssortmentId, ?Cost)";
			With.Transaction(t => {
				var command = new MySqlCommand(sql, t.Connection);
				command.Parameters.Add("Date", MySqlDbType.DateTime);
				command.Parameters.Add("SupplierId", MySqlDbType.UInt32);
				command.Parameters.Add("RegionId", MySqlDbType.UInt64);
				command.Parameters.Add("AssortmentId", MySqlDbType.UInt32);
				command.Parameters.Add("Cost", MySqlDbType.Decimal);
				command.Prepare();
				foreach (OfferId key in hash.Keys)
				{
					var costs = ((Hashtable) hash[key]);
					foreach (uint assortmentId in costs.Keys)
					{
						command.Parameters["Date"].Value = date;
						command.Parameters["SupplierId"].Value = key.SupplierId;
						command.Parameters["RegionId"].Value = key.RegionId;
						command.Parameters["AssortmentId"].Value = assortmentId;
						command.Parameters["Cost"].Value = costs[assortmentId];
						command.ExecuteNonQuery();
					}
				}
			});
		}

		public IEnumerable<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> Offers(IEnumerable<ClientRating> ratings, int count)
		{
			var clients = ratings.Select(r => r.ClientId).Distinct().ToList();
			if (count > clients.Count)
				count = clients.Count;
			var tasks = new Task<IEnumerable<Offer>>[count];
			for(var i = 0; i < count; i++)
			{
				tasks[i] = RunTask(clients[i]);
			}

			while (clients.Count > 0)
			{
				var index = Task.WaitAny(tasks);
				var task = tasks[index];
				if (clients.Count > count)
					tasks[index] = RunTask(clients.Skip(count).First());
				var id = (uint) task.AsyncState;
				clients.Remove(id);
				yield return Tuple.Create(ratings.Where(r => r.ClientId == id), task.Result);
			}
		}

		private Task<IEnumerable<Offer>> RunTask(uint id)
		{
			var task = new Task<IEnumerable<Offer>>(
				clientId => GetOffers((uint) clientId),
				id);
			task.Start();
			return task;
		}
	}
}