using System;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Data;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem.Writers
{
	public class OptimizationEfficiencyOleExcelWriter : BaseExcelWriter, IWriter
	{
		public void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
		{
			var optimizationSettings = (OptimizationEfficiencySettings) settings;

			_reportCode = optimizationSettings.ReportCode;
			_reportCaption = optimizationSettings.ReportCaption;
			_beginDate = optimizationSettings.BeginDate;
			_endDate = optimizationSettings.EndDate;
			_clientId = optimizationSettings.ClientId;
			_optimizedCount = optimizationSettings.OptimizedCount;

			DataTableToExcel(reportData.Tables["Results"], fileName, settings.ReportCode);
			FormatExcel(reportData, fileName);
		}

		private int _clientId;
		private int _optimizedCount;
		private ulong _reportCode;
		private string _reportCaption;
		private DateTime _beginDate;
		private DateTime _endDate;

		private void FormatExcel(DataSet dsReport, string fileName)
		{
			int row = 1;
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

						ws.Cells[row, 1] = String.Format("Статистика оптимизации цен {2} за период с {0} по {1}",
							_beginDate.ToString("dd.MM.yyyy"),
							_endDate.ToString("dd.MM.yyyy"),
							(_clientId != 0) ?
								"для клиента " + Convert.ToString(dsReport.Tables["Client"].Rows[0][0]) :
								"для всех клиентов");
						((MSExcel.Range)ws.Cells[row, 1]).Font.Bold = true;
						((MSExcel.Range)ws.Cells[row++, 1]).HorizontalAlignment = MSExcel.XlHAlign.xlHAlignCenter;


						ws.Cells[row++, 1] = String.Format("Всего заказано {0} позиций на сумму {1} руб. из них цены оптимизированы у {2}",
										dsReport.Tables["Common"].Rows[0][0],
										Convert.ToDouble(dsReport.Tables["Common"].Rows[0][1]).ToString("### ### ### ##0.00"),
										_optimizedCount);

						ws.Cells[row++, 1] = String.Format("Цены завышены у {0} позиции в среднем на {1}%",
								dsReport.Tables["OverPrice"].Rows[0]["Count"],
								dsReport.Tables["OverPrice"].Rows[0]["Summ"]);

						ws.Cells[row++, 1] = String.Format("Суммарный экономический эффект {0} руб.",
								Convert.ToDouble(dsReport.Tables["Money"].Rows[0][0]).ToString("### ### ### ##0.00"));

						ws.Cells[row++, 1] = String.Format("Цены занижены у {0} позиции в среднем на {1}%",
								dsReport.Tables["UnderPrice"].Rows[0]["Count"],
								dsReport.Tables["UnderPrice"].Rows[0]["Summ"]);

						double percent = 0;
						double allCost = Convert.ToDouble(dsReport.Tables["Common"].Rows[0][1]);
						double cost = Convert.ToDouble(dsReport.Tables["Volume"].Rows[0][0]);
						if(allCost > 0)
							percent = Math.Round(cost/(allCost - cost) * 100, 2);
						ws.Cells[row++, 1] = String.Format("Суммарное увеличение продаж {0} руб. ({1}%)",
							Convert.ToDouble(dsReport.Tables["Volume"].Rows[0][0]).ToString("### ### ### ##0.00"),
							percent);
						row++;

						int col = 1;
						//Форматируем заголовок отчета
						((MSExcel.Range) ws.Cells[row, col]).RowHeight = 25;

						ws.Cells[row, col] = "Дата";
						((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 18;
						
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
						((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 15.5;

						ws.Cells[row, col] = "Результирующая цена (руб.)";
						((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 19;

						ws.Cells[row, col] = "Разница (руб.)";
						((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 11;

						ws.Cells[row, col] = "Разница (%)";
						((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 11;

						ws.Cells[row, col] = "Экономический эффект (руб.)";
						((MSExcel.Range)ws.Cells[row, col++]).ColumnWidth = 18;

						ws.Cells[row, col] = "Увеличение продаж (руб.)";
						((MSExcel.Range)ws.Cells[row, col]).ColumnWidth = 18;
										

						for(int i = 1; i <= col; i++)
						{
							((MSExcel.Range)ws.Cells[row, i]).WrapText = true;
							((MSExcel.Range)ws.Cells[row, i]).Font.Bold = true;
							((MSExcel.Range)ws.Cells[row, i]).HorizontalAlignment = MSExcel.XlHAlign.xlHAlignCenter;
						}


						int lastRow = _optimizedCount + 9;
						ws.Cells[lastRow, 1] = "Итого:";
						((MSExcel.Range)ws.Cells[lastRow, 1]).Font.Bold = true;

						ws.Cells[lastRow, 11] = dsReport.Tables["Money"].Rows[0][0];
						((MSExcel.Range)ws.Cells[lastRow, 11]).Font.Bold = true;

						ws.Cells[lastRow, 12] = dsReport.Tables["Volume"].Rows[0][0];
						((MSExcel.Range)ws.Cells[lastRow, 12]).Font.Bold = true;


						((MSExcel.Range)ws.Cells[1, 7]).Clear();
						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[row, 1], ws.Cells[_optimizedCount + 9, dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[row, 1], ws.Cells[_optimizedCount + 8, dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//Объединяем несколько ячеек, чтобы в них написать текст
						((MSExcel.Range)ws.get_Range("A1:L1", System.Reflection.Missing.Value)).Select();
						((MSExcel.Range)exApp.Selection).Merge(null);

						// объединяем Итого
						((MSExcel.Range)ws.get_Range(ws.Cells[_optimizedCount + 9, 1], ws.Cells[_optimizedCount + 9, dsReport.Tables["Results"].Columns.Count - 2])).Merge(null);

						/*
						row = lastRow+4;
						((MSExcel.Range)ws.get_Range(ws.Cells[row, 1], ws.Cells[row, 9])).Merge(null);
						ws.Cells[row, 1] = "Статистика заказов у конкурентов по оптимизированным позициям";
						((MSExcel.Range)ws.Cells[row, 1]).Font.Bold = true;

						row++; row++;
						col = 1;
						//Форматируем заголовок отчета2
						((MSExcel.Range)ws.Cells[row, col]).RowHeight = 25;

						ws.Cells[row, col++] = "Дата";
						ws.Cells[row, col++] = "Код товара";
						ws.Cells[row, col++] = "Код производителя";
						ws.Cells[row, col++] = "Наименование";
						ws.Cells[row, col++] = "Производитель";
						ws.Cells[row, col++] = "Количество";
						ws.Cells[row, col++] = "Цена конкурента(руб.)";
						ws.Cells[row, col++] = "Оптимизированная цена (руб.)";
						ws.Cells[row, col] = "Сумма (руб.)";
						for (int i = 1; i <= col; i++)
						{
							((MSExcel.Range)ws.Cells[row, i]).WrapText = true;
							((MSExcel.Range)ws.Cells[row, i]).Font.Bold = true;
							((MSExcel.Range)ws.Cells[row, i]).HorizontalAlignment = MSExcel.XlHAlign.xlHAlignCenter;
						}

						//рисуем границы на всю таблицу2
						ws.get_Range(ws.Cells[row, 1], ws.Cells[row + _lostCount + 1, 9]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[row, 1], ws.Cells[dsReport.Tables["Results"].Rows.Count+1, 9])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);*/

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
