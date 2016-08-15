using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inforoom.ReportSystem;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

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