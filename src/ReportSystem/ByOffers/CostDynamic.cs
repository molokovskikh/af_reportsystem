using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common.MySql;
using Common.Tools.Calendar;
using ExecuteTemplate;
using Common.Tools;
using Inforoom.ReportSystem.Model;
using Inforoom.ReportSystem.Writers;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.ByOffers
{
	public class CostDynamicSettings : ReportSettings.BaseReportSettings
	{
		private DateTime _date;

		public uint[] Suppliers;
		public ulong[] Regions;

		public DateTime Date
		{
			get { return _date; }
			set
			{
				_date = value;
				PrevDay = _date.AddDays(-1);
				PrevWeek = _date.AddDays(-7 - (_date.DayOfWeek - DayOfWeek.Monday));
				PrevMonth = _date.AddMonths(-1).FirstDayOfMonth();
			}
		}

		public DateTime SomeDate;
		public DateTime PrevMonth;
		public DateTime PrevWeek;
		public DateTime PrevDay;

		public List<string> Filters = new List<string>();

		public CostDynamicSettings(ulong reportCode, string reportCaption)
			: base(reportCode, reportCaption)
		{}

		public string DateGroupLabel()
		{
			return String.Format("Относительно {0:d MMMM yyyy}", SomeDate);
		}

		public string PrevMonthLabel()
		{
			return String.Format("Относительно 1 числа прошлого месяца ({0})", PrevMonth.ToShortDateString());
		}

		public string PrevWeekLabel()
		{
			return String.Format("Относительно понедельника прошедшей недели ({0})", PrevWeek.ToShortDateString());
		}

		public string PrevDayLabel()
		{
			return String.Format("Относительно вчерашнего дня ({0})", PrevDay.ToShortDateString());
		}
	}

	public class CostDynamic : BaseReport
	{
		private ulong[] regions;
		private uint[] suppliers;
		private DateTime date;
		private DateTime someDate;
		private CostDynamicSettings settings;

		private string _ordersSchema = "Orders";

		public CostDynamic()
		{
		}

		public CostDynamic(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
#if !DEBUG
			_ordersSchema = "OrdersOld";
#endif
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			var command = args.DataAdapter.SelectCommand;
			if (regions.Length == 0)
			{
				command.CommandText = String.Format(@"
select oh.RegionCode
from {0}.OrdersHead oh
where oh.WriteTime >= ?begin and oh.WriteTime <= ?end
group by oh.RegionCode", _ordersSchema);
				command.Parameters.AddWithValue("begin", date);
				command.Parameters.AddWithValue("end", date.AddDays(1));
				var regionTable = new DataTable();
				args.DataAdapter.Fill(regionTable);
				regions = regionTable.AsEnumerable().Select(r => Convert.ToUInt64(r["RegionCode"])).ToArray();
			}

			if (suppliers.Length == 0 && regions.Length > 0)
			{
				command.CommandText = String.Format(@"
select pd.FirmCode
from {0}.OrdersHead oh
join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
where oh.WriteTime >= ?begin and oh.WriteTime <= ?end
and oh.RegionCode in ({1})
group by pd.FirmCode", _ordersSchema, regions.Implode());
				command.Parameters.Clear();
				command.Parameters.AddWithValue("begin", date);
				command.Parameters.AddWithValue("end", date.AddDays(1));
				var supplierIdTable = new DataTable();
				args.DataAdapter.Fill(supplierIdTable);
				suppliers = supplierIdTable.AsEnumerable().Select(r => Convert.ToUInt32(r["FirmCode"])).ToArray();
			}

			settings = new CostDynamicSettings(_reportCode, _reportCaption) {
				Regions = regions,
				Suppliers = suppliers,
				Date = date,
				SomeDate = someDate
			};

			settings.Filters.Add(String.Format("Динамика уровня цен и доли рынка на {0}", date.ToShortDateString()));
			settings.Filters.Add(String.Format("Регион {0}", settings.Regions.Select(r => Region.Find(r).Name).Implode()));

			var begin = date.AddMonths(-1);
			var end = date;
			//join Catalogs.Catalog c on c.Id = p.CatalogId вроде бы не нужен 
			//но без него оптимизатор строит неправильный план
			command.CommandText = String.Format(@"
select a.Id, sum(ol.Quantity) as quantity
from {0}.OrdersHead oh
join {0}.OrdersList ol on oh.RowId = ol.OrderId
join Catalogs.Products p on p.Id = ol.ProductId
join Catalogs.Catalog c on c.Id = p.CatalogId
join Catalogs.Assortment a on a.CatalogId = c.Id and a.ProducerId = ol.CodeFirmCr
where oh.WriteTime >= ?begin and oh.WriteTime <= ?end
group by a.Id
", _ordersSchema);
			command.Parameters.Clear();
			command.Parameters.AddWithValue("begin", begin);
			command.Parameters.AddWithValue("end", end);
			var quantityTable = new DataTable();
			args.DataAdapter.Fill(quantityTable);
			var quantities = new Hashtable();

			foreach (DataRow row in quantityTable.Rows)
			{
				quantities.Add(Convert.ToUInt32(row["Id"]), Convert.ToDecimal(row["quantity"]));
			}

			command.CommandText = String.Format(@"select Id, Name
from Future.Suppliers
where id in ({0})", suppliers.Implode());
			var supplierTable = new DataTable();
			args.DataAdapter.Fill(supplierTable);

			var results = CreateResultTable();
			_dsReport.Tables.Add(results);

			foreach (DataRow supplier in supplierTable.Rows)
			{
				var row = results.NewRow();
				row["Id"] = supplier["Id"];
				row["Name"] = supplier["Name"];
				results.Rows.Add(row);
			}

			var dateMap = new Dictionary<string, DateTime> {
				{"CostDiff", settings.SomeDate},
				{"PrevMonthCostDiff", settings.PrevMonth},
				{"PrevWeekCostDiff", settings.PrevWeek},
				{"PrevDayCostDiff", settings.PrevDay}
			};

			var baseOrderTotals = GetMarketShare(suppliers, regions, date);
			var marketTotalOnCurrentDate = GetMarketValue(regions, date);

			var someDayOrderTotals = GetMarketShare(suppliers, regions, settings.SomeDate);
			var marketTotalOnSomeDate = GetMarketValue(regions, settings.SomeDate);

			var prevDayOrderTotals = GetMarketShare(suppliers, regions, settings.PrevDay);
			var marketTotalOnPrevDate = GetMarketValue(regions, settings.PrevDay);

			var prevWeekOrderTotals = GetMarketShare(suppliers, regions, settings.PrevWeek);
			var marketTotalOnPrevWeek = GetMarketValue(regions, settings.PrevWeek);

			var prevMonthOrder = GetMarketShare(suppliers, regions, settings.PrevMonth);
			var marketTotalOnPrevMonth = GetMarketValue(regions, settings.PrevMonth);

			foreach (var supplier in suppliers)
			{
				var row = results.Rows.Cast<DataRow>().First(r => Convert.ToUInt32(r["Id"]) == supplier);

				var baseTotal = GetTotal(baseOrderTotals, supplier);

				var marketShare = SaveInPercentOf(baseTotal, marketTotalOnCurrentDate);
				row.SetField("MarketShare", marketShare);
				row.SetField("MarketShareDiff", marketShare - CalculateShareDiff(marketTotalOnSomeDate, someDayOrderTotals, supplier));
				row.SetField("PrevDayMarketShareDiff", marketShare - CalculateShareDiff(marketTotalOnPrevDate, prevDayOrderTotals, supplier));
				row.SetField("PrevWeekMarketShareDiff", marketShare - CalculateShareDiff(marketTotalOnPrevWeek, prevWeekOrderTotals, supplier));
				row.SetField("PrevMonthMarketShareDiff", marketShare - CalculateShareDiff(marketTotalOnPrevMonth, prevMonthOrder, supplier));

				foreach (var dateToColumn in dateMap)
				{
					var currentDate = dateToColumn.Value;
					var baseCostIndex = regions.Sum(r => CalculateCostIndex(begin, currentDate, supplier, r, quantities));
					var currentCostIndex = regions.Sum(r => CalculateCostIndex(currentDate, begin, supplier, r, quantities));
					row.SetField(dateToColumn.Key, SaveInPercentOf(baseCostIndex, currentCostIndex) - 1);
				}
			}

			var resultRow = results.NewRow();
			resultRow["Name"] = "Суммарно по мониторируемым компаниям:";
			foreach (var column in results.Columns.Cast<DataColumn>().Where(c => c.DataType == typeof(decimal)))
			{
				resultRow[column] = results.AsEnumerable()
					.Where(r => r[column] != DBNull.Value)
					.Sum(r => Convert.ToDecimal(r[column]));
			}
			results.Rows.Add(resultRow);
		}

		private static decimal? CalculateShareDiff(decimal? valueTotal, DataTable currentOrderTotals, uint supplier)
		{
			var baseTotal = GetTotal(currentOrderTotals, supplier);
			return SaveInPercentOf(baseTotal, valueTotal);
		}

		private static decimal? SaveInPercentOf(decimal valueTotal, decimal baseTotal)
		{
			if (baseTotal == 0)
				return null;

			return InPercentOf(valueTotal, baseTotal);
		}

		private static decimal? SaveInPercentOf(decimal? valueTotal, decimal? baseTotal)
		{
			if (valueTotal == null || baseTotal == null)
				return null;

			return SaveInPercentOf(valueTotal.Value, baseTotal.Value);
		}

		private static decimal InPercentOf(decimal value, decimal @base)
		{
			return Math.Round(value/@base, 4);
		}

		private static decimal? GetTotal(DataTable table, uint supplier)
		{
			var row = table.Rows.Cast<DataRow>()
				.Where(r => r["SupplierId"] != DBNull.Value)
				.FirstOrDefault(r => Convert.ToUInt32(r["SupplierId"]) == supplier);
			if (row == null)
				return null;
			return Convert.ToDecimal(row["total"]);
		}

		private decimal GetMarketValue(ulong[] regions, DateTime date)
		{
			var command = args.DataAdapter.SelectCommand;
			command.CommandText = String.Format(@"
select sum(ol.Cost * ol.Quantity) as total
from {1}.OrdersHead oh
join {1}.OrdersList ol on oh.RowId = ol.OrderId
where writetime >= ?begin and writetime < ?end
and oh.RegionCode in ({0})
", regions.Implode(), _ordersSchema);

			command.Parameters.Clear();
			command.Parameters.AddWithValue("begin", date.Date);
			command.Parameters.AddWithValue("end", date.Date.AddDays(1));
			var table = new DataTable();
			args.DataAdapter.Fill(table);
			var value = table.Rows[0][0];
			if (value == DBNull.Value)
				return 0;
			return Convert.ToDecimal(value);
		}

		private DataTable GetMarketShare(uint[] suppliers, ulong[] regions, DateTime date)
		{
			var command = args.DataAdapter.SelectCommand;
			command.CommandText = String.Format(@"
select sum(ol.Cost * ol.Quantity) as total, pd.FirmCode as SupplierId
from {2}.OrdersHead oh
join {2}.OrdersList ol on oh.RowId = ol.OrderId
join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
where writetime >= ?begin and writetime < ?end
and pd.FirmCode in ({0}) and oh.RegionCode in ({1})
group by pd.FirmCode
", suppliers.Implode(), regions.Implode(), _ordersSchema);

			command.Parameters.Clear();
			command.Parameters.AddWithValue("begin", date.Date);
			command.Parameters.AddWithValue("end", date.Date.AddDays(1));
			var table = new DataTable();
			args.DataAdapter.Fill(table);
			return table;
		}
		//оптимизированный способ выборки попробуем пока без него
/*		public struct CostIndexKey
		{
			public DateTime Date;
			public ulong RegionId;
			public uint SupplierId;

			public CostIndexKey(DateTime date, ulong regionId, uint supplierId)
			{
				Date = date;
				RegionId = regionId;
				SupplierId = supplierId;
			}
		}

		public struct AssortmentKey
		{
			public uint SupplierId;
			public ulong RegionId;

			public AssortmentKey(uint supplierId, ulong regionId)
			{
				SupplierId = supplierId;
				RegionId = regionId;
			}
		}

		public Hashtable c(DateTime[] dates, Hashtable assortments)
		{
			Hashtable quantities = null;
			Hashtable results = null;
			var command = args.DataAdapter.SelectCommand;
			command.CommandText = String.Format(@"
select Date, RegionId, SupplierId, AssortmentId, Cost
from Reports.AverageCosts
where SupplierId = ({1})
and RegionId in ({0})
and Date in ({2})
", suppliers.Implode(), regions.Implode(), dates.Implode(d => "'" + d.ToString("yyyy-MM-dd") + "'"));

			using(var reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					var assortmentId = reader.GetUInt32("AssortmentId");
					var cost = reader.GetDecimal("Cost");
					var quantity = (decimal)quantities[assortmentId];

					var regionId = reader.GetUInt64("RegionId");
					var supplierId = reader.GetUInt32("SupplierId");
					var key = new CostIndexKey(reader.GetDateTime("Date"),
						regionId,
						supplierId);

					var availableAssortment = (HashSet<uint>)assortments[new AssortmentKey(supplierId, regionId)];
					if (!availableAssortment.Contains(assortmentId))
						continue;

					decimal result = 0;
					var resultValue = results[key];
					if (resultValue != null)
						result = (decimal)resultValue;

					result += cost*quantity;

					results[key] = result;
				}
			}
		}*/

		public decimal CalculateCostIndex(DateTime date, DateTime toDate, uint supplierId, ulong regionId, Hashtable quantities)
		{
			decimal result = 0;

			var command = args.DataAdapter.SelectCommand;
			command.CommandText = @"
select a.AssortmentId, a.Cost
from Reports.AverageCosts a
join Reports.AverageCosts a1 on a.SupplierId = a1.SupplierId and a.RegionId = a1.RegionId and a.AssortmentId = a1.AssortmentId and a1.Date = ?toDate
where a.SupplierId = ?supplierId
and a.RegionId = ?regionId
and a.Date = ?date
";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("supplierId", supplierId);
			command.Parameters.AddWithValue("regionId", regionId);
			command.Parameters.AddWithValue("date", date);
			command.Parameters.AddWithValue("toDate", toDate);
			var table = new DataTable();
			args.DataAdapter.Fill(table);
			foreach (DataRow row in table.Rows)
			{
				var quantiry = quantities[row["AssortmentId"]];
				if (quantiry == null)
					continue;
				result += Convert.ToDecimal(row["Cost"])*Convert.ToDecimal(quantiry);
			}
			return result;
		}

		public DataTable CreateResultTable()
		{
			var results = new DataTable("Results");
			var column = results.Columns.Add("Id", typeof (uint));

			column = results.Columns.Add("Name");
			column.Caption = "Поставщик";
			column.ExtendedProperties.Add("Width", 23);

			column = results.Columns.Add("MarketShare", typeof (decimal));
			column.Caption = "Текущая доля рынка";
			column.ExtendedProperties.Add("Width", 13);

			column = results.Columns.Add("MarketShareDiff", typeof (decimal));
			column.Caption = "Прирост доли";
			column.ExtendedProperties.Add("Width", 13);

			column = results.Columns.Add("CostDiff", typeof (decimal));
			column.Caption = "Изменение индекса цен ΔP";
			column.ExtendedProperties.Add("Width", 13);

			column = results.Columns.Add("PrevMonthMarketShareDiff", typeof (decimal));
			column.Caption = "Прирост доли";
			column.ExtendedProperties.Add("Width", 13);

			column = results.Columns.Add("PrevMonthCostDiff", typeof (decimal));
			column.Caption = "Изменение индекса цен ΔP";
			column.ExtendedProperties.Add("Width", 13);

			column = results.Columns.Add("PrevWeekMarketShareDiff", typeof (decimal));
			column.Caption = "Прирост доли";
			column.ExtendedProperties.Add("Width", 13);

			column = results.Columns.Add("PrevWeekCostDiff", typeof (decimal));
			column.Caption = "Изменение индекса цен ΔP";
			column.ExtendedProperties.Add("Width", 13);

			column = results.Columns.Add("PrevDayMarketShareDiff", typeof (decimal));
			column.Caption = "Прирост доли";
			column.ExtendedProperties.Add("Width", 13);

			column = results.Columns.Add("PrevDayCostDiff", typeof (decimal));
			column.Caption = "Изменение индекса цен ΔP";
			column.ExtendedProperties.Add("Width", 13);

			return results;
		}

		public override void ReadReportParams()
		{
			date = DateTime.Today;
			if (reportParamExists("date"))
				date = (DateTime) getReportParam("date");

			if (_dtFrom != DateTime.MinValue)
				date = _dtFrom;

			someDate = (DateTime) getReportParam("someDate");
			regions = ((List<ulong>) getReportParam("regions")).ToArray();
			suppliers = ((List<ulong>) getReportParam("suppliers")).Select(Convert.ToUInt32).ToArray();
		}

		protected override ReportSettings.BaseReportSettings GetSettings()
		{
			return settings;
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			return new CostDynamicWriter();
		}
	}
}