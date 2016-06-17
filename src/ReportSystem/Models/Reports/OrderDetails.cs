using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using Common.Models;
using Common.MySql;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Inforoom.ReportSystem.Models.Reports
{
	[Description("Детализация заявок")]
	public class OrderDetails : BaseOrdersReport
	{
		[Description("Клиент")]
		public uint ClientId { get; set; }

		public OrderDetails()
		{
			RegistredField.Clear();
		}

		public OrderDetails(MySqlConnection conn, DataSet dsProperties)
			: base(conn, dsProperties)
		{
		}

		public override void Write(string filename)
		{
			ReadReportParams();
			var client = Session.Load<Client>(ClientId);
			Header.Add($"Выбранный клиент: {client.Name}");
			var sql = $@"
select s.Id as SupplierId, s.Name as SupplierName, oh.RowId as Id, a.Address,
	sum(ol.Cost * ol.Quantity) as Sum
from {OrdersSchema}.OrdersHead oh
	join {OrdersSchema}.OrdersList ol on ol.OrderId = oh.RowId
		join Customers.Addresses a on a.Id = oh.AddressId
			join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
				join Customers.Suppliers s on s.Id = pd.FirmCode
where oh.WriteTime > ?begin
	and oh.WriteTime < ?end
	and oh.ClientCode = ?clientId
group by oh.RowId
";
			var table = Connection.Fill(sql, new {
				clientId = ClientId,
				begin = Begin,
				end = End,
			});
			var groups = table.AsEnumerable().GroupBy(x => x["SupplierId"]);
			IWorkbook book;
			if (File.Exists(filename))
				book = WorkbookFactory.Create(filename);
			else
				book = new HSSFWorkbook();
			int rownum = 0;
			var headerStyle = book.CreateCellStyle();
			headerStyle.BorderBottom = BorderStyle.Thin;
			headerStyle.BorderLeft = BorderStyle.Thin;
			headerStyle.BorderRight = BorderStyle.Thin;
			headerStyle.BorderTop = BorderStyle.Thin;
			var headerFont = book.CreateFont();
			headerFont.Boldweight = (short)FontBoldWeight.Bold;
			headerStyle.SetFont(headerFont);

			var dataStyle = book.CreateCellStyle();
			dataStyle.BorderBottom = BorderStyle.Thin;
			dataStyle.BorderLeft = BorderStyle.Thin;
			dataStyle.BorderRight = BorderStyle.Thin;
			dataStyle.BorderTop = BorderStyle.Thin;

			var sheet = book.CreateSheet(GetSheetName());

			WriteDesc(sheet, ref rownum);
			var header = sheet.CreateRow(rownum++);
			header.Cell(0, "Номер заказа", headerStyle);
			header.Cell(1, "Торговая точка", headerStyle);
			header.Cell(2, "Сумма", headerStyle);
			foreach (var group in groups.OrderBy(x => x.First()["SupplierName"])) {
				var row = sheet.CreateRow(rownum++);
				row.Cell(0, group.First()["SupplierName"].ToString(), headerStyle);
				var items = @group.Where(x => !(x["Sum"] is DBNull)).OrderBy(x => x["Id"]);
				row.Cell(2, items.Sum(x => Convert.ToDouble(x["Sum"])), headerStyle);
				foreach (var dataRow in items) {
					row = sheet.CreateRow(rownum++);
					row.Cell(0, dataRow["Id"], dataStyle);
					row.Cell(1, dataRow["Address"], dataStyle);
					row.Cell(2, Convert.ToDouble(dataRow["Sum"]), dataStyle);
				}
			}
			sheet.AutoSizeColumn(0);
			sheet.AutoSizeColumn(1);
			sheet.AutoSizeColumn(2);

			sql = $@"
select s.Id as SupplierId,
	s.Name as SupplierName,
	oh.RowId as Id,
	a.Address,
	s.Synonym as Product,
	sfc.Synonym as Producer,
	ol.Cost,
	ol.Quantity,
	ol.Cost * ol.Quantity as Sum
from {OrdersSchema}.OrdersHead oh
	join {OrdersSchema}.OrdersList ol on ol.OrderId = oh.RowId
		join Customers.Addresses a on a.Id = oh.AddressId
			join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
				join Customers.Suppliers s on s.Id = pd.FirmCode
		left join Farm.SynonymArchive s on s.SynonymCode = ol.SynonymCode
			left join Farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode
where oh.WriteTime > ?begin
	and oh.WriteTime < ?end
	and oh.ClientCode = ?clientId
";
			table = Connection.Fill(sql, new {
				clientId = ClientId,
				begin = Begin,
				end = End,
			});
			groups = table.AsEnumerable().GroupBy(x => x["SupplierId"]);

			rownum = 0;
			sheet = book.CreateSheet(ExcelHelper.GetSheetName(GetSheetName() + "-детализация"));
			WriteDesc(sheet, ref rownum);
			header = sheet.CreateRow(rownum++);
			header.Cell(0, "Номер заказа", headerStyle);
			header.Cell(1, "Торговая точка", headerStyle);
			header.Cell(2, "Наименование", headerStyle);
			header.Cell(3, "Производитель", headerStyle);
			header.Cell(4, "Цена", headerStyle);
			header.Cell(5, "Кол-во", headerStyle);
			header.Cell(6, "Сумма", headerStyle);
			foreach (var group in groups.OrderBy(x => x.First()["SupplierName"])) {
				var row = sheet.CreateRow(rownum++);
				row.Cell(0, group.First()["SupplierName"].ToString(), headerStyle);
				var items = @group.Where(x => !(x["Sum"] is DBNull)).OrderBy(x => x["Id"]).ThenBy(x => x["Product"]);
				row.Cell(6, items.Sum(x => Convert.ToDouble(x["Sum"])), headerStyle);
				foreach (var dataRow in items) {
					row = sheet.CreateRow(rownum++);
					row.Cell(0, dataRow["Id"], dataStyle);
					row.Cell(1, dataRow["Address"], dataStyle);
					row.Cell(2, dataRow["Product"], dataStyle);
					row.Cell(3, dataRow["Producer"], dataStyle);
					row.Cell(4, Convert.ToDouble(dataRow["Cost"]), dataStyle);
					row.Cell(5, Convert.ToDouble(dataRow["Quantity"]), dataStyle);
					row.Cell(6, Convert.ToDouble(dataRow["Sum"]), dataStyle);
				}
			}
			sheet.AutoSizeColumn(0);
			sheet.AutoSizeColumn(1);
			sheet.AutoSizeColumn(2);
			sheet.AutoSizeColumn(3);
			sheet.AutoSizeColumn(4);
			sheet.AutoSizeColumn(5);
			sheet.AutoSizeColumn(6);

			using (var stream = File.Create(filename))
				book.Write(stream);
		}

		private void WriteDesc(ISheet sheet, ref int rownum)
		{
			foreach (var description in Header) {
				var desc = sheet.CreateRow(rownum++);
				desc.Cell(0, description);
			}
		}
	}

	public static class Helper
	{
		public static void Cell(this IRow row, int colnum, object value, ICellStyle style = null)
		{
			var cell = row.CreateCell(colnum);
			if (style != null)
				cell.CellStyle = style;
			if (value is double)
				cell.SetCellValue((double)value);
			else if (value != null)
				cell.SetCellValue(value.ToString());
		}
	}
}