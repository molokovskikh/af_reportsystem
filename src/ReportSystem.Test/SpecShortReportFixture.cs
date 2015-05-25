using System;
using System.Collections.Generic;
using Inforoom.ReportSystem;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SpecShortReportFixture : BaseProfileFixture2
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
			Assert.Throws<ReportException>(() => ProcessReport(typeof(SpecShortReport)),
				"Фактическое количество прайс листов меньше трех, получено прайс-листов 1");
		}
	}
}