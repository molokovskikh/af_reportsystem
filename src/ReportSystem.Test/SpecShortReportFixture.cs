using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inforoom.ReportSystem;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using NHibernate.Linq;
using Common.Tools;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SpecShortReportFixture : ReportFixture
	{
		[Test]
		public void Fail_if_suppliers_is_not_enough()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			Property("ReportType", 0);
			Property("CalculateByCatalog", false);
			Property("PriceCode", (int)supplier.Prices[0].Id);
			Property("ReportIsFull", false);
			Property("FirmCodeEqual", new List<ulong> { supplier.Id });
			Property("Clients", new List<ulong> { client.Id });
			Assert.Throws<ReportException>(() => BuildReport(reportType: (typeof(SpecShortReport))),
				"Фактическое количество прайс листов меньше трех, получено прайс-листов 1");
		}

		[Test]
		public void Configure_min_supplier_count()
		{
			var supplier1 = TestSupplier.CreateNaked(session);
			var supplier2 = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			Property("ReportType", 0);
			Property("CalculateByCatalog", false);
			Property("PriceCode", (int)supplier1.Prices[0].Id);
			Property("ReportIsFull", false);
			Property("MinSupplierCount", 2);
			Property("FirmCodeEqual", new List<ulong> { supplier1.Id, supplier2.Id });

			// http://redmine.analit.net/issues/52707
			Property("FirmCodeEqual2", new List<ulong> { supplier2.Id });

			Property("Clients", new List<ulong> { client.Id });
			BuildReport(reportType: typeof(SpecShortReport));
			Assert.IsTrue(File.Exists("test.xls"));
		}

		// http://redmine.analit.net/issues/52707
		[Test]
		public void With_FirmCodeEqual2()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var supplier1 = TestSupplier.CreateNaked(session);
			var supplier2 = TestSupplier.CreateNaked(session);

			supplier1.Name = "Тестовый поставщик 1";
			session.Save(supplier1);

			supplier2.Name = "Тестовый поставщик 2";
			session.Save(supplier2);

			var product1 = session.Query<TestProduct>().First(p => p.CatalogProduct.Pharmacie);
			var product2 = session.Query<TestProduct>().First(p => !p.CatalogProduct.Pharmacie);

			supplier1.CreateSampleCore(session, new[] { product1 });
			supplier2.CreateSampleCore(session, new[] { product2 });
			supplier.CreateSampleCore(session, new[] { product1, product2 });

			// Минимальные цены (90 руб) у конкурентов, у supplier - 100 руб.
			var offer1 = supplier1.Prices[0].Core[0];
			offer1.Costs[0].Cost = 90;
			session.Save(offer1);

			var offer2 = supplier2.Prices[0].Core[0];
			offer2.Costs[0].Cost = 90;
			session.Save(offer2);

			var client = TestClient.CreateNaked(session);

			Property("ReportType", 0);
			Property("CalculateByCatalog", false);
			Property("PriceCode", (int)supplier.Prices[0].Id);
			Property("ReportIsFull", false);
			Property("FirmCodeEqual", new List<ulong> { supplier.Id, supplier1.Id, supplier2.Id });
			Property("FirmCodeEqual2", new List<ulong> { supplier1.Id });
			Property("Clients", new List<ulong> { client.Id });

			var sheet = ReadReport<SpecShortReport>();
			var rows = sheet.Rows().ToArray();

			// минимальная цена по первой позиции 90 руб
			var firstProduct = rows.First(r => r.GetCell(3) != null && r.GetCell(3).StringCellValue == product1.FullName);
			Assert.AreEqual(90, firstProduct.GetCell(7).NumericCellValue);

			// второго товара нет в отчете, потому что минимальная цена на него у supplier2, которого нет в FirmCodeEqual2
			var secondProduct = rows.FirstOrDefault(r => r.GetCell(3) != null && r.GetCell(3).StringCellValue == product2.FullName);
			Assert.IsNull(secondProduct);

			var result = ToText(sheet);
			Assert.That(result, Does.Contain("В отчете размещены позиции, минимальные цены по которым принадлежат поставщикам: Тестовый поставщик 1 - Воронеж"));
		}

		[Test]
		public void Check_max_supplier_count()
		{
			var supplier1 = TestSupplier.CreateNaked(session);
			var supplier2 = TestSupplier.CreateNaked(session);
			var supplier3 = TestSupplier.CreateNaked(session);
			var client1 = TestClient.CreateNaked(session);
			var client2 = TestClient.CreateNaked(session);
			client2.Users[0].CleanPrices(session, supplier3);

			session.CreateSQLQuery("update customers.intersection set AgencyEnabled = 0 where PriceId not in (:ids) and ClientId = :clientId")
				.SetParameter("clientId", client1.Id)
				.SetParameterList("ids", supplier1.Prices.Select(p => p.Id).ToArray())
				.ExecuteUpdate();

			Property("ReportType", 0);
			Property("CalculateByCatalog", false);
			Property("PriceCode", (int)supplier1.Prices[0].Id);
			Property("ReportIsFull", false);
			Property("FirmCodeEqual", new List<ulong> { supplier1.Id, supplier2.Id, supplier3.Id });
			Property("Clients", new List<ulong> { client1.Id, client2.Id });
			//проверяем отсутствие исключения Фактическое количество прайс листов меньше трех, получено прайс-листов 1
			BuildReport(reportType: (typeof(SpecShortReport)));
		}
	}
}