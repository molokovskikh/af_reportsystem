using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	internal class OrdersStatisticsFixture : BaseProfileFixture2
	{
		[Test]
		public void CheckReport()
		{
			Property("ReportInterval", 7);
			Property("ByPreviousMonth", false);

			var report = new OrdersStatistics(0, "Automate Created Report", Conn, ReportFormats.Excel, properties);
			TestHelper.ProcessReport(report, ReportsTypes.OrdersStatistics);
		}

		[Test]
		public void Region_filter()
		{
			var supplier = TestSupplier.CreateNaked(session);
			supplier.AddRegion(session.Load<TestRegion>(512ul));

			var client = TestClient.CreateNaked(session, 512, 512);
			var order = CreateOrder(client, supplier);
			session.Save(order);

			var client1 = TestClient.CreateNaked(session);
			var order1 = CreateOrder(client1, supplier);
			session.Save(order1);

			Property("ReportInterval", 7);
			Property("ByPreviousMonth", false);
			Property("RegionEqual", new List<long> { 512 });

			var report = ReadReport<OrdersStatistics>();
			var result = ToText(report);
			Assert.That(result, Is.Not.StringContaining("Воронеж"));
		}

		[Test]
		public void FreeOrdersTest()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			var product = new TestProduct("Тестовый продукт");
			session.Save(product);

			var order = new TestOrder(client.Users.First(), supplier.Prices[0]);
			order.AddItem(product, 1, 100);
			session.Save(order);

			var result = ExecuteReport();
			var sum1 = result.AsEnumerable().Where(r => r["OrdersSum"] != DBNull.Value).Sum(r => (decimal)r["OrdersSum"]);

			var freeOrdersQuery = session.CreateSQLQuery(
				String.Format("INSERT INTO billing.freeorders VALUES({0}, {1});",
					order.Address.Payer.Id,
					supplier.Payer.Id));
			freeOrdersQuery.ExecuteUpdate();

			result = ExecuteReport();
			var sum2 = result.AsEnumerable().Where(r => r["OrdersSum"] != DBNull.Value).Sum(r => (decimal)r["OrdersSum"]);

			Assert.That(sum2, Is.EqualTo(sum1 - 100));
		}

		private DataTable ExecuteReport()
		{
			if (properties.Tables[0].Rows.Count == 0)
				Property("ByPreviousMonth", false);

			TryInitReport<OrdersStatistics>("test.xls");
			report.Interval = true;
			report.From = DateTime.Today;
			report.To = DateTime.Today.AddDays(1);
			report.ReadReportParams();
			report.ProcessReport();
			return report.GetReportTable();
		}
	}
}