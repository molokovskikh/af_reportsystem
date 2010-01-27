﻿using System;
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
select oh.writetime, ol.Code, ol.CodeCr, s.Synonym, sfc.Synonym as Firm, ol.Quantity, col.SelfCost, col.ResultCost,
	round(col.ResultCost - col.SelfCost, 2) absDiff, round((col.ResultCost / col.SelfCost - 1) * 100, 2) diff,
    CASE WHEN col.ResultCost > col.SelfCost THEN (col.ResultCost - col.SelfCost)*ol.Quantity ELSE null END EkonomEffect,
	CASE WHEN col.ResultCost < col.SelfCost THEN col.ResultCost*ol.Quantity ELSE null END IncreaseSales
from orders.ordershead oh
  join orders.orderslist ol on ol.orderid = oh.rowid
  join usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
  join logs.CostOptimizationLogs col on 
        oh.writetime > col.LoggedOn and col.ProductId = ol.ProductId and ol.Cost = col.ResultCost and (col.ClientId = ?clientId or ?clientId = 0)
    join farm.Synonym s on s.SynonymCode = ol.SynonymCode
    join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode
where (oh.clientcode = ?clientId or ?clientId = 0) and pd.FirmCode = ?supplierId and ol.Junk = 0 
  and Date(oh.writetime) >= Date(?beginDate) and Date(oh.writetime) <= Date(?endDate)
group by ol.RowId
order by oh.writetime, ol.RowId;";

			_endDate = DateTime.Now;
			if (_byPreviousMonth) // Определяем интервал построения отчета
			{
				_endDate = _endDate.AddDays(-_endDate.Day); // Последний день прошлого месяца
				_beginDate = _endDate.AddMonths(-1).AddDays(1);
			}
			else
				_beginDate = _endDate.AddDays(-_reportInterval);

			command.Parameters.AddWithValue("?beginDate", _beginDate);
			command.Parameters.AddWithValue("?endDate", _endDate);
			command.Parameters.AddWithValue("?clientId", _clientId);
			command.Parameters.AddWithValue("?supplierId", 5);
			command.ExecuteNonQuery();

			command.CommandText =
@"select count(*), ifnull(sum(ol.Cost*ol.Quantity), 0) Summ
from orders.ordershead oh
  join orders.orderslist ol on ol.orderid = oh.rowid
  join usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
where (oh.clientcode = ?clientId or ?clientId = 0) and pd.FirmCode = ?supplierId and ol.Junk = 0 
and Date(oh.writetime) >= Date(?beginDate) and Date(oh.writetime) <= Date(?endDate);";
			e.DataAdapter.Fill(_dsReport, "Common");

			command.CommandText =
@"select count(*) Count, round(avg(diff), 2) Summ from CostOptimization
where diff > 0;";
			e.DataAdapter.Fill(_dsReport, "OverPrice");

			command.CommandText =
@"select count(*) Count, round(avg(diff), 2) Summ from CostOptimization
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
@"select concat(cd.ShortName, ' (', reg.Region, ')')
    from usersettings.ClientsData cd
         join farm.Regions reg on reg.RegionCode = cd.RegionCode
   where FirmCode = ?clientId
  union
  select concat(cl.Name, ' (', reg.Region, ')')
    from future.Clients cl
         join farm.Regions reg on reg.RegionCode = cl.RegionCode
   where Id = ?clientId";
				e.DataAdapter.Fill(_dsReport, "Client");
			}

			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("writetime", typeof(DateTime));
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

			_dsReport.Tables.Add(dtRes);

		}

		public override void ReadReportParams()
		{
			if(_reportParams.ContainsKey("ClientCode"))
				_clientId = (int)_reportParams["ClientCode"];

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

		protected override BaseReportSettings GetSettings(IWriter writer)
		{
			return new OptimizationEfficiencySettings(_reportCode, _reportCaption, _beginDate, _endDate, _clientId);
		}
	}
}
