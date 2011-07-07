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
	public class OptimizationEfficiency : BaseReport
	{
		private DateTime _beginDate;
		private DateTime _endDate;
		private int _clientId = 0;
		private int _supplierId = 5;
		private int _reportInterval;
		private bool _byPreviousMonth;
		private int _optimizedCount;	

		public OptimizationEfficiency(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{ 
		}

		public override void GenerateReport(ExecuteTemplate.ExecuteArgs e)
		{
			var command = e.DataAdapter.SelectCommand;
			
			command.CommandText =
@"drop temporary table IF EXISTS CostOptimization;
create temporary table CostOptimization engine memory
select oh.writetime,
#	if(u.id is null, cd.ShortName, fc.Name) as ClientName,
    if(u.id is null, cl.Name, fc.Name) as ClientName,
	u.Name as UserName,
    ol.Code, ol.CodeCr, s.Synonym, sfc.Synonym as Firm, ol.Quantity, col.SelfCost, col.ResultCost,
	round(col.ResultCost - col.SelfCost, 2) absDiff, round((col.ResultCost / col.SelfCost - 1) * 100, 2) diff,
    CASE WHEN col.ResultCost > col.SelfCost THEN (col.ResultCost - col.SelfCost)*ol.Quantity ELSE null END EkonomEffect,
	CASE WHEN col.ResultCost < col.SelfCost THEN col.ResultCost*ol.Quantity ELSE null END IncreaseSales
from " +
#if DEBUG
  @"orders.ordershead oh
  join orders.orderslist ol on ol.orderid = oh.rowid " +
#else
  @"ordersold.ordershead oh
  join ordersold.orderslist ol on ol.orderid = oh.rowid " +
#endif
 @"join usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
  left join usersettings.includeregulation ir on ir.IncludeClientCode = oh.clientcode
  join logs.CostOptimizationLogs col on 
        oh.writetime > col.LoggedOn and col.ProductId = ol.ProductId and ol.Cost = col.ResultCost and 
		(col.ClientId = ?clientId or ?clientId = 0 or col.ClientId = ir.PrimaryClientCode) and
        col.SupplierId = pd.FirmCode
  join farm.Synonym s on s.SynonymCode = ol.SynonymCode
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode
  join usersettings.CostOptimizationClients coc on coc.ClientId = oh.ClientCode
  join usersettings.CostOptimizationRules cor on cor.Id = coc.RuleId and cor.SupplierId = ?supplierId
  left join Future.Users u on u.Id = oh.UserId
    left join Future.Clients fc on fc.Id = u.ClientId
#  left join UserSettings.ClientsData cd on cd.FirmCode = oh.ClientCode
   left join Future.Clients cl on cl.Id = oh.ClientCode
where (oh.clientcode = ?clientId or ?clientId = 0) and pd.FirmCode = ?supplierId and ol.Junk = 0 
  and Date(oh.writetime) >= Date(?beginDate) and Date(oh.writetime) <= Date(?endDate)
group by ol.RowId
order by oh.writetime, ol.RowId;";

#if DEBUG
            Debug.WriteLine(command.CommandText);
#endif

			_endDate = DateTime.Today;
			if (_byPreviousMonth) // Определяем интервал построения отчета
			{
				_beginDate = DateTime.Today.AddMonths(-1).FirstDayOfMonth();
				_endDate = DateTime.Today.AddMonths(-1).LastDayOfMonth();
			}
			else
				_beginDate = _endDate.AddDays(-_reportInterval);

			command.Parameters.AddWithValue("?beginDate", _beginDate);
			command.Parameters.AddWithValue("?endDate", _endDate);
			command.Parameters.AddWithValue("?clientId", _clientId);
			command.Parameters.AddWithValue("?supplierId", _supplierId);
			command.ExecuteNonQuery();

	/*		command.CommandText =  На случай показа позиций заказанных у других поставщиков
@"select oh.writetime, ol.Code, ol.CodeCr, s.Synonym, sfc.Synonym as Firm, ol.Quantity, ol.Cost, col.ResultCost OurFirmCost,
	ol.Cost * ol.Quantity LostSumm
from orders.ordershead oh
  join orders.orderslist ol on ol.orderid = oh.rowid
  join usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
  left join usersettings.includeregulation ir on ir.IncludeClientCode = oh.clientcode
  join logs.CostOptimizationLogs col on 
        oh.writetime > col.LoggedOn and col.ProductId = ol.ProductId and col.ResultCost < col.SelfCost and 
		(col.ClientId = ?clientId or ?clientId = 0 or col.ClientId = ir.PrimaryClientCode) and col.ProducerId = ol.CodeFirmCr
  join farm.Synonym s on s.SynonymCode = ol.SynonymCode
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode
  join usersettings.CostOptimizationClients cl on cl.ClientId = oh.ClientCode
where (oh.clientcode = ?clientId or ?clientId = 0) and pd.FirmCode <> ?supplierId and ol.Junk = 0 
  and Date(oh.writetime) >= Date(?beginDate) and Date(oh.writetime) <= Date(?endDate)
group by ol.RowId
order by oh.writetime, ol.RowId;";
			e.DataAdapter.Fill(_dsReport, "LostOrders");*/

			command.CommandText =
@"select count(*), ifnull(sum(ol.Cost*ol.Quantity), 0) Summ
from " +
#if DEBUG
@"orders.ordershead oh
  join orders.orderslist ol on ol.orderid = oh.rowid " +
#else
@"ordersold.ordershead oh
  join ordersold.orderslist ol on ol.orderid = oh.rowid " +
#endif
 @"join usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
  join usersettings.CostOptimizationClients coc on coc.ClientId = oh.ClientCode
  join usersettings.CostOptimizationRules cor on cor.Id = coc.RuleId and cor.SupplierId = ?supplierId
where (oh.clientcode = ?clientId or ?clientId = 0) and pd.FirmCode = ?supplierId and ol.Junk = 0 
and Date(oh.writetime) >= Date(?beginDate) and Date(oh.writetime) <= Date(?endDate);";
			e.DataAdapter.Fill(_dsReport, "Common");

			command.CommandText =
@"select count(*) Count, ifnull(round(avg(diff), 2), 0) Summ from CostOptimization
where diff > 0;";
			e.DataAdapter.Fill(_dsReport, "OverPrice");

			command.CommandText =
@"select count(*) Count, ifnull(round(avg(diff), 2), 0) Summ from CostOptimization
where diff < 0;";
			e.DataAdapter.Fill(_dsReport, "UnderPrice");

			command.CommandText =
@"select ifnull(round(sum(Quantity * (ResultCost - SelfCost)), 2), 0)
from CostOptimization
where diff > 0";
			e.DataAdapter.Fill(_dsReport, "Money");

			command.CommandText =
@"select ifnull(round(sum(Quantity * ResultCost), 2), 0)
from CostOptimization
where diff < 0";
			e.DataAdapter.Fill(_dsReport, "Volume");

			command.CommandText =
@"select * from CostOptimization order by WriteTime;";
			e.DataAdapter.Fill(_dsReport, "Temp");

			if(_clientId != 0)
			{
                command.CommandText =
                @"select concat(cl.Name, ' (', reg.Region, ')'), 1
    from future.Clients cl
         join farm.Regions reg on reg.RegionCode = cl.RegionCode
   where Id = ?clientId";
				e.DataAdapter.Fill(_dsReport, "Client");
			}
			_optimizedCount = _dsReport.Tables["Temp"].Rows.Count;

			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("writetime", typeof(DateTime));

			if (_clientId == 0)
				dtRes.Columns.Add("ClientName");
			if (_clientId == 0 || Convert.ToBoolean(_dsReport.Tables["Client"].Rows[0][1]))
				dtRes.Columns.Add("UserName");

			dtRes.Columns.Add("Code");
			dtRes.Columns.Add("CodeCr");
			dtRes.Columns.Add("Synonym");
			dtRes.Columns.Add("Firm");
			dtRes.Columns.Add("Quantity", typeof(int));
			dtRes.Columns.Add("SelfCost", typeof(decimal));
			dtRes.Columns.Add("ResultCost", typeof(decimal));
			dtRes.Columns.Add("absDiff", typeof(decimal));
			dtRes.Columns.Add("diff", typeof(double));
			dtRes.Columns.Add("EkonomEffect", typeof(decimal));
			dtRes.Columns.Add("IncreaseSales", typeof(decimal));

			// Добавляем пустые строки для заголовка
			for (int i = 0; i < 7; i++)
				dtRes.Rows.Add(dtRes.NewRow());

			foreach (DataRow row in _dsReport.Tables["Temp"].Rows)
			{
				var newRow = dtRes.NewRow();
				newRow["writetime"] = row["writetime"];
				//если строим отчет для всех клиентов или для новых
				if (_clientId == 0)
					newRow["ClientName"] = row["ClientName"];
				if (_clientId == 0 || Convert.ToBoolean(_dsReport.Tables["Client"].Rows[0][1]))
					newRow["UserName"] = row["UserName"];

				newRow["Code"] = row["Code"];
				newRow["CodeCr"] = row["CodeCr"];
				newRow["Synonym"] = row["Synonym"];
				newRow["Firm"] = row["Firm"];
				newRow["Quantity"] = row["Quantity"];
				newRow["SelfCost"] = row["SelfCost"];
				newRow["ResultCost"] = row["ResultCost"];
				newRow["absDiff"] = row["absDiff"];
				newRow["diff"] = row["diff"];
				newRow["EkonomEffect"] = row["EkonomEffect"];
				newRow["IncreaseSales"] = row["IncreaseSales"];
				dtRes.Rows.Add(newRow);
			}

			/*   На случай показа позиций заказанных у других поставщиков
			for (int i = 0; i < 7; i++)
				dtRes.Rows.Add(dtRes.NewRow());

			foreach (DataRow row in _dsReport.Tables["LostOrders"].Rows)
			{
				var newRow = dtRes.NewRow();
				newRow["writetime"] = row["writetime"];
				newRow["Code"] = row["Code"];
				newRow["CodeCr"] = row["CodeCr"];
				newRow["Synonym"] = row["Synonym"];
				newRow["Firm"] = row["Firm"];
				newRow["Quantity"] = row["Quantity"];
				newRow["SelfCost"] = row["Cost"];
				newRow["ResultCost"] = row["OurFirmCost"];
				newRow["absDiff"] = row["LostSumm"];
				
				dtRes.Rows.Add(newRow);
			}*/

			_dsReport.Tables.Add(dtRes);

		}

		public override void ReadReportParams()
		{
			if(_reportParams.ContainsKey("ClientCode"))
				_clientId = (int)_reportParams["ClientCode"];
			if (_reportParams.ContainsKey("FirmCode"))
				_supplierId = (int) _reportParams["FirmCode"];
			if (_supplierId == 0)
				_supplierId = 5; // Если не выбрали поставщика, то считаем что это Протек

			_reportInterval = (int)getReportParam("ReportInterval");
			_byPreviousMonth = (bool)getReportParam("ByPreviousMonth");
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if(format != ReportFormats.Excel)
				return null;
			//return new OptimizationEfficiencyNativeExcelWriter();
			return new OptimizationEfficiencyOleExcelWriter();
		}

		protected override BaseReportSettings GetSettings()
		{
			return new OptimizationEfficiencySettings(_reportCode, _reportCaption, _beginDate, _endDate, 
				_clientId, _optimizedCount);
		}
	}
}
