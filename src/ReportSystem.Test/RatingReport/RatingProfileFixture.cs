using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NUnit.Framework;
using Inforoom.ReportSystem;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class RatingProfileFixture : BaseProfileFixture2
	{
		[Test, Ignore("Переполнение электронной таблицы")]
		public void Rating()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Rating);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Rating);
		}

		[Test]
		public void RatingJunkOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingJunkOnly);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingJunkOnly);
		}

		[Test, Ignore("Переполнение электронной таблицы")]
		public void RatingNotJunkOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingNotJunkOnly);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingNotJunkOnly);
		}

		[Test]
		public void RatingFull()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingFull);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingFull);
		}

		[Test]
		public void RatingFullWithProductByPrice()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingFullWithProductByPrice);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingFullWithProductByPrice);
		}

		[Test]
		public void Build_chart()
		{
			Property("ByPreviousMonth", false);
			Property("ClientCodeEqual", new List<ulong> { 3110, 465, 11279 });
			Property("ProductNamePosition", 0);
			Property("BuildChart", true);
			var file = "Build_chart.xls";
			report = new RatingReport(1, file, Conn, ReportFormats.Excel, properties);
			BuildOrderReport(file);
		}

		[Test]
		public void Show_only_relative_values()
		{
			Property("ByPreviousMonth", false);
			Property("ClientCodeEqual", new List<ulong> { 3110, 465, 11279 });
			Property("ProductNamePosition", 0);
			Property("BuildChart", true);
			Property("DoNotShowAbsoluteValues", true);
			var file = "Show_only_relative_values.xls";
			report = new RatingReport(1, file, Conn, ReportFormats.Excel, properties);
			BuildOrderReport(file);
		}

		[Test]
		public void Group_by_code_and_product()
		{
			var code = new string(Guid.NewGuid().ToString().Take(20).ToArray());
			var supplier = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			var price = supplier.Prices[0];
			var order = new TestOrder(client.Users[0], price) {
				WriteTime = DateTime.Now.AddDays(-5)
			};
			var productSynonym = price.AddProductSynonym(TestProduct.Random(session).First());
			session.Save(productSynonym);
			var item = order.AddItem(TestProduct.RandomProducts(session).First(), 10, 456);
			item.SynonymCode = productSynonym.Id;
			item.Code = code;
			session.Save(order);
			Property("ReportInterval", 5);
			Property("SupplierProductCodePosition", 0);
			Property("SupplierProductNamePosition", 1);
			Property("FirmCodeEqual", new List<ulong> { supplier.Id });
			var sheet = ReadReport<RatingReport>();
			Assert.AreEqual("Оригинальный код товара", sheet.GetRow(2).GetCell(0).StringCellValue);
			Assert.AreEqual(code, sheet.GetRow(3).GetCell(0).StringCellValue);
			Assert.AreEqual("Оригинальное наименование товара", sheet.GetRow(2).GetCell(1).StringCellValue);
			Assert.AreEqual(productSynonym.Name, sheet.GetRow(3).GetCell(1).StringCellValue);
		}

		[Test]
		public void Group_by_supplier_product_and_supplier_producer()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			var price = supplier.Prices[0];
			var order = new TestOrder(client.Users[0], price) {
				WriteTime = DateTime.Now.AddDays(-5)
			};
			var productSynonym = price.AddProductSynonym(TestProduct.Random(session).First());
			session.Save(productSynonym);
			var producerSynonym = price.AddProducerSynonym(TestProducer.Random(session).First());
			session.Save(productSynonym);
			var item = order.AddItem(TestProduct.RandomProducts(session).First(), 10, 456);
			item.SynonymCode = productSynonym.Id;
			item.SynonymFirmCrCode = producerSynonym.Id;
			session.Save(order);

			Property("ReportInterval", 5);
			Property("SupplierProductNamePosition", 0);
			Property("SupplierProducerNamePosition", 1);
			Property("FirmCodeEqual", new List<ulong> { supplier.Id });
			var sheet = ReadReport<RatingReport>();
			Assert.AreEqual("Оригинальное наименование товара", sheet.GetRow(2).GetCell(0).StringCellValue);
			Assert.AreEqual(productSynonym.Name, sheet.GetRow(3).GetCell(0).StringCellValue);
			Assert.AreEqual("Оригинальное наименование производителя", sheet.GetRow(2).GetCell(1).StringCellValue);
			Assert.AreEqual(producerSynonym.Name, sheet.GetRow(3).GetCell(1).StringCellValue);
		}
	}
}