using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using Inforoom.ReportSystem.ByOrders;

namespace ReportSystem.Test
{
	[TestFixture]
	public class PulsOrderReportFixture : ReportFixture
	{
		[Test]
		public void Generate_report()
		{
			var code = new string(Guid.NewGuid().ToString().Take(20).ToArray());
			var supplier = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			var price = supplier.Prices[0];
			var order = new TestOrder(client.Users[0], price)
			{
				WriteTime = DateTime.Now.AddDays(-5)
			};
			var productSynonym = price.AddProductSynonym(TestProduct.Random(session).First());
			session.Save(productSynonym);
			var item = order.AddItem(TestProduct.RandomProducts(session).First(), 10, 456);
			item.SynonymCode = productSynonym.Id;
			item.Code = code;
			session.Save(order);

			Property("ReportInterval", 5);
			Property("SupplierId", supplier.Id);
			Property("RegionEqual", new List<ulong> {
				client.RegionCode
			});

			var AFCode = $"{item.Product.Id}_{item.CodeFirmCr ?? 0}";
			var sheet = ReadReport<PulsOrderReport>();
			var text = ToText(sheet);
			Assert.That(text, Does.Contain(AFCode));
		}
	}
}