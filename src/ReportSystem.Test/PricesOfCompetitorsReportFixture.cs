using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class PricesOfCompetitorsReportFixture : BaseProfileFixture2
	{
		[Test]
		public void Check_max_supplier_count()
		{
			var supplier1 = TestSupplier.CreateNaked(session);
			var supplier2 = TestSupplier.CreateNaked(session);
			var supplier3 = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			var client1 = TestClient.CreateNaked(session);
			session.CreateSQLQuery("update customers.intersection set AgencyEnabled = 0 where PriceId not in (:ids) and ClientId = :clientId")
				.SetParameter("clientId", client.Id)
				.SetParameterList("ids", supplier1.Prices.Select(p => p.Id).ToArray())
				.ExecuteUpdate();

			Property("PriceCode", supplier1.Prices[0].Id);
			Property("ProducerAccount", false);
			Property("AllAssortment", false);
			Property("WithWithoutProperties", false);
			Property("ShowCodeC", false);
			Property("ShowCodeCr", false);
			Property("FirmCodeEqual", new List<long> { supplier1.Id, supplier2.Id, supplier3.Id });
			Property("Clients", new List<long> { client.Id, client1.Id });
			//проверяем что не возникает ошибка
			//Для клиента ... получено фактическое количество прайс листов меньше трех, получено прайс-листов 1
			ProcessReport(typeof(PricesOfCompetitorsReport));
		}

		[Test]
		public void Prices_filter()
		{
			var supplier1 = TestSupplier.CreateNaked(session);
			var supplier2 = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);

			Property("PriceCode", supplier1.Prices[0].Id);
			Property("ProducerAccount", false);
			Property("AllAssortment", false);
			Property("WithWithoutProperties", false);
			Property("ShowCodeC", false);
			Property("ShowCodeCr", false);
			Property("PriceCodeValues", new List<long> { supplier1.Prices[0].Id, supplier2.Prices[0].Id });
			Property("Clients", new List<long> { client.Id });
			BuildReport(reportType: typeof(PricesOfCompetitorsReport));
		}
	}
}