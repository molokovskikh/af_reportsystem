using System;
using System.Collections.Generic;
using System.Linq;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	internal class OrdersStatisticsProfileFixture : BaseProfileFixture2
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
			var supplier = TestSupplier.CreateNaked();
			supplier.AddRegion(session.Load<TestRegion>(512ul));

			var client = TestClient.CreateNaked(512, 512);
			var order = MakeOrder(client, supplier);
			session.Save(order);

			var client1 = TestClient.CreateNaked();
			var order1 = MakeOrder(client1, supplier);
			session.Save(order1);

			Property("ReportInterval", 7);
			Property("ByPreviousMonth", false);
			Property("RegionEqual", new List<long> { 512 });

			var report = ReadReport<OrdersStatistics>();
			var result = ToText(report);
			Assert.That(result, Is.Not.StringContaining("Воронеж"));
		}

		private TestOrder MakeOrder(TestClient client, TestSupplier supplier)
		{
			var order = new TestOrder(client.Users[0], supplier.Prices[0]);
			var product = session.Query<TestProduct>().First();
			order.WriteTime = order.WriteTime.AddDays(-1);
			order.AddItem(product, 10, 897.23f);
			return order;
		}
	}
}