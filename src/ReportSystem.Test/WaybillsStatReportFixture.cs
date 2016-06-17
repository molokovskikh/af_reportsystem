using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
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
	public class WaybillsStatReportFixture : ReportFixture
	{
		private TestSupplier supplier;
		private TestProduct product1;
		private TestProduct product2;

		[SetUp]
		public void Setup()
		{
			var client = TestClient.CreateNaked(session);
			var address = client.Addresses[0];
			supplier = TestSupplier.CreateNaked(session);

			var waybill = new TestWaybill(new TestDocumentLog(supplier, address));
			product1 = session.Query<TestProduct>().First();
			product2 = session.Query<TestProduct>().Skip(1).First();
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "Аксетин",
				CatalogProduct = product1,
				Quantity = 10,
				SerialNumber = "4563",
				EAN13 = "5290931004832",
				ProducerCost = 56,
				SupplierCost = 100,
			});
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "Аксетин",
				CatalogProduct = product2,
				Quantity = 10,
				SerialNumber = "4563",
				EAN13 = "5290931004832",
				ProducerCost = 56,
				SupplierCost = 70,
			});
			session.Save(waybill);
		}

		[Test]
		public void Build()
		{
			Property("ProductNamePosition", 0);
			Property("ByPreviousMonth", false);
			report = new WaybillsStatReport(Conn, properties);
			report.From = DateTime.Today.AddDays(-10);
			report.To = DateTime.Today;
			report.Interval = true;
			var sheet = ReadReport();
			var row = sheet.Rows().FirstOrDefault(r => r.GetCell(0).StringCellValue.Contains(product1.CatalogProduct.Name));
			Assert.IsNotNull(row, "товар = {0}\r\n данные = {1}", product1.CatalogProduct.Name, ToText(sheet));
			//Кол-во заявок по препарат
			Assert.That(row.GetCell(8).NumericCellValue, Is.GreaterThan(0));
			//Кол-во адресов доставки, заказавших препарат
			Assert.That(row.GetCell(9).NumericCellValue, Is.GreaterThan(0));
			var row2 = sheet.Rows().FirstOrDefault(r => r.GetCell(0).StringCellValue.Contains(product2.CatalogProduct.Name));
			Assert.IsNotNull(row2, "товар = {0}\r\n данные = {1}", product2.CatalogProduct.Name, ToText(sheet));
		}

		[Test]
		public void Show_code()
		{
			var synonym = new TestProductSynonym(product1.CatalogProduct.Name, product1, supplier.Prices[0]);
			session.Save(synonym);
			var offer = new TestCore(synonym);
			offer.Code = Generator.Random().First().ToString();
			session.Save(offer);

			Property("ProductNamePosition", 0);
			Property("ByPreviousMonth", false);
			Property("ShowCode", true);
			Property("SupplierId", supplier.Id);
			report = new WaybillsStatReport(Conn, properties);
			report.From = DateTime.Today.AddDays(-10);
			report.To = DateTime.Today;
			report.Interval = true;
			var sheet = ReadReport();
			var row = sheet.Rows().FirstOrDefault(r => r.GetCell(1).StringCellValue.Contains(product1.CatalogProduct.Name));
			Assert.IsNotNull(row, "товар = {0}\r\n данные = {1}", product1.CatalogProduct.Name, ToText(sheet));
			Assert.AreEqual(offer.Code, row.GetCell(0).StringCellValue);
		}
	}
}