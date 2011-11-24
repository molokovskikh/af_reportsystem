using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
			get { return _date;  }
			set
			{
				_date = value;
				PrevDay = _date.AddDays(-1);
				PrevWeek = _date.AddDays(-7 - (_date.DayOfWeek - DayOfWeek.Monday));
				PrevMonth = _date.AddMonths(-1).FirstDayOfMonth();
			}
		}

		public DateTime PrevMonth;
		public DateTime PrevWeek;
		public DateTime PrevDay;

		public List<string> Filters = new List<string>();

		public CostDynamicSettings(ulong reportCode, string reportCaption)
			: base(reportCode, reportCaption)
		{}

		public string DateGroupLabel()
		{
			return String.Format("Относительно {0:d MMMM yyyy}", Date);
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
		private CostDynamicSettings settings;

		public CostDynamic()
		{
		}

		public CostDynamic(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties) : base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			settings = new CostDynamicSettings(_reportCode, _reportCaption) {
				Regions = regions,
				Suppliers = suppliers,
				Date = date
			};

			settings.Filters.Add(String.Format("Динамика уровня цен и доли рынка на {0}", date.ToShortDateString()));
			settings.Filters.Add(String.Format("Регион {0}", settings.Regions.Select(r => Region.Find(r).Name).Implode()));

			var begin = date.AddMonths(-1);
			var end = date;
			//join Catalogs.Catalog c on c.Id = p.CatalogId вроде бы не нужен 
			//но без него оптимизатор строит неправильный план
			args.DataAdapter.SelectCommand.CommandText = @"
select a.Id, sum(ol.Quantity) as quantity
from Orders.OrdersHead oh
join Orders.OrdersList ol on oh.RowId = ol.OrderId
join Catalogs.Products p on p.Id = ol.ProductId
join Catalogs.Catalog c on c.Id = p.CatalogId
join Catalogs.Assortment a on a.CatalogId = c.Id and a.ProducerId = ol.CodeFirmCr
where oh.WriteTime >= ?begin and oh.WriteTime <= ?end
group by a.Id
";
			args.DataAdapter.SelectCommand.Parameters.AddWithValue("begin", begin);
			args.DataAdapter.SelectCommand.Parameters.AddWithValue("end", end);
			var quantityTable = new DataTable();
			args.DataAdapter.Fill(quantityTable);
			var quantities = new Hashtable();

			foreach (DataRow row in quantityTable.Rows)
			{
				quantities.Add(Convert.ToUInt32(row["Id"]), Convert.ToDecimal(row["quantity"]));
			}

			args.DataAdapter.SelectCommand.CommandText = String.Format(@"select Id, Name
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
				{"PrevMonthCostDiff", settings.PrevMonth},
				{"PrevWeekCostDiff", settings.PrevWeek},
				{"PrevDayCostDiff", settings.PrevDay}
			};

			var beginOrderTotals = GetMarketShare(suppliers, regions, date);
			var prevDayOrderTotals = GetMarketShare(suppliers, regions, settings.PrevDay);
			var prevWeekOrderTotals = GetMarketShare(suppliers, regions, settings.PrevWeek);
			var prevMonthOrder = GetMarketShare(suppliers, regions, settings.PrevMonth);

			foreach (var supplier in suppliers)
			{
				var row = results.Rows.Cast<DataRow>().First(r => Convert.ToUInt32(r["Id"]) == supplier);
				var beginPrice = regions.Sum(r => CalculatePriceCost(begin, supplier, r, quantities));
				if (beginPrice == 0)
					break;

				row["PrevDayMarketShareDiff"] = CalculateShareDiff(beginOrderTotals, prevDayOrderTotals, supplier);
				row["PrevWeekMarketShareDiff"] = CalculateShareDiff(beginOrderTotals, prevWeekOrderTotals, supplier);
				row["PrevMonthMarketShareDiff"] = CalculateShareDiff(beginOrderTotals, prevMonthOrder, supplier);
				
				foreach (var dateToColumn in dateMap)
				{
					var endPrice = regions.Sum(r => CalculatePriceCost(dateToColumn.Value, supplier, r, quantities));
					row[dateToColumn.Key] = InPercentOf(endPrice, beginPrice);
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

		private static decimal CalculateShareDiff(DataTable beginOrderTotals, DataTable prevDayOrderTotals, uint supplier)
		{
			var currentRow = beginOrderTotals.Rows.Cast<DataRow>()
				.Where(r => r["SupplierId"] != DBNull.Value)
				.FirstOrDefault(r => Convert.ToUInt32(r["SupplierId"]) == supplier);
			var baseRow = prevDayOrderTotals.Rows.Cast<DataRow>()
				.Where(r => r["SupplierId"] != DBNull.Value)
				.FirstOrDefault(r => Convert.ToUInt32(r["SupplierId"]) == supplier);

			if (currentRow == null || baseRow == null)
				return 0;

			return InPercentOf(
				Convert.ToDecimal(currentRow["total"]),
				Convert.ToDecimal(baseRow["total"]));
		}

		private static decimal InPercentOf(decimal value, decimal @base)
		{
			return Math.Round((value/@base - 1)*100, 2);
		}

		private DataTable GetMarketShare(uint[] suppliers, ulong[] regions, DateTime date)
		{
			var command = args.DataAdapter.SelectCommand;
			command.CommandText = String.Format(@"
select sum(ol.Cost * ol.Quantity) as total, pd.FirmCode as SupplierId
from Orders.OrdersHead oh
join Orders.OrdersList ol on oh.RowId = ol.OrderId
join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
where writetime >= ?begin and writetime < ?end
and pd.FirmCode in ({0}) and oh.RegionCode in ({1})
", suppliers.Implode(), regions.Implode());

			command.Parameters.Clear();
			command.Parameters.AddWithValue("begin", date.Date);
			command.Parameters.AddWithValue("end", date.Date.AddDays(1));
			var table = new DataTable();
			args.DataAdapter.Fill(table);
			return table;
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

		public decimal CalculatePriceCost(DateTime date, uint supplierId, ulong regionId, Hashtable quantities)
		{
			decimal result = 0;

			var command = args.DataAdapter.SelectCommand;
			command.CommandText = @"
select AssortmentId, Cost
from Reports.AverageCosts
where SupplierId = ?supplierId
and RegionId = ?regionId
and Date = ?date
";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("supplierId", supplierId);
			command.Parameters.AddWithValue("regionId", regionId);
			command.Parameters.AddWithValue("date", date);
			var table = new DataTable();
			args.DataAdapter.Fill(table);
			foreach (DataRow row in table.Rows)
			{
				var quantiry = quantities[row["AssortmentId"]];
				if (quantiry == null)
					continue;
				result = Convert.ToDecimal(row["Cost"])*Convert.ToDecimal(quantiry);
			}
			if (result == 0)
				throw new Exception(String.Format("Данные за период {0} не подготовленны, в таблице reports.AverageCosts нет данных для этой даты", date.ToShortDateString()));
			return result;
		}

		public override void ReadReportParams()
		{
			date = DateTime.Today;
			if (reportParamExists("date"))
				date = (DateTime) getReportParam("date");

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