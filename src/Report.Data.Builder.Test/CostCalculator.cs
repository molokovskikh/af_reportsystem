using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common.MySql;
using MySql.Data.MySqlClient;

namespace Report.Data.Builder.Test
{
	public class CostCalculator
	{
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

		private IEnumerable<Offer> GetOffers(uint client)
		{
			var sql = @"
set @UserId = (select Id
from Future.Users
where ClientId = ?client
limit 1);

call Future.GetPrices(@UserId);
update Usersettings.Prices
set DisabledByClient = 0;
call Future.GetActivePrices(@UserId);
call Future.GetOffers(@UserId);

select a.Id, c.RegionCode, ap.FirmCode, c.Cost
from Usersettings.Core c
join Farm.Core0 c0 on c0.Id = c.Id
join Catalogs.Products p on p.Id = c.ProductId
join Catalogs.Assortment a on a.CatalogId = p.CatalogId and a.ProducerId = c0.CodeFirmCr
join Usersettings.ActivePrices ap on ap.PriceCode = c.PriceCode
group by ap.FirmCode, c.RegionCode, a.Id
;
";
			return Db.Read(sql, 
				r => new Offer(new OfferId(r.GetUInt32("FirmCode"), r.GetUInt64("RegionCode")), r.GetUInt32("Id"), r.GetDecimal("Cost")),
				new {client});
		}

		public Hashtable Calculate(IEnumerable<uint> clients, IEnumerable<Rating> ratings)
		{
			var result = new Hashtable();
			foreach (var client in clients)
			{
				var currentRatings = ratings.Where(r => r.ClientId == client).ToArray();
				if (currentRatings.Count() == 0)
					continue;

				var rating = currentRatings.ToDictionary(r => r.RegionId, r => r.Value);

				var first = true;
				Console.WriteLine("{0:hh:mm:ss.fff} {1}", DateTime.Now, client);
				foreach (var offer in GetOffers(client))
				{
					if (first)
					{
						Console.WriteLine("{0:hh:mm:ss.fff} {1} calculate", DateTime.Now, client);
						first = false;
					}

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
				Console.WriteLine("{0:hh:mm:ss.fff} {1} end", DateTime.Now, client);
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
	}
}