using System;
using MySql.Data.MySqlClient;
using System.Data;
using ExcelLibrary.SpreadSheet;
using System.IO;
using Inforoom.ReportSystem.Helpers;

namespace Inforoom.ReportSystem
{
	public class OptimizationEfficiency : BaseReport
	{
		private DateTime _beginDate;
		private DateTime _endDate;
		private int _clientId;
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
select col.LoggedOn, s.Synonym, ol.Code, sfc.Synonym as Firm, ol.Quantity, col.SelfCost, col.ResultCost,
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
@"select * from CostOptimization;";
			e.DataAdapter.Fill(_dsReport, "Results");

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
		}

		public override void ReadReportParams()
		{
			_clientId = (int)getReportParam("ClientCode");
			_reportInterval = (int)getReportParam("ReportInterval");
			_byPreviousMonth = (bool)getReportParam("ByPreviousMonth");
		}

		protected override void DataTableToExcel(DataTable dtExport, string ExlFileName)
		{
			dtExport.Columns[0].Caption = "Дата";
			dtExport.Columns[1].Caption = "Наименование";
			dtExport.Columns[2].Caption = "Код товара";
			dtExport.Columns[3].Caption = "Производитель";
			dtExport.Columns[4].Caption = "Количество";
			dtExport.Columns[5].Caption = "Исходная цена (руб.)";
			dtExport.Columns[6].Caption = "Результирующая цена (руб.)";
			dtExport.Columns[7].Caption = "Разница (руб.)";
			dtExport.Columns[8].Caption = "Разница (%)";
			dtExport.Columns[9].Caption = "Экономический эффект (руб.)";
			dtExport.Columns[10].Caption = "Увеличение продаж (руб.)";

			_optimizedCount = dtExport.Rows.Count;

			Workbook book;
			if (File.Exists(ExlFileName))
				book = Workbook.Load(ExlFileName);
			else
				book = new Workbook();			

			var ws = new Worksheet(_reportCaption);
			book.Worksheets.Add(ws);

			int row = 0;

			ws.Merge(row, 0, row, dtExport.Columns.Count - 1);
			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Статистика оптимизации цен {2} за период с {0} по {1}",
					_beginDate.ToString("dd.MM.yyyy"),
					_endDate.ToString("dd.MM.yyyy"),
                    (_clientId != 0) ?
						"для клиента "  + Convert.ToString(_dsReport.Tables["Client"].Rows[0][0]) :
                        "для всех клиентов"),
					ExcelHelper.HeaderStyle);
			row++;

			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Всего заказано {0} позиций на сумму {1} руб. из них цены оптимизированы у {2}",
							_dsReport.Tables["Common"].Rows[0][0],
							_dsReport.Tables["Common"].Rows[0][1],
							_optimizedCount), ExcelHelper.PlainStyle);
			row++;

			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Цены завышены у {0} позиции в среднем на {1}%",
					_dsReport.Tables["OverPrice"].Rows[0]["Count"],
					_dsReport.Tables["OverPrice"].Rows[0]["Summ"]), ExcelHelper.PlainStyle);
			row++;

			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Суммарный экономический эффект {0} руб.",
					_dsReport.Tables["Money"].Rows[0][0]), ExcelHelper.PlainStyle);
			row++;

			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Цены занижены у {0} позиции в среднем на {1}%",
					_dsReport.Tables["UnderPrice"].Rows[0]["Count"],
					_dsReport.Tables["UnderPrice"].Rows[0]["Summ"]), ExcelHelper.PlainStyle);
			row++;

			double percent = Math.Round(Convert.ToDouble(_dsReport.Tables["Volume"].Rows[0][0]) /
				Convert.ToDouble(_dsReport.Tables["Common"].Rows[0][1]) * 100, 2);
			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Суммарное увеличение продаж {0} руб. ({1}%)",
					_dsReport.Tables["Volume"].Rows[0][0],
					percent), ExcelHelper.PlainStyle);
			row++;

			ExcelHelper.WriteDataTable(ws,row, 0, dtExport, true);
			row += dtExport.Rows.Count + 1;			
			ExcelHelper.WriteCell(ws, row, 0, "Итого:", ExcelHelper.TableHeader);
			for (int i = 1; i < 9; i++)
				ExcelHelper.WriteCell(ws, row, i, null, ExcelHelper.TableHeader);
			ExcelHelper.WriteCell(ws, row, 9, _dsReport.Tables["Money"].Rows[0][0], ExcelHelper.TableHeader);
			ExcelHelper.WriteCell(ws, row, 10, _dsReport.Tables["Volume"].Rows[0][0], ExcelHelper.TableHeader);
			ws.Merge(row, 0, row, 8);

			ExcelHelper.SetColumnsWidth(ws, 4000, 8000, 3000, 6000, 3000, 3000, 4300, 3000, 3000, 4000, 3100);
			book.Save(ExlFileName);
		}

		protected override void FormatExcel(string fileName)
		{}
	}
}
