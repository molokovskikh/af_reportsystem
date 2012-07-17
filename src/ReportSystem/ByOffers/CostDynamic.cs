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
				var dayOfWeek = (int)_date.DayOfWeek;
				//у американцев неделя начинается с воскресенья
				if (dayOfWeek == 0)
					dayOfWeek = 7;
				PrevWeek = _date.AddDays(-(dayOfWeek - (int)DayOfWeek.Monday));

				if (PrevWeek == Date)
					PrevWeek = PrevWeek.AddDays(-7);

				PrevMonth = _date.FirstDayOfMonth();
				if (PrevMonth == Date)
					PrevMonth = PrevMonth.AddMonths(-1);

				SameDayOnPastWeek = _date.AddDays(-7);

				Dates = new List<ColumnGroupDescription> {

					new ColumnGroupDescription(SomeDate,
						"SomeDate",
						String.Format("Относительно {0:d MMMM yyyy}", SomeDate)),

					new ColumnGroupDescription(PrevMonth,
						"PrevMonth",
						String.Format("Относительно 1-го числа ({0})", PrevMonth.ToShortDateString())),

					new ColumnGroupDescription(SameDayOnPastWeek,
						"SameDayOnPastWeek",
						String.Format("Относительно того же дня недели прошедшей недели ({0})", SameDayOnPastWeek.ToShortDateString())),

					new ColumnGroupDescription(PrevWeek,
						"PrevWeek",
						String.Format("Относительно предыдущего понедельника ({0})", PrevWeek.ToShortDateString())),

					new ColumnGroupDescription(PrevDay,
						"PrevDay",
						String.Format("Относительно предыдущего дня ({0})", PrevDay.ToShortDateString())),
				};
			}
		}

		public DateTime SomeDate;
		public DateTime PrevMonth;
		public DateTime PrevWeek;
		public DateTime PrevDay;
		public DateTime SameDayOnPastWeek;

		public List<string> Filters = new List<string>();
		public List<ColumnGroupDescription> Dates;

		public CostDynamicSettings(ulong reportCode, string reportCaption)
			: base(reportCode, reportCaption)
		{}
	}

	public class ColumnGroupDescription
	{
		public DateTime Date;
		public string Name;
		public string Label;

		public ColumnGroupDescription(DateTime date, string name, string label)
		{
			Date = date;
			Name = name;
			Label = label;
		}
	}

	public class OrdersOnDate
	{
		public DataTable SupplierMarketShares;
		public decimal MarketTotal;

		public OrdersOnDate(DataTable supplierMarketShares, decimal marketTotal)
		{
			SupplierMarketShares = supplierMarketShares;
			MarketTotal = marketTotal;
		}
	}

	public class CostDynamic : OrdersReport
	{
		private ulong[] regions;
		private uint[] suppliers;
		private DateTime date;
		private DateTime someDate;
		private CostDynamicSettings settings;

		private MySqlCommand command;

		public CostDynamic()
		{}

		public CostDynamic(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{}

		public override void GenerateReport(ExecuteArgs e)
		{
			command = args.DataAdapter.SelectCommand;
			if (regions.Length == 0)
			{
				command.CommandText = String.Format(@"
select oh.RegionCode
from {0}.OrdersHead oh
where oh.WriteTime >= ?begin and oh.WriteTime <= ?end
group by oh.RegionCode", OrdersSchema);
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
group by pd.FirmCode", OrdersSchema, regions.Implode());
				command.Parameters.Clear();
				command.Parameters.AddWithValue("begin", date);
				command.Parameters.AddWithValue("end", date.AddDays(1));
				var supplierIdTable = new DataTable();
				args.DataAdapter.Fill(supplierIdTable);
				suppliers = supplierIdTable.AsEnumerable().Select(r => Convert.ToUInt32(r["FirmCode"])).ToArray();
			}

			settings = new CostDynamicSettings(ReportCode, ReportCaption) {
				Regions = regions,
				Suppliers = suppliers,
				SomeDate = someDate,
				Date = date,
			};

			settings.Filters.Add(String.Format("Динамика уровня цен и доли рынка на {0}", date.ToShortDateString()));
			settings.Filters.Add(String.Format("Регион {0}", settings.Regions.Select(r => Region.Find(r).Name).Implode()));
			FillFilterDescriptions();
			settings.Filters.AddRange(filterDescriptions);

			var quantities = GetQuantities();

			command.CommandText = String.Format(@"select Id, Name
from Customers.Suppliers
where id in ({0})", suppliers.Implode());
			var supplierTable = new DataTable();
			args.DataAdapter.Fill(supplierTable);

			var results = CreateResultTable(settings.Dates);

			foreach (DataRow supplier in supplierTable.Rows)
			{
				var row = results.NewRow();
				row["Id"] = supplier["Id"];
				row["Name"] = supplier["Name"];
				results.Rows.Add(row);
			}

			var baseOrderTotals = GetMarketShare(suppliers, regions, date);
			var marketTotalOnCurrentDate = GetMarketValue(regions, date);

			var marketSharesOnDate = new Dictionary<string, OrdersOnDate>();

			foreach (var dateColumn in settings.Dates)
			{
				var supplierMarketShares = GetMarketShare(suppliers, regions, dateColumn.Date);
				var marketTotal = GetMarketValue(regions, dateColumn.Date);
				marketSharesOnDate.Add(dateColumn.Name, new OrdersOnDate(supplierMarketShares, marketTotal));
			}

			foreach (var supplier in suppliers)
			{
				var row = results.Rows.Cast<DataRow>().First(r => Convert.ToUInt32(r["Id"]) == supplier);

				var baseTotal = GetTotal(baseOrderTotals, supplier);

				var marketShare = SaveInPercentOf(baseTotal, marketTotalOnCurrentDate);
				row.SetField("MarketShare", marketShare);

				foreach (var dateToColumn in settings.Dates)
				{
					var column = dateToColumn.Name + "MarketShareDiff";
					var currentDate = dateToColumn.Date;

					var supplierMarketShares = marketSharesOnDate[dateToColumn.Name].SupplierMarketShares;
					var marketTotal = marketSharesOnDate[dateToColumn.Name].MarketTotal;
					row.SetField(column, marketShare - CalculateShareDiff(marketTotal, supplierMarketShares, supplier));

					var baseCostIndex = regions.Sum(r => CalculateCostIndex(date, currentDate, supplier, r, quantities));
					var currentCostIndex = regions.Sum(r => CalculateCostIndex(currentDate, date, supplier, r, quantities));
					var columnName = dateToColumn.Name + "CostIndex";
					row.SetField(columnName, SaveInPercentOf(baseCostIndex, currentCostIndex) - 1);
				}
			}

			results.DefaultView.Sort = "MarketShare DESC";
			results = results.DefaultView.ToTable();
			_dsReport.Tables.Add(results);

			var orderColums = results.Columns.Cast<DataColumn>().Where(c => c.ColumnName.Contains("MarketShare"));
			BuildAggregateRow(results, "Суммарно по мониторируемым компаниям:",
				Enumerable.Sum, orderColums);
			var costIndexColumns = results.Columns.Cast<DataColumn>().Where(c => c.ColumnName.Contains("CostIndex"));
			BuildAggregateRow(results, "Среднее по мониторируемым компаниям:",
				Enumerable.Average,costIndexColumns);
		}

		private static void BuildAggregateRow(DataTable results, string name, Func<IEnumerable<decimal>, decimal> aggregate, IEnumerable<DataColumn> columns)
		{
			var resultRow = results.NewRow();
			resultRow["Name"] = name;
			foreach (var column in columns.Where(c => c.DataType == typeof (decimal)))
			{
				var values = results.AsEnumerable()
					.Where(r => r[column] != DBNull.Value)
					.Select(r => Convert.ToDecimal(r[column]));
				if (values.Count() > 0)
					resultRow[column] = aggregate(values);
			}
			results.Rows.Add(resultRow);
		}

		private Hashtable GetQuantities()
		{
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
group by a.Id", OrdersSchema);
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
			return quantities;
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
  join catalogs.products p on p.Id = ol.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
  left join Customers.Clients cl on cl.Id = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join Customers.suppliers prov on prov.Id = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join Customers.addresses adr on oh.AddressId = adr.Id
  join billing.LegalEntities le on adr.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId
where writetime >= ?begin and writetime < ?end
and oh.RegionCode in ({0})
", regions.Implode(), OrdersSchema);

			command.CommandText = ApplyUserFilters(command.CommandText);

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
  join catalogs.products p on p.Id = ol.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
  left join Customers.Clients cl on cl.Id = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join Customers.suppliers prov on prov.Id = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join Customers.addresses adr on oh.AddressId = adr.Id
  join billing.LegalEntities le on adr.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId
where writetime >= ?begin and writetime < ?end
and pd.FirmCode in ({0}) and oh.RegionCode in ({1})
", suppliers.Implode(), regions.Implode(), OrdersSchema);

			command.CommandText = ApplyUserFilters(command.CommandText);
			command.CommandText += "\r\ngroup by pd.FirmCode";

			command.Parameters.Clear();
			command.Parameters.AddWithValue("begin", date.Date);
			command.Parameters.AddWithValue("end", date.Date.AddDays(1));
			var table = new DataTable();
			args.DataAdapter.Fill(table);
			return table;
		}

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

		public DataTable CreateResultTable(List<ColumnGroupDescription> dates)
		{
			var results = new DataTable("Results");
			var column = results.Columns.Add("Id", typeof (uint));

			column = results.Columns.Add("Name");
			column.Caption = "Поставщик";
			column.ExtendedProperties.Add("Width", 23);

			column = results.Columns.Add("MarketShare", typeof (decimal));
			column.Caption = "Текущая доля рынка";
			column.ExtendedProperties.Add("Width", 13);

			foreach(var date in dates)
			{
				column = results.Columns.Add(date.Name + "MarketShareDiff", typeof (decimal));
				column.Caption = "Прирост доли";
				column.ExtendedProperties.Add("Width", 13);

				column = results.Columns.Add(date.Name + "CostIndex", typeof (decimal));
				column.Caption = "Изменение индекса цен ΔP";
				column.ExtendedProperties.Add("Width", 13);
			}

			return results;
		}

		public override void ReadReportParams()
		{
			date = DateTime.Today.AddDays(-1);
			if (reportParamExists("date"))
				date = (DateTime) getReportParam("date");

			if (From != DateTime.MinValue)
				date = From;

			someDate = (DateTime) getReportParam("someDate");
			regions = ((List<ulong>) getReportParam("regions")).ToArray();
			suppliers = ((List<ulong>) getReportParam("suppliers")).Select(Convert.ToUInt32).ToArray();

			LoadAdditionFiles();
			LoadFilters();
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