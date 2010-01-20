using System;
using MySql.Data.MySqlClient;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	public class OptimizationEfficiency : BaseReport
	{
		private DateTime _beginDate;
		private DateTime _endDate;
		private int _clientId;
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
select s.Synonym, sfc.Synonym as Firm, ol.Quantity, col.SelfCost, col.ResultCost,
	round(col.ResultCost - col.SelfCost, 2) absDiff, round((col.ResultCost / col.SelfCost - 1) * 100, 2) diff
from orders.ordershead oh
  join orders.orderslist ol on ol.orderid = oh.rowid
    left join logs.CostOptimizationLogs col on 
        oh.writetime > col.LoggedOn and col.ProductId = ol.ProductId and ol.Cost = col.ResultCost and col.ClientId = ?clientId
    join farm.Synonym s on s.SynonymCode = ol.SynonymCode
    join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode
where oh.clientcode = ?clientId and oh.pricecode = 4596 and ol.Junk = 0 
  and Date(oh.writetime) >= Date(?beginDate) and Date(oh.writetime) <= Date(?endDate)
  and col.Id is not null
group by ol.RowId
order by oh.writetime, ol.RowId;";

			_beginDate = DateTime.Now.AddDays(-13); // Находим прошлую неделю 
			while (_beginDate.DayOfWeek != DayOfWeek.Monday) _beginDate = _beginDate.AddDays(1); // с понедельника 
			_endDate = _beginDate.AddDays(6); // по воскресенье

			command.Parameters.AddWithValue("?beginDate", _beginDate);
			command.Parameters.AddWithValue("?endDate", _endDate);
			command.Parameters.AddWithValue("?clientId", _clientId);
			command.ExecuteNonQuery();

			command.CommandText =
@"select count(*)
from orders.ordershead oh
  join orders.orderslist ol on ol.orderid = oh.rowid
where oh.clientcode = ?clientId and oh.pricecode = 4596 and ol.Junk = 0 
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
@"select round(sum(Quantity * (ResultCost - SelfCost)), 2)
from CostOptimization
where diff > 0";
			e.DataAdapter.Fill(_dsReport, "Money");

			command.CommandText =
@"select round(sum(Quantity * ResultCost))
from CostOptimization
where diff < 0";
			e.DataAdapter.Fill(_dsReport, "Volume");

			command.CommandText =
@"select * from CostOptimization;";
			e.DataAdapter.Fill(_dsReport, "Temp");

			_optimizedCount = _dsReport.Tables["Temp"].Rows.Count;

			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("Synonym");
			dtRes.Columns.Add("Firm");
			dtRes.Columns.Add("Quantity", typeof(int));
			dtRes.Columns.Add("SelfCost", typeof(decimal));
			dtRes.Columns.Add("ResultCost", typeof(decimal));
			dtRes.Columns.Add("absDiff", typeof(decimal));
			dtRes.Columns.Add("diff", typeof(double));

			// Добавляем пустые строки для заголовка
			for (int i = 0; i < 6; i++ )
				dtRes.Rows.Add(dtRes.NewRow());

			foreach (DataRow row in _dsReport.Tables["Temp"].Rows)
			{
				var newRow = dtRes.NewRow();
				newRow["Synonym"] = row["Synonym"];
				newRow["Firm"] = row["Firm"];
				newRow["Quantity"] = row["Quantity"];
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
			_clientId = (int)getReportParam("ClientCode");			
			if(_clientId == 0)
				throw new ReportException(
					String.Format("В {0} (код отчета {1}) не задан клиент по данным которого строиться отчет.", 
						_reportCaption, _reportCode));
		}

		protected override void FormatExcel(string fileName)
		{
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				MSExcel.Workbook wb = exApp.Workbooks.Open(fileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try
				{
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

					try
					{
						ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);

						ws.Cells[1, 1] = String.Format("Статистика оптимизации цен за период с {0} по {1}", 
							_beginDate.ToString("dd.MM.yyyy"),
							_endDate.ToString("dd.MM.yyyy"));
						((MSExcel.Range) ws.Cells[1, 1]).Font.Bold = true;
						((MSExcel.Range)ws.Cells[1, 1]).HorizontalAlignment = MSExcel.XlHAlign.xlHAlignCenter;

						ws.Cells[2, 1] = String.Format("Всего заказано {0} позиций из них цены оптимизированы у {1}",
							_dsReport.Tables["Common"].Rows[0][0], _optimizedCount);

						ws.Cells[3, 1] = String.Format("Цены завышены у {0} позиции в среднем на {1}%",
							_dsReport.Tables["OverPrice"].Rows[0]["Count"],
							_dsReport.Tables["OverPrice"].Rows[0]["Summ"]);

						ws.Cells[4, 1] = String.Format("Цены занижены у {0} позиции в среднем на {1}%",
							_dsReport.Tables["UnderPrice"].Rows[0]["Count"],
							_dsReport.Tables["UnderPrice"].Rows[0]["Summ"]);

						ws.Cells[5, 1] = String.Format("Суммарный экономический эффект {0}",
							_dsReport.Tables["Money"].Rows[0][0]);

						ws.Cells[6, 1] = String.Format("Суммарное увеличение продаж {0}",
							_dsReport.Tables["Volume"].Rows[0][0]);

						//Форматируем заголовок отчета
						ws.Cells[7, 1] = "Наименование";
						((MSExcel.Range)ws.Cells[7, 1]).ColumnWidth = 30;
						((MSExcel.Range) ws.Cells[7, 1]).Font.Bold = true;
						ws.Cells[7, 2] = "Производитель";
						((MSExcel.Range)ws.Cells[7, 2]).ColumnWidth = 20;
						((MSExcel.Range)ws.Cells[7, 2]).Font.Bold = true;
						ws.Cells[7, 3] = "Количество";
						((MSExcel.Range)ws.Cells[7, 3]).ColumnWidth = 15;
						((MSExcel.Range)ws.Cells[7, 3]).Font.Bold = true;
						ws.Cells[7, 4] = "Исходная цена";
						((MSExcel.Range)ws.Cells[7, 4]).ColumnWidth = 18;
						((MSExcel.Range)ws.Cells[7, 4]).Font.Bold = true;
						ws.Cells[7, 5] = "Результирующая цена";
						((MSExcel.Range)ws.Cells[7, 5]).ColumnWidth = 27;
						((MSExcel.Range)ws.Cells[7, 5]).Font.Bold = true;
						ws.Cells[7, 6] = "Разница (руб.)";
						((MSExcel.Range)ws.Cells[7, 6]).ColumnWidth = 19;
						((MSExcel.Range)ws.Cells[7, 6]).Font.Bold = true;
						ws.Cells[7, 7] = "Разница (%)";
						((MSExcel.Range)ws.Cells[7, 7]).ColumnWidth = 16;
						((MSExcel.Range)ws.Cells[7, 7]).Font.Bold = true;

						((MSExcel.Range) ws.Cells[1, 7]).Clear();
						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[7, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[7, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//Объединяем несколько ячеек, чтобы в них написать текст
						((MSExcel.Range)ws.get_Range("A1:E1", System.Reflection.Missing.Value)).Select();
						((MSExcel.Range)exApp.Selection).Merge(null);
					}
					finally
					{
						wb.SaveAs(fileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, MSExcel.XlSaveConflictResolution.xlLocalSessionChanges, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
					}
				}
				finally
				{
					ws = null;
					wb = null;
					try { exApp.Workbooks.Close(); }
					catch { }
				}
			}
			finally
			{
				try { exApp.Quit(); }
				catch { }
				exApp = null;
			}
		}
	}
}
