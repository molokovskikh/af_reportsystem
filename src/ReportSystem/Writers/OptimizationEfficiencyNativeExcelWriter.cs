using System;
using System.Data;
using ExcelLibrary.SpreadSheet;
using System.IO;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem.Writers
{
	public class OptimizationEfficiencyNativeExcelWriter : IWriter
	{
		public void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
		{
			var optimizationSettins = (OptimizationEfficiencySettings) settings;

			var beginDate = optimizationSettins.BeginDate;
			var endDate = optimizationSettins.EndDate;
			var clientId = optimizationSettins.ClientId;
			var reportCaption = optimizationSettins.ReportCaption;

			var dtExport = reportData.Tables["Results"];

			dtExport.Columns[0].Caption = "Дата";
			dtExport.Columns[1].Caption = "Код товара";
			dtExport.Columns[2].Caption = "Код производителя";
			dtExport.Columns[3].Caption = "Наименование";
			dtExport.Columns[4].Caption = "Производитель";
			dtExport.Columns[5].Caption = "Количество";
			dtExport.Columns[6].Caption = "Исходная цена (руб.)";
			dtExport.Columns[7].Caption = "Результирующая цена (руб.)";
			dtExport.Columns[8].Caption = "Разница (руб.)";
			dtExport.Columns[9].Caption = "Разница (%)";
			dtExport.Columns[10].Caption = "Экономический эффект (руб.)";
			dtExport.Columns[11].Caption = "Увеличение продаж (руб.)";

			var optimizedCount = dtExport.Rows.Count;

			Workbook book;
			if (File.Exists(fileName))
				book = Workbook.Load(fileName);
			else
				book = new Workbook();

			var ws = new Worksheet(reportCaption);
			book.Worksheets.Add(ws);

			int row = 0;

			ws.Merge(row, 0, row, dtExport.Columns.Count - 1);
			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Статистика оптимизации цен {2} за период с {0} по {1}",
					beginDate.ToString("dd.MM.yyyy"),
					endDate.ToString("dd.MM.yyyy"),
					(clientId != 0) ?
						"для клиента " + Convert.ToString(reportData.Tables["Client"].Rows[0][0]) :
						"для всех клиентов"),
					ExcelHelper.HeaderStyle);
			row++;

			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Всего заказано {0} позиций на сумму {1} руб. из них цены оптимизированы у {2}",
							reportData.Tables["Common"].Rows[0][0],
							Convert.ToDouble(reportData.Tables["Common"].Rows[0][1]).ToString("### ### ### ##0.00"),
							optimizedCount), ExcelHelper.PlainStyle);
			row++;

			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Цены завышены у {0} позиции в среднем на {1}%",
					reportData.Tables["OverPrice"].Rows[0]["Count"],
					reportData.Tables["OverPrice"].Rows[0]["Summ"]), ExcelHelper.PlainStyle);
			row++;

			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Суммарный экономический эффект {0} руб.",
					Convert.ToDouble(reportData.Tables["Money"].Rows[0][0]).ToString("### ### ### ##0.00")), ExcelHelper.PlainStyle);
			row++;

			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Цены занижены у {0} позиции в среднем на {1}%",
					reportData.Tables["UnderPrice"].Rows[0]["Count"],
					reportData.Tables["UnderPrice"].Rows[0]["Summ"]), ExcelHelper.PlainStyle);
			row++;

			double percent = Math.Round(Convert.ToDouble(reportData.Tables["Volume"].Rows[0][0]) /
				Convert.ToDouble(reportData.Tables["Common"].Rows[0][1]) * 100, 2);
			ExcelHelper.WriteCell(ws, row, 0,
				String.Format("Суммарное увеличение продаж {0} руб. ({1}%)",
					Convert.ToDouble(reportData.Tables["Volume"].Rows[0][0]).ToString("### ### ### ##0.00"),
					percent), ExcelHelper.PlainStyle);
			row++; row++;

			ExcelHelper.WriteDataTable(ws, row, 0, dtExport, true);
			row += dtExport.Rows.Count + 1;

			ExcelHelper.WriteCell(ws, row, 0, "Итого:", ExcelHelper.TableHeader);
			for (int i = 1; i < 10; i++)
				ExcelHelper.WriteCell(ws, row, i, null, ExcelHelper.TableHeader);
			ExcelHelper.WriteCell(ws, row, 10, Convert.ToDouble(reportData.Tables["Money"].Rows[0][0]).ToString("### ### ### ##0.00"), ExcelHelper.TableHeader);
			ExcelHelper.WriteCell(ws, row, 11, Convert.ToDouble(reportData.Tables["Volume"].Rows[0][0]).ToString("### ### ### ##0.00"), ExcelHelper.TableHeader);
			ws.Merge(row, 0, row, 9);

			ExcelHelper.SetColumnsWidth(ws, 4000, 3000, 4000, 8000, 6000, 3000, 3000, 4300, 3000, 3000, 4000, 3100);
			book.Save(fileName);
		}
	}
}
