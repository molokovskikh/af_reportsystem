using System;
using Common.Tools;
using ExcelLibrary;
using Inforoom.ReportSystem;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test.MatrixReport
{
	[TestFixture]
	public class MatrixReportFixture : ReportFixture
	{
		[Test]
		public void BaseTest()
		{
			var producerName = Generator.Name();
			var clientId = 0u;
			var supplier = TestSupplier.CreateNaked(session);
			var producer = new TestProducer(producerName);
			session.Save(producer);
			var product = new TestProduct("testProduct");
			session.Save(product);
			var price = supplier.Prices[0];
			price.Enabled = true;
			price.AgencyEnabled = true;
			session.Save(price);

			var client = TestClient.CreateNaked(session);

			var core = new TestCore(price.AddProductSynonym(product)) {
				Price = price,
				Producer = producer,
				Quantity = "2",
				Code = "2",
				Period = ""
			};
			session.Save(core);

			var matrix = new TestMatrix();
			session.Save(matrix);

			var costId = new CostPrimaryKey {
				CoreId = core.Id,
				CostId = price.Costs[0].Id
			};
			var cost = new TestCost {
				Id = costId,
				Cost = 10
			};
			session.Save(cost);

			var rule = client.Settings;
			rule.BuyingMatrix = matrix;
			rule.BuyingMatrixAction = TestMatrixAction.Delete;
			rule.BuyingMatrixType = TestMatrixType.BlackList;
			session.Save(rule);

			session.CreateSQLQuery(string.Format(@"
insert into farm.BuyingMatrix (PriceId, ProductId, MatrixId)
value
({0}, {1}, {2})", price.Id, product.Id, matrix.Id))
				.ExecuteUpdate();
			clientId = client.Id;

			Property("ClientCode", clientId);
			report = new Inforoom.ReportSystem.ByOffers.MatrixReport(Conn, properties);
			BuildOrderReport("Rep.xls");
			var resuleSet = DataSetHelper.CreateDataSet("Rep.xls").Tables[0];
			Assert.That(resuleSet.Rows[4][13], Does.Contain("Удаление предложения"));
			Assert.That(resuleSet.Rows[4][5], Does.Contain(producerName));
		}
	}
}
