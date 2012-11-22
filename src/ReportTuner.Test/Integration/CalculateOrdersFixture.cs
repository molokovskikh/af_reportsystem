using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportTuner.Test.Integration
{
	[TestFixture]
	public class CalculateOrdersFixture : IntegrationFixture
	{
		[Test]
		public void FreeOrdersTest()
		{
			var supplier = TestSupplier.Create();
			var client = TestClient.CreateNaked();
			var product = new TestProduct("Тестовый продукт");
			session.Save(product);
			var order = new TestOrder(client.Users.First(), supplier.Prices[0]);
			session.Save(order);
			order.AddItem(product, 1, 100);
			session.Save(order);

			var queryString = String.Format("call orders.CalculateOrders('{0}','{1}')",
				DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"),
				DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"));
			var query = session.CreateSQLQuery(queryString);
			var result = query.List<object[]>();
			decimal sum1 = 0;
			decimal sum2 = 0;
			foreach (var obj in result) {
				sum1 += (decimal)obj[3];
			}

			var freeOrdersQuery = session.CreateSQLQuery(
				String.Format("INSERT INTO billing.freeorders VALUES({0}, {1});",
					order.Address.Payer.Id,
					supplier.Payer.Id));
			var a = freeOrdersQuery.ExecuteUpdate();
			Reopen();
			query = session.CreateSQLQuery(queryString);
			result = query.List<object[]>();
			foreach (var obj in result) {
				sum2 += (decimal)obj[3];
			}

			Assert.That(sum2, Is.EqualTo(sum1 - 100));
		}
	}
}
