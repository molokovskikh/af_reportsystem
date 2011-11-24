using System;
using System.Collections.Generic;
using System.Linq;
using Common.MySql;
using MySql.Data.MySqlClient;

namespace Report.Data.Builder.Test
{
	public class RatingCalculator
	{
		private DateTime _begin;
		private DateTime _end;

		public RatingCalculator()
		{}

		public RatingCalculator(DateTime begin, DateTime end)
		{
			_begin = begin;
			_end = end;
		}

		public IEnumerable<Tuple<decimal, uint, ulong>> Ratings()
		{
			return Calculate(CalculateRegionalTotals(), CalculateRating());
		}

		public IEnumerable<Tuple<decimal, uint, ulong>> Calculate(
			IEnumerable<Tuple<decimal, ulong>> regional, 
			IEnumerable<Tuple<decimal, uint, ulong>> clients)
		{
			return clients.Join(regional, c => c.Item3, r => r.Item2, (c, r) => Tuple.Create(c.Item1/r.Item1, c.Item2, c.Item3));
		}

		private IEnumerable<Tuple<decimal, ulong>> CalculateRegionalTotals()
		{
			var sql = @"
select sum(ol.Quantity * ol.Cost) as total, oh.RegionCode
from Orders.OrdersHead oh
join Orders.OrdersList ol on ol.OrderId = oh.RowId
where oh.WriteTime >= ?begin and oh.WriteTime <= ?end
group by oh.RegionCode
";
			return Db.Read(sql,
				r => Tuple.Create(
					r.GetDecimal("total"),
					r.GetUInt64("RegionCode")),
				new { begin = _begin, end = _end });
		}

		private IEnumerable<Tuple<decimal, uint, ulong>> CalculateRating()
		{
			var sql = @"
select sum(ol.Quantity * ol.Cost) as total, oh.ClientCode, oh.RegionCode
from Orders.OrdersHead oh
join Orders.OrdersList ol on ol.OrderId = oh.RowId
where oh.WriteTime >= ?begin and oh.WriteTime <= ?end
group by oh.ClientCode, oh.RegionCode
";
			return Db.Read(sql,
				r => Tuple.Create(
					r.GetDecimal("total"),
					r.GetUInt32("ClientCode"),
					r.GetUInt64("RegionCode")),
				new { begin = _begin, end = _end });
		}

		public static IEnumerable<Rating> Caclucated(DateTime begin, DateTime end)
		{
			return new RatingCalculator(begin, end)
				.Ratings()
				.Select(t => new Rating(t.Item2, t.Item3, t.Item1));
		}

		public void Save(DateTime date, IEnumerable<Tuple<decimal, uint, ulong>> ratings)
		{
			With.Transaction(t => {
				var sql = "insert into Reports.ClientRatings(Date, ClientId, RegionId, Rating) value (?Date, ?ClientId, ?RegionId, ?Rating)";
				var command = new MySqlCommand(sql, t.Connection);
				command.Parameters.Add("Date", MySqlDbType.DateTime);
				command.Parameters.Add("ClientId", MySqlDbType.UInt32);
				command.Parameters.Add("RegionId", MySqlDbType.UInt64);
				command.Parameters.Add("Rating", MySqlDbType.Decimal);
				command.Prepare();
				foreach (var tuple in ratings)
				{
					command.Parameters["Date"].Value = date;
					command.Parameters["ClientId"].Value = tuple.Item2;
					command.Parameters["RegionId"].Value = tuple.Item3;
					command.Parameters["Rating"].Value = tuple.Item1;
					command.ExecuteNonQuery();
				}
			});
		}
	}
}