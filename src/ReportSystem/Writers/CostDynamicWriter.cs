﻿using System;
using System.Data;
using Inforoom.ReportSystem.ByOffers;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using Microsoft.Office.Interop.Excel;
using XlBorderWeight = Microsoft.Office.Core.XlBorderWeight;

namespace Inforoom.ReportSystem.Writers
{
	public class CostDynamicWriter : BaseExcelWriter
	{
		public override void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings reportSettings)
		{
			var settings = (CostDynamicSettings)reportSettings;
			var results = reportData.Tables["Results"];
			results.Columns.Remove("Id");
			DataTableToExcel(reportData.Tables["Results"], fileName, settings.ReportCode);

			ExcelHelper.Workbook(fileName, b => {
				var sheet = ExcelHelper.GetSheet(b, settings.ReportCode);
				sheet.Name = reportSettings.ListName;
				sheet.Activate();
				var row = 0;
				foreach (var filter in settings.Filters) {
					ExcelHelper.Header(sheet, row, 10, filter);
					row++;
				}

				((Range)sheet.Cells[row + 1, 1]).Select();
				var sheetRow = ((Range)sheet.Application.Selection).EntireRow;
				sheetRow.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);

				((Range)sheet.Cells[row + 1, 2]).Value2 = settings.Date.ToShortDateString();
				for (var i = 0; i < settings.Dates.Count; i++)
					ExcelHelper.Merge(sheet, row, (i + 1) * 2, 2, settings.Dates[i].Label);

				row++;
				MakeHeder(row, sheet, 45);

				row++;
				ExcelHelper.FormatHeader(sheet, row, results);
				MakeHeder(row, sheet);

				var tableBegin = settings.Filters.Count + 1;
				var tableHeaderSize = 2;
				var count = results.Rows.Count;
				var columnCount = results.Columns.Count;
				var tableEnd = tableBegin + count + 1;

				var range = sheet.get_Range(sheet.Cells[tableBegin, 1], sheet.Cells[tableEnd, columnCount]);
				range.Borders.Weight = XlBorderWeight.xlThin;

				sheet.get_Range(sheet.Cells[tableBegin + tableHeaderSize, 2], sheet.Cells[tableEnd, columnCount]).NumberFormat = "0.00%";
			});
		}

		private static void MakeHeder(int row, _Worksheet sheet, int height = 27)
		{
			var sheetRow = sheet.get_Range(sheet.Cells[row, 1], sheet.Cells[row, 1]).EntireRow;
			sheetRow.HorizontalAlignment = XlHAlign.xlHAlignLeft;
			sheetRow.VerticalAlignment = XlVAlign.xlVAlignTop;
			sheetRow.WrapText = true;
			sheetRow.RowHeight = height;
		}
	}
}