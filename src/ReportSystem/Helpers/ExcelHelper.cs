﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using ExcelLibrary.SpreadSheet;
using System.Data;
using Microsoft.Office.Interop.Excel;
using MySql.Data.Types;
using Borders = ExcelLibrary.SpreadSheet.Borders;
using CellFormat = ExcelLibrary.SpreadSheet.CellFormat;
using DataTable = System.Data.DataTable;
using Font = ExcelLibrary.SpreadSheet.Font;
using Workbook = Microsoft.Office.Interop.Excel.Workbook;
using Worksheet = ExcelLibrary.SpreadSheet.Worksheet;

namespace Inforoom.ReportSystem.Helpers
{
	public static class ExcelHelper
	{
		public static Borders FullBordered = new Borders {
			Bottom = BorderStyle.Thin,
			Left = BorderStyle.Thin,
			Right = BorderStyle.Thin,
			Top = BorderStyle.Thin
		};

		public static Font FontSmall = new Font("MS Sans Serif", 10);
		public static Font FontBold = new Font("MS Sans Serif", 10) { Bold = true };

		public static CellStyle TableHeader = new CellStyle {
			Borders = FullBordered,
			Font = FontBold,
			Warp = true,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		public static CellStyle TableCell = new CellStyle {
			Borders = FullBordered,
			Font = FontSmall,
		};

		public static CellStyle HeaderStyle = new CellStyle {
			Font = FontBold,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		public static CellStyle PlainStyle = new CellStyle
		{
			Font = FontSmall,
		};

		public static CellFormat DateCellFormat = new CellFormat(CellFormatType.DateTime, "dd.mm.yyyy HH:MM:SS");
		

		public static void WriteCell(Worksheet ws, int row, int col, object value, CellStyle style)
		{
			object temp = null;
			if (value is MySqlDateTime)
				temp = ((MySqlDateTime)value).GetDateTime();
			else
				temp = value;

			if (temp != null && temp != DBNull.Value)
				if (temp is DateTime)
				{
					ws.Cells[row, col] = new Cell(temp);
					ws.Cells[row, col].Format = DateCellFormat;
				}
				else
					ws.Cells[row, col] = new Cell(temp);
			else
				ws.Cells[row, col] = new Cell(String.Empty);

			if (style != null)
				ws.Cells[row, col].Style = style;
		}

		public static void WriteDataTable(Worksheet ws, int row, int col, DataTable table, bool writeHeaders)
		{
			int curCol = col;

			if (writeHeaders)
			{
				foreach (DataColumn column in table.Columns)
				{
					WriteCell(ws, row, curCol, column.Caption, TableHeader);
					curCol++;
				}
				row++;
			}

			foreach (DataRow curRow in table.Rows)
			{
				curCol = col;
				foreach (DataColumn column in table.Columns)
				{
					WriteCell(ws, row, curCol, curRow[column], TableCell);
					curCol++;
				}
				row++;
			}
		}

		public static void SetColumnsWidth(Worksheet ws, params ushort[] widths)
		{
			for (ushort i = 0; i < widths.Length; i++)
				ws.Cells.ColumnWidth[i] = widths[i];
		}

		public static int PutHeader(_Worksheet ws, int beginRow, int columnCount, string message)
		{
			((Range) ws.Cells[beginRow + 1, 1]).Select();
			var row = ((Range) ws.Application.Selection).EntireRow;
			row.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);
			row.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);
			row.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);

			beginRow += 3;
			var range = ws.Range[
				ws.Cells[beginRow - 3, 1], 
				ws.Cells[beginRow - 1, columnCount]];
			range.Select();
			((Range)ws.Application.Selection).Merge();
			var activeCell = ws.Application.ActiveCell;
			activeCell.FormulaR1C1 = message;
			activeCell.WrapText = true;
			activeCell.HorizontalAlignment = XlHAlign.xlHAlignLeft;
			activeCell.VerticalAlignment = XlVAlign.xlVAlignTop;
			return beginRow;
		}

		public static void Header(_Worksheet ws, int beginRow, int columnCount, string message)
		{
			((Range) ws.Cells[beginRow + 1, 1]).Select();
			var row = ((Range) ws.Application.Selection).EntireRow;
			row.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);

			Merge(ws, beginRow, 0, columnCount, message);
		}

		public static void Merge(_Worksheet ws, int beginRow, int beginColumn, int columnCount, string message)
		{
			var range = ws.Range[
				ws.Cells[beginRow + 1, beginColumn + 1],
				ws.Cells[beginRow + 1, beginColumn + columnCount]];
			range.Merge();
			range.FormulaR1C1 = message;
			range.WrapText = true;
			range.HorizontalAlignment = XlHAlign.xlHAlignLeft;
			range.VerticalAlignment = XlVAlign.xlVAlignTop;
		}

		public static _Worksheet GetSheet(Workbook wb, ulong reportId)
		{
			return (_Worksheet)wb.Worksheets["rep" + reportId.ToString()];
		}

		public static void FormatHeader(_Worksheet sheet, int row, DataTable table)
		{
			for (var i = 0; i < table.Columns.Count; i++)
			{
				sheet.Cells[row, i + 1] = "";
				sheet.Cells[row, i + 1] = table.Columns[i].Caption;
				if (table.Columns[i].ExtendedProperties.ContainsKey("Width"))
					((Range)sheet.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)table.Columns[i].ExtendedProperties["Width"]).Value;
				else
					((Range)sheet.Columns[i + 1, Type.Missing]).AutoFit();
				if (table.Columns[i].ExtendedProperties.ContainsKey("Color"))
					sheet.get_Range(sheet.Cells[row, i + 1], sheet.Cells[table.Rows.Count + 1, i + 1]).Interior.Color = ColorTranslator.ToOle((Color)table.Columns[i].ExtendedProperties["Color"]);
			}
		}
	}
}
