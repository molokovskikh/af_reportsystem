using System.Data;
using System.Linq;
using ExcelLibrary.SpreadSheet;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem.Writers
{
	public class LeakOffersNativeWriter : BaseExcelWriter, IWriter
	{
		public void WriteReportToFile(DataSet data, string file, BaseReportSettings settings)
		{
			var workbook = new Workbook();
			foreach (var row in data.Tables["prices"].Rows.Cast<DataRow>())
			{
				var offers = data.Tables[row["PriceCode"].ToString()];
				if (offers == null)
					continue;

				var name = row["ShortName"] + " " + row["PriceName"];
				if (name.Length > 26)
					name = name.Substring(0, 26);

				var header = new CellStyle {
					Warp = true,
					Font = new Font("Arial", 11) {
						Bold = true
					}, 
					HorizontalAlignment = HorizontalAlignment.Center
				};

				var body = new CellStyle {
					Borders = Borders.Box(BorderStyle.Thin)
				};

				var sheet = new Worksheet(name);

				sheet.Cells[0, 0] = new Cell("Код"){Style = header};
				sheet.Cells.ColumnWidth[0] = 11*255;
				sheet.Cells[0, 1] = new Cell("Код изготовителя"){Style = header};
				sheet.Cells.ColumnWidth[1] = 11*255;
				sheet.Cells[0, 2] = new Cell("Наименование"){Style = header};
				sheet.Cells.ColumnWidth[2] = 30*255;
				sheet.Cells[0, 3] = new Cell("Изготовитель"){Style = header};
				sheet.Cells.ColumnWidth[3] = 25*255;
				sheet.Cells[0, 4] = new Cell("Цена"){Style = header};
				sheet.Cells.ColumnWidth[4] = (ushort) (15.5*255);
				sheet.Cells[0, 5] = new Cell("Остаток"){Style = header};
				sheet.Cells.ColumnWidth[5] = 17*255;
				sheet.Cells[0, 6] = new Cell("Срок годности"){Style = header};
				sheet.Cells.ColumnWidth[6] = 10*255;
				sheet.Cells[0, 7] = new Cell("Примечание"){Style = header};
				sheet.Cells.ColumnWidth[7] = 20*255;

				var i = 1;
				foreach (var offer in offers.Rows.Cast<DataRow>())
				{
					sheet.Cells[i, 0] = new Cell(offer["Code"]){Style = body};
					sheet.Cells[i, 1] = new Cell(offer["CodeCr"]){Style = body};
					sheet.Cells[i, 2] = new Cell(offer["Product"]){Style = body};
					sheet.Cells[i, 3] = new Cell(offer["Producer"]){Style = body};
					sheet.Cells[i, 4] = new Cell(offer["Cost"]){Style = body};
					sheet.Cells[i, 5] = new Cell(offer["Quantity"]){Style = body};
					sheet.Cells[i, 6] = new Cell(offer["Period"]){Style = body};
					sheet.Cells[i, 7] = new Cell(offer["Note"]){Style = body};
					i++;
				}
				

				workbook.Worksheets.Add(sheet);
			}
			workbook.Save(file);
		}
	}
}