using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelLibrary.SpreadSheet;
using System.Data;
using MySql.Data.Types;

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

			curCol = col;
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
	}
}
