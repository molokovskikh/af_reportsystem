using System;
using System.Diagnostics;
using Common.Tools.Calendar;
using MySql.Data.MySqlClient;
using System.Data;
using ExcelLibrary.SpreadSheet;
using System.IO;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Writers;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem
{
	public class OptimizationRivalOrders : BaseReport
	{
		private DateTime _beginDate;
		private DateTime _endDate;
		private int _clientId = 0;
		private int _supplierId;
		private int _reportInterval;
		private bool _byPreviousMonth;
		private int _optimizedCount;
		private string _suppliersConcurent;
		private string _supplierName;

		public OptimizationRivalOrders(MySqlConnection Conn, DataSet dsProperties)
			: base(Conn, dsProperties)
		{
		}

		protected override void GenerateReport()
		{
			_suppliersConcurent = OptimizationEfficiency.GetCostOptimizationConcurents(DataAdapter, _supplierId);
			_supplierName = OptimizationEfficiency.GetSupplierName(DataAdapter, _supplierId);
			var command = DataAdapter.SelectCommand;

			command.CommandText =
				@"drop temporary table IF EXISTS CostOptimization;
create temporary table CostOptimization engine memory
select
oh.writetime,
ol.Cost,
	if(u.id is null, cl.Name, fc.Name) as ClientName,
	adr.Address as Address,
	u.Name as UserName,
	ol.Code, ol.CodeCr, s.Synonym, sfc.Synonym as Firm, ol.Quantity, col.SelfCost, col.ResultCost,
	round(col.ResultCost - ol.Cost, 2) absDiff, round((col.ResultCost / ol.Cost - 1) * 100, 2) diff
from " +
#if DEBUG
					@"orders.ordershead oh
	join orders.orderslist ol on ol.orderid = oh.rowid " +
#else
	@"ordersold.ordershead oh
	join ordersold.orderslist ol on ol.orderid = oh.rowid " +
#endif
					@"
	join usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
	join logs.CostOptimizationLogs col on
		oh.writetime > col.LoggedOn and col.ProductId = ol.ProductId and col.ProducerId = ol.CodeFirmCr and
		 (col.ClientId = ?clientId or ?clientId = 0) and col.SupplierId = ?supplierId and oh.UserId = col.UserId
and col.LoggedOn in (select max(LoggedOn) from logs.CostOptimizationLogs where SupplierId = ?supplierId and oh.UserId = UserId and LoggedOn < oh.writetime)
	join farm.Synonym s on s.SynonymCode = ol.SynonymCode
	join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode
	join usersettings.CostOptimizationClients coc on coc.ClientId = oh.ClientCode
	join usersettings.CostOptimizationRules cor on cor.Id = coc.RuleId and cor.SupplierId = ?supplierId
	left join Customers.Users u on u.Id = oh.UserId
	left join Customers.Clients fc on fc.Id = u.ClientId
	left join Customers.Clients cl on cl.Id = oh.ClientCode
left join Customers.Addresses adr on adr.Id = oh.AddressId
where (oh.clientcode = ?clientId or ?clientId = 0) and pd.FirmCode <> ?supplierId and ol.Junk = 0 and ol.Cost > col.ResultCost and pd.IsLocal = 0
	and Date(oh.writetime) >= Date(?beginDate) and Date(oh.writetime) <= Date(?endDate)";
#if DEBUG
			command.CommandText += @"
group by ol.RowId
order by oh.writetime, ol.RowId;";
#else
			command.CommandText += @"
			group by ol.OrderId, ol.ProductId, ol.CodeFirmCr
			order by oh.writetime, ol.OrderId, ol.ProductId, ol.CodeFirmCr";
#endif

#if DEBUG
			Debug.WriteLine(command.CommandText);
#endif

			_endDate = DateTime.Today;
			if(Interval) {
				_beginDate = From;
				_endDate = To;
			}
			else if (_byPreviousMonth) {
				_beginDate = DateTime.Today.AddMonths(-1).FirstDayOfMonth();
				_endDate = DateTime.Today.AddMonths(-1).LastDayOfMonth();
			}
			else {
				_beginDate = _endDate.AddDays(-_reportInterval);
				_endDate = _endDate.AddDays(-1);
			}

			command.Parameters.AddWithValue("?beginDate", _beginDate);
			command.Parameters.AddWithValue("?endDate", _endDate);
			command.Parameters.AddWithValue("?clientId", _clientId);
			command.Parameters.AddWithValue("?supplierId", _supplierId);
			command.ExecuteNonQuery();

			command.CommandText =
				@"select count(*), ifnull(sum(ol.Cost*ol.Quantity), 0) Summ from CostOptimization ol";
			DataAdapter.Fill(_dsReport, "Common");

			command.CommandText =
				@"select ifnull(round(avg(diff), 2), 0) Summ, ifnull(round(avg(absDiff), 2), 0) SummAbs from CostOptimization;";
			DataAdapter.Fill(_dsReport, "AvgDiff");

			command.CommandText =
				@"select ifnull(sum(SelfCost*Quantity), 0) Summ from CostOptimization;";
			DataAdapter.Fill(_dsReport, "OrderVolume");

			command.CommandText =
				@"select * from CostOptimization order by WriteTime;";
			DataAdapter.Fill(_dsReport, "Temp");

			if (_clientId != 0) {
				command.CommandText =
					@"select concat(cl.Name, ' (', reg.Region, ')'), 1
	from Customers.Clients cl
		 join farm.Regions reg on reg.RegionCode = cl.RegionCode
	where Id = ?clientId";
				DataAdapter.Fill(_dsReport, "Client");
			}
			_optimizedCount = _dsReport.Tables["Temp"].Rows.Count;

			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("writetime", typeof(DateTime));

			if (_clientId == 0)
				dtRes.Columns.Add("ClientName");
			dtRes.Columns.Add("Address");
			if (_clientId == 0 || Convert.ToBoolean(_dsReport.Tables["Client"].Rows[0][1]))
				dtRes.Columns.Add("UserName");

			dtRes.Columns.Add("Code");
			dtRes.Columns.Add("CodeCr");
			dtRes.Columns.Add("Synonym");
			dtRes.Columns.Add("Firm");
			dtRes.Columns.Add("Quantity", typeof(int));
			dtRes.Columns.Add("SelfCost", typeof(decimal));
			dtRes.Columns.Add("Cost", typeof(decimal));
			dtRes.Columns.Add("ResultCost", typeof(decimal));
			dtRes.Columns.Add("absDiff", typeof(decimal));
			dtRes.Columns.Add("diff", typeof(double));

			// Добавляем пустые строки для заголовка
			for (int i = 0; i < 8; i++)
				dtRes.Rows.Add(dtRes.NewRow());

			foreach (DataRow row in _dsReport.Tables["Temp"].Rows) {
				var newRow = dtRes.NewRow();
				newRow["writetime"] = row["writetime"];
				//если строим отчет для всех клиентов или для новых
				if (_clientId == 0)
					newRow["ClientName"] = row["ClientName"];
				if (_clientId == 0 || Convert.ToBoolean(_dsReport.Tables["Client"].Rows[0][1]))
					newRow["UserName"] = row["UserName"];

				newRow["Address"] = row["Address"];
				newRow["Code"] = row["Code"];
				newRow["CodeCr"] = row["CodeCr"];
				newRow["Synonym"] = row["Synonym"];
				newRow["Firm"] = row["Firm"];
				newRow["Quantity"] = row["Quantity"];
				newRow["Cost"] = row["Cost"];
				newRow["SelfCost"] = row["SelfCost"];
				newRow["ResultCost"] = row["ResultCost"];
				newRow["absDiff"] = row["absDiff"];
				newRow["diff"] = row["diff"];
				dtRes.Rows.Add(newRow);
			}

			_dsReport.Tables.Add(dtRes);
		}

		public override void ReadReportParams()
		{
			if (_reportParams.ContainsKey("ClientCode"))
				_clientId = (int)_reportParams["ClientCode"];
			_supplierId = (int)GetReportParam("FirmCode");
			_reportInterval = (int)GetReportParam("ReportInterval");
			_byPreviousMonth = (bool)GetReportParam("ByPreviousMonth");
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format != ReportFormats.Excel)
				return null;
			return new OptimizationEfficiencyExcludeOleExcelWriter();
		}

		protected override BaseReportSettings GetSettings()
		{
			return new OptimizationEfficiencySettings(ReportCode, ReportCaption, _beginDate, _endDate,
				_clientId, _optimizedCount, _suppliersConcurent, _supplierName);
		}
	}
}