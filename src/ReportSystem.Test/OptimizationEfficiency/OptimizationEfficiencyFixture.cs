using System;
using System.Linq;
using Inforoom.ReportSystem;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OptimizationEfficiencyFixture : ReportFixture
	{
		[Test]
		public void OptimizationEfficiencyNorman()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OptimizationEfficiencyWithSupplier);
			var report = new OptimizationEfficiency(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OptimizationEfficiencyWithSupplier);
		}

		[Test]
		public void OptimizationEfficiencyNew()
		{
			var fileName = "OptimizationEfficiencyNew.xls";
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 70);
			Property("FirmCode", 5);
			report = new OptimizationEfficiency(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OptimizationRivalOrders()
		{
			var fileName = "OptimizationRivalOrders.xls";
			Property("ByPreviousMonth", false);
			Property("FirmCode", 5);
			Property("ReportInterval", 70);
			report = new OptimizationRivalOrders(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OptimizationRivalOrdersWithSupplier()
		{
			var fileName = "OptimizationRivalOrdersWithSupplier.xls";
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 70);
			Property("FirmCode", 5);
			report = new OptimizationRivalOrders(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OptimizationRivalOrdersWithClient()
		{
			var fileName = "OptimizationRivalOrdersWithClient.xls";
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 70);
			Property("FirmCode", 5);
			Property("ClientCode", 376);
			report = new OptimizationRivalOrders(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void Cost_optimization_for_global_rule()
		{
			var product = session.Query<TestProduct>().First();
			var producer = session.Query<TestProducer>().First();
			var supplier = TestSupplier.CreateNaked(session);
			var productSynonym = supplier.Prices[0].AddProductSynonym(product);
			var producerSynonym = supplier.Prices[0].AddProducerSynonym(producer);
			var client = TestClient.CreateNaked(session);

			var order = new TestOrder(client.Users[0], supplier.Prices[0]);
			var item = order.AddItem(product, 10, 150);
			item.CodeFirmCr = producer.Id;
			item.SynonymCode = productSynonym.Id;
			item.SynonymFirmCrCode = producerSynonym.Id;
			order.WriteTime = order.WriteTime.AddDays(-2);
			session.Save(order);

			session.CreateSQLQuery(@"insert into logs.CostOptimizationLogs(LoggedOn, ClientId, ProductId, ProducerId, SelfCost, ResultCost, SupplierId, UserId)
values (:loggedOn, :clientId, :productId, :producerId, :selfCost, :resultCost, :supplierId, :userId);")
				.SetParameter("clientId", client.Id)
				.SetParameter("productId", product.Id)
				.SetParameter("producerId", producer.Id)
				.SetParameter("selfCost", 100)
				.SetParameter("resultCost", 150)
				.SetParameter("supplierId", supplier.Id)
				.SetParameter("userId", client.Users[0].Id)
				.SetParameter("loggedOn", order.WriteTime.AddHours(-2))
				.ExecuteUpdate();

			Property("ByPreviousMonth", false);
			Property("ReportInterval", 5);
			Property("FirmCode", supplier.Id);
			var sheet = ReadReport<OptimizationEfficiency>();
			var row = sheet.GetRow(9);
			//количество
			Assert.AreEqual(10, row.GetCell(9).NumericCellValue);
			//начальная цена
			Assert.AreEqual(100, row.GetCell(10).NumericCellValue);
			//конечная цена
			Assert.AreEqual(150, row.GetCell(11).NumericCellValue);
		}
	}
}