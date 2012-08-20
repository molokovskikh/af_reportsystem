using System;
using System.Data;
using System.Linq;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using Microsoft.Office.Interop.Excel;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem.Writers
{
	public class LeakOffersWriter : BaseExcelWriter, IWriter
	{
		public void WriteReportToFile(DataSet data, string file, BaseReportSettings settings)
		{
			foreach (var table in data.Tables.Cast<DataTable>().Where(t => t.TableName != "prices"))
				DataTableToExcel(table, file, table.TableName);

			MakePretty(data, file, settings);
		}

		private void MakePretty(DataSet data, string file, BaseReportSettings settings)
		{
			UseExcel.Workbook(file, b => {
				foreach (DataRow row in data.Tables["Prices"].Rows) {
					var sheet = b.Worksheets.Cast<_Worksheet>().FirstOrDefault(s => s.Name == row["PriceCode"].ToString());
					if (sheet == null)
						continue;
					var name = row["ShortName"].ToString() + " " + row["PriceName"].ToString();
					if (name.Length > 26)
						name = name.Substring(0, 26);
					sheet.Name = name;
					sheet.Cells[1, 1] = "Код";
					((Range)sheet.Cells[1, 1]).ColumnWidth = 11;
					sheet.Cells[1, 2] = "Код изготовителя";
					((Range)sheet.Cells[1, 2]).ColumnWidth = 11;
					sheet.Cells[1, 3] = "Наименование";
					((Range)sheet.Cells[1, 3]).ColumnWidth = 30;
					sheet.Cells[1, 4] = "Изготовитель";
					((Range)sheet.Cells[1, 4]).ColumnWidth = 25;
					sheet.Cells[1, 5] = "Цена";
					((Range)sheet.Cells[1, 5]).ColumnWidth = 15.5;
					sheet.Cells[1, 6] = "Остаток";
					((Range)sheet.Cells[1, 6]).ColumnWidth = 17;
					sheet.Cells[1, 7] = "Срок годности";
					((Range)sheet.Cells[1, 7]).ColumnWidth = 10;
					sheet.Cells[1, 8] = "Примечание";
					((Range)sheet.Cells[1, 8]).ColumnWidth = 20;

					var header = sheet.get_Range(sheet.Cells[1, 1], sheet.Cells[1, 8]);
					header.WrapText = true;
					header.Font.Bold = true;
					header.HorizontalAlignment = XlHAlign.xlHAlignCenter;
					var table = data.Tables[row["PriceCode"].ToString()];
					sheet.get_Range(sheet.Cells[1, 1], sheet.Cells[table.Rows.Count + 1, 8]).Borders.Weight = XlBorderWeight.xlThin;
				}
			});
		}
	}
}