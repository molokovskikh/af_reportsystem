﻿using System;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Data;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem.Writers
{
	public class OptimizationEfficiencyExcludeOleExcelWriter : BaseExcelWriter, IWriter
	{
		public void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
		{
			var optimizationSettings = (OptimizationEfficiencySettings)settings;

			_reportCode = optimizationSettings.ReportCode;
			_reportCaption = optimizationSettings.ReportCaption;
			_beginDate = optimizationSettings.BeginDate;
			_endDate = optimizationSettings.EndDate;
			_clientId = optimizationSettings.ClientId;
			_concurents = optimizationSettings.Concurents;
			_supplierName = optimizationSettings.SupplierName;

			DataTableToExcel(reportData.Tables["Results"], fileName, settings.ReportCode);
			FormatExcel(reportData, fileName);
		}

		private int _clientId;
		private ulong _reportCode;
		private string _reportCaption;
		private DateTime _beginDate;
		private DateTime _endDate;
		private string _concurents;
		private string _supplierName;

		private void FormatExcel(DataSet dsReport, string fileName)
		{
			var row = 1;
			ExcelHelper.Workbook(fileName, b => {
				var exApp = b.Application;
				var wb = b;
				var ws = ExcelHelper.GetSheet(wb, _reportCode);

				ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);

				ws.Cells[row, 1] = String.Format("Заказы конкурирующим поставщикам {2} за период с {0} по {1} включительно",
					_beginDate.ToString("dd.MM.yyyy"),
					_endDate.ToString("dd.MM.yyyy"),
					(_clientId != 0) ? "для клиента " + Convert.ToString(dsReport.Tables["Client"].Rows[0][0]) :
						"для всех клиентов");
				((MSExcel.Range)ws.Cells[row, 1]).Font.Bold = true;
				((MSExcel.Range)ws.Cells[row++, 1]).HorizontalAlignment = MSExcel.XlHAlign.xlHAlignCenter;

				ws.Cells[row++, 1] = String.Format("Оптимизация проводится для {0}", _supplierName);

				ws.Cells[row++, 1] = String.Format("Оптимизация проводится по следующим поставщикам-конкурентам: {0}", _concurents);

				ws.Cells[row++, 1] = String.Format("Всего заказано поставщикам-конкурентам, указанным в данном отчете, " +
					"товаров, по которым у {2} цены были ниже цены закупки: {0} позиций на сумму {1} руб.",
					dsReport.Tables["Common"].Rows[0][0],
					Convert.ToDouble(dsReport.Tables["Common"].Rows[0][1]).ToString("### ### ### ##0.00"),
					_supplierName);

				ws.Cells[row++, 1] = String.Format("Средняя разница цен составляет {0} руб. ({1}%)",
					dsReport.Tables["AvgDiff"].Rows[0]["SummAbs"],
					dsReport.Tables["AvgDiff"].Rows[0]["Summ"]);

				ws.Cells[row++, 1] = String.Format("Общий объем заказа в исходных ценах {1}: {0} руб.",
					dsReport.Tables["OrderVolume"].Rows[0]["Summ"],
					_supplierName);
				row++;
				int col = 1;
				//Форматируем заголовок отчета
				((MSExcel.Range)ws.Cells[row, col]).RowHeight = 25;

				ws.Cells[row, col] = "Дата";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 18;

				if (_clientId == 0) {
					ws.Cells[row, col] = "Аптека";
					((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 18;
				}

				ws.Cells[row, col] = "Адрес";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 18;

				if (_clientId == 0 || Convert.ToBoolean(dsReport.Tables["Client"].Rows[0][1])) {
					ws.Cells[row, col] = "Пользователь";
					((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 18;
				}

				ws.Cells[row, col] = "Код товара";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 11.5;

				ws.Cells[row, col] = "Код производителя";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 17;

				ws.Cells[row, col] = "Наименование";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 30;

				ws.Cells[row, col] = "Производитель";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 25;

				ws.Cells[row, col] = "Количество";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 17;

				ws.Cells[row, col] = "Исходная цена (руб.)";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 17;

				ws.Cells[row, col] = "Цена заказа у другого поставщика (руб.)";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 22;

				ws.Cells[row, col] = "Оптимизированная цена (руб.)";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 17;

				ws.Cells[row, col] = "Разница (руб.)";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 11;

				ws.Cells[row, col] = "Разница (%)";
				((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 11;

				for (int i = 1; i <= col; i++) {
					((MSExcel.Range)ws.Cells[row, i]).WrapText = true;
					((MSExcel.Range)ws.Cells[row, i]).Font.Bold = true;
					((MSExcel.Range)ws.Cells[row, i]).HorizontalAlignment = MSExcel.XlHAlign.xlHAlignCenter;
				}

				int lastRow = dsReport.Tables["Results"].Rows.Count + 2;

				((MSExcel.Range)ws.Cells[1, 7]).Clear();
				//рисуем границы на всю таблицу
				ws.get_Range(ws.Cells[row, 1], ws.Cells[dsReport.Tables["Results"].Rows.Count + 2, dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

				ws.Activate();

				//Устанавливаем АвтоФильтр на все колонки
				ws.get_Range(ws.Cells[row, 1], ws.Cells[dsReport.Tables["Results"].Rows.Count + 1, dsReport.Tables["Results"].Columns.Count]).Select();
				((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

				//Объединяем несколько ячеек, чтобы в них написать текст
				ws.get_Range("A1:N1", System.Reflection.Missing.Value).Select();
				((MSExcel.Range)exApp.Selection).Merge(null);

				// объединяем Итого
				ws.get_Range(ws.Cells[dsReport.Tables["Results"].Rows.Count + 2, 1], ws.Cells[dsReport.Tables["Results"].Rows.Count + 2, dsReport.Tables["Results"].Columns.Count]).Merge(null);
			});
		}
	}
}