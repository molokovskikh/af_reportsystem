using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using NHibernate.Linq;
using NPOI.SS.UserModel;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class WaybillsStatReportFixture : BaseProfileFixture2
	{
		[Test]
		public void Build()
		{
			var client = TestClient.CreateNaked();
			var address = client.Addresses[0];
			var supplier = TestSupplier.CreateNaked();

			var waybill = new TestWaybill(new TestDocumentLog(supplier, address));
			var product = session.Query<TestProduct>().First();
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "Аксетин",
				CatalogProduct = product,
				Quantity = 10,
				SerialNumber = "4563",
				EAN13 = "5290931004832",
				ProducerCost = 56,
				SupplierCost = 100,
			});
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "Аксетин",
				CatalogProduct = product,
				Quantity = 10,
				SerialNumber = "4563",
				EAN13 = "5290931004832",
				ProducerCost = 56,
				SupplierCost = 70,
			});
			session.Save(waybill);

			Reopen();
			Property("ProductNamePosition", 0);
			Property("ByPreviousMonth", false);
			report = new WaybillsStatReport(1, "test", Conn, ReportFormats.Excel, properties);
			report.From = DateTime.Today.AddDays(-10);
			report.To = DateTime.Today;
			report.Interval = true;
			var sheet = ReadReport();
			var row = sheet.GetRowEnumerator().Cast<IRow>().FirstOrDefault(r => r.GetCell(0).StringCellValue.Contains(product.CatalogProduct.Name));
			Assert.IsNotNull(row, "товар = {0}\r\n данные = {1}", product.CatalogProduct.Name, ToText(sheet));
			//Кол-во заявок по препарат
			Assert.That(row.GetCell(8).NumericCellValue, Is.GreaterThan(0));
			//Кол-во адресов доставки, заказавших препарат
			Assert.That(row.GetCell(9).NumericCellValue, Is.GreaterThan(0));
		}
	}
}