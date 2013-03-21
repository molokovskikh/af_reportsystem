using System;
using System.Collections.Generic;
using System.Linq;
using Common.MySql;
using Common.Tools.Calendar;
using MySql.Data.MySqlClient;

namespace Report.Data.Builder
{
	public class RatingCalculator
	{
		private DateTime _begin;
		private DateTime _end;
		private string _ordersSchema = "OrdersOld";

		public RatingCalculator()
		{
		}

		public RatingCalculator(DateTime begin, DateTime end)
		{
#if DEBUG
			_ordersSchema = "Orders";
#endif

			_begin = begin;
			_end = end;
		}

		public IEnumerable<ClientRating> Ratings()
		{
			return Calculate(CalculateRegionalTotals(), CalculateRating());
		}

		public IEnumerable<ClientRating> Calculate(
			IEnumerable<Tuple<decimal, ulong>> regional,
			IEnumerable<ClientRating> clients)
		{
			return clients.Join(regional,
				c => c.RegionId,
				r => r.Item2,
				(c, r) => new ClientRating(c.ClientId, c.RegionId, c.Value / r.Item1));
		}

		private IEnumerable<Tuple<decimal, ulong>> CalculateRegionalTotals()
		{
			var sql = String.Format(@"
select sum(ol.Quantity * ol.Cost) as total, oh.RegionCode
from {0}.OrdersHead oh
join {0}.OrdersList ol on ol.OrderId = oh.RowId
join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
where oh.WriteTime >= ?begin and oh.WriteTime < ?end and pd.IsLocal = 0
group by oh.RegionCode
", _ordersSchema);
			return Db.Read(sql,
				r => Tuple.Create(
					r.GetDecimal("total"),
					r.GetUInt64("RegionCode")),
				new { begin = _begin, end = _end });
		}

		private IEnumerable<ClientRating> CalculateRating()
		{
			var sql = String.Format(@"
select sum(ol.Quantity * ol.Cost) as total, oh.ClientCode, oh.RegionCode
from {0}.OrdersHead oh
join {0}.OrdersList ol on ol.OrderId = oh.RowId
join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
where oh.WriteTime >= ?begin and oh.WriteTime < ?end and pd.IsLocal = 0
group by oh.ClientCode, oh.RegionCode
", _ordersSchema);
			return Db.Read(sql,
				r => new ClientRating(
					r.GetUInt32("ClientCode"),
					r.GetUInt64("RegionCode"),
					r.GetDecimal("total")),
				new { begin = _begin, end = _end });
		}

		public static IEnumerable<ClientRating> Caclucated(DateTime begin, DateTime end)
		{
			return new RatingCalculator(begin, end).Ratings();
		}

		public static void Save(DateTime date, IEnumerable<ClientRating> ratings)
		{
			With.Transaction(t => {
				var sql = "insert into Reports.ClientRatings(Date, ClientId, RegionId, Rating) value (?Date, ?ClientId, ?RegionId, ?Rating)";
				var command = new MySqlCommand(sql, t.Connection);
				command.Parameters.Add("Date", MySqlDbType.DateTime);
				command.Parameters.Add("ClientId", MySqlDbType.UInt32);
				command.Parameters.Add("RegionId", MySqlDbType.UInt64);
				command.Parameters.Add("Rating", MySqlDbType.Decimal);
				command.Prepare();
				foreach (var rating in ratings) {
					command.Parameters["Date"].Value = date;
					command.Parameters["ClientId"].Value = rating.ClientId;
					command.Parameters["RegionId"].Value = rating.RegionId;
					command.Parameters["Rating"].Value = rating.Value;
					command.ExecuteNonQuery();
				}
			});
		}

		public static IEnumerable<ClientRating> CaclucatedAndSave(DateTime date)
		{
			var ratings = ReadRating(date);

			if (ratings.Length > 0)
				return ratings;

			var calculator = new RatingCalculator(date, date.LastDayOfMonth());
			ratings = calculator.Ratings().ToArray();
			Save(date, ratings);
			return ratings;
		}

		public static ClientRating[] ReadRating(DateTime date)
		{
			var ratings = Db.Read("select ClientId, RegionId, Rating from Reports.ClientRatings where date = ?date",
				r => new ClientRating(r.GetUInt32("ClientId"), r.GetUInt64("RegionId"), r.GetDecimal("Rating")),
				new { date })
				.ToArray();
			return ratings;
		}
	}
}