using System.Collections.Generic;
using Inforoom.ReportSystem;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OffersReportAscFixture : ReportFixture
	{
		[Test]
		public void Read_properties()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			Property("ReportType", 2);
			Property("ClientCode", client.Id);
			Property("CalculateByCatalog", false);
			Property("PriceCode", supplier.Prices[0].Id);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 10);
			Property("FirmCodeEqual", new List<ulong> { supplier.Id });
			Property("MinSupplierCount", 1);
			TryInitReport<OffersReportAsc>();
			BuildReport();
		}
	}
}