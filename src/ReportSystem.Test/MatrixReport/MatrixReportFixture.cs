using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Common.Models.BuyingMatrix;
using Common.Tools;
using ExcelLibrary;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test.MatrixReport
{
	public class MatrixReportForTest : Inforoom.ReportSystem.ByOffers.MatrixReport
	{
		public MatrixReportForTest(ulong reportCode, string reportCaption, MySqlConnection connection, ReportFormats format, DataSet dsProperties) : base(reportCode, reportCaption, connection, format, dsProperties)
		{
		}

		public DataSet Result
		{
			get { return _dsReport; }
		}
	}

	[TestFixture]
	public class MatrixReportFixture : BaseProfileFixture2
	{
		[Test]
		public void BaseTest()
		{
			var producerName = Generator.Name();
			var clientId = 0u;
			var supplier = TestSupplier.CreateNaked();
			var producer = new TestProducer(producerName);
			session.Save(producer);
			var product = new TestProduct("testProduct");
			session.Save(product);
			var price = supplier.Prices[0];
			price.Enabled = true;
			price.AgencyEnabled = true;
			session.Save(price);

			var client = TestClient.CreateNaked();

			var core = new TestCore() {
				Price = price,
				Producer = producer,
				Product = product,
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

			Reopen();

			Property("ClientCode", clientId);
			report = new MatrixReportForTest(clientId, "test", Conn, ReportFormats.Excel, properties);
			BuildOrderReport("Rep.xls");
			var resuleSet = DataSetHelper.CreateDataSet("Rep.xls").Tables[0];
			Assert.That(resuleSet.Rows[4][13], Is.StringContaining("Удаление предложения"));
			Assert.That(resuleSet.Rows[4][5], Is.StringContaining(producerName));
		}
	}
}
