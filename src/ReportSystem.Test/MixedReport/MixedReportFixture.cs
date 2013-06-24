using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NPOI.SS.UserModel;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MixedReportFixture : BaseProfileFixture2
	{
		private TestSupplier rival;
		private TestSupplier supplier;
		private TestClient client;
		private TestOrder order;

		[Test]
		public void Build_with_mnn()
		{
			DefaultConf();

			var sheet = ReadReport<MixedReport>();
			var product = order.Items[0].Product;
			Assert.That(sheet.GetRow(5).GetCell(1).StringCellValue, Is.EqualTo(product.CatalogProduct.CatalogName.Mnn.Mnn));
		}

		[Test]
		public void Filter_by_mnn()
		{
			DefaultConf();

			var product = order.Items[0].Product;
			var mnn = product.CatalogProduct.CatalogName.Mnn;
			var product1 = session.Query<TestProduct>().First(p => p.CatalogProduct.CatalogName.Mnn != mnn);
			var mnn1 = product1.CatalogProduct.CatalogName.Mnn;
			Property("MnnNonEqual", new List<long> { mnn1.Id });
			order.AddItem(product1, 34, 123.34f);

			var sheet = ReadReport<MixedReport>();
			var text = ToText(sheet);
			Assert.That(sheet.GetRow(3).GetCell(0).StringCellValue,
				Is.EqualTo(String.Format("Следующие МНН исключены из отчета: {0}", mnn1.Mnn)));
			Assert.That(sheet.GetRow(6).GetCell(1).StringCellValue, Is.EqualTo(mnn.Mnn));
			var tableText = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
				.Skip(4)
				.Implode(Environment.NewLine);
			Assert.That(tableText, Is.Not.StringContaining(mnn1.Mnn));
		}

		[Test]
		public void Reorder_column()
		{
			Assert.That(MakeColumns("ProductName, FirmCr, Mnn"), Is.EqualTo("ProductName, Mnn, FirmCr"));
			Assert.That(MakeColumns("Mnn, ProductName, FirmCr"), Is.EqualTo("ProductName, Mnn, FirmCr"));
		}

		[Test]
		public void Ignore_mnn_without_product()
		{
			var report = new OrdersReport();
			var mnn = report.registredField.First(r => r.reportPropertyPreffix == "Mnn");
			report.selectedField.Add(mnn);
			var producer = report.registredField.First(r => r.reportPropertyPreffix == "FirmCr");
			report.selectedField.Add(producer);
			report.CheckAfterLoadFields();
			report.SortFields();
			Assert.That(report.selectedField.Implode(f => f.reportPropertyPreffix), Is.EqualTo("FirmCr"));
		}

		private static string MakeColumns(string decl)
		{
			var report = new OrdersReport();
			var columns = decl.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < columns.Length; i++) {
				var product = report.registredField.First(r => r.reportPropertyPreffix == columns[i].Trim());
				product.position = i;
				report.selectedField.Add(product);
			}

			report.CheckAfterLoadFields();
			report.SortFields();
			return report.selectedField.Implode(f => f.reportPropertyPreffix);
		}

		private void DefaultConf()
		{
			rival = TestSupplier.CreateNaked();
			supplier = TestSupplier.CreateNaked();
			client = TestClient.CreateNaked();
			order = new TestOrder(client.Users[0], supplier.Prices[0]);
			var product = session.Query<TestProduct>().First(p => p.CatalogProduct.CatalogName.Mnn != null);
			order.WriteTime = order.WriteTime.AddDays(-1);
			order.AddItem(product, 10, 897.23f);
			session.Save(order);

			Property("ProductNamePosition", 0);
			Property("MnnPosition", 1);

			Property("ByPreviousMonth", false);
			Property("ReportInterval", 1);

			Property("SourceFirmCode", (int)supplier.Id);
			Property("BusinessRivals", new List<long> { rival.Id });
		}
	}
}