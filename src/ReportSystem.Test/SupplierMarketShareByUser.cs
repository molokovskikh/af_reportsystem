using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NPOI.SS.UserModel;
using NUnit.Framework;
using Test.Support;
using Test.Support.Logs;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SupplierMarketShareByUserFixture : BaseProfileFixture2
	{
		private TestSupplier supplier;
		private TestOrder order;

		[SetUp]
		public void Setup()
		{
			order = MakeOrder();
			supplier = order.Price.Supplier;
			session.Save(order);
			Property("SupplierId", supplier.Id);
			Property("Begin", DateTime.Now.AddDays(-10));
			Property("End", DateTime.Now.AddDays(1));
			Property("Regions", new List<long> { (long)order.RegionCode });
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 12);
		}

		[Test]
		public void Build_report()
		{
			Property("Type", 0);

			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, ReportFormats.Excel, properties);
			BuildReport();
		}

		[Test]
		public void Build_report_by_address()
		{
			Property("Type", 1);

			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, ReportFormats.Excel, properties);
			BuildReport("SupplierMarketShareByUserByAddress.xls");
		}

		[Test]
		public void Build_report_by_client()
		{
			Property("Type", 2);

			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, ReportFormats.Excel, properties);
			BuildReport("SupplierMarketShareByUserByClient.xls");
		}

		[Test]
		public void Build_report_by_legal_entity()
		{
			Property("Type", 3);

			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, ReportFormats.Excel, properties);
			BuildReport("SupplierMarketShareByUserByLegalEntity.xls");
		}

		[Test]
		public void Calculate_supplier_client_id()
		{
			var intersection = session.Query<TestIntersection>()
				.First(i => i.Price == order.Price && i.Client == order.Client);
			intersection.SupplierClientId = Guid.NewGuid().ToString();
			session.Save(intersection);

			session.Save(new TestAnalitFUpdateLog(TestRequestType.SendOrders, order.User) {
				RequestTime = DateTime.Now.AddDays(-1)
			});

			Property("Type", 3);

			var report = ReadReport<SupplierMarketShareByUser>();
			var result = ToText(report);
			Assert.That(result, Is.StringContaining(intersection.SupplierClientId));
			Assert.That(result, Is.StringContaining("Кол-во поставщиков"));
			Assert.That(result, Is.StringContaining("Кол-во сессий отправки заказов"));
			Assert.That(result, Is.StringContaining("Самая поздняя заявка"));
			var rows = report.Rows().ToArray();
			//проверяем что индексы которые используются ниже не изменились
			var header = rows[4];
			Assert.AreEqual("Кол-во поставщиков", header.GetCell(4).StringCellValue);
			Assert.AreEqual("Кол-во сессий отправки заказов", header.GetCell(5).StringCellValue);
			Assert.AreEqual("Самая поздняя заявка", header.GetCell(6).StringCellValue);
			//проверяем что в колонке Кол-во поставщиков есть данные
			var reportRow = rows
				.First(r => r.GetCell(0) != null && r.GetCell(0).StringCellValue == intersection.SupplierClientId);
			Assert.That(Convert.ToUInt32(reportRow.GetCell(4).StringCellValue), Is.GreaterThan(0));
			Assert.AreEqual("1", reportRow.GetCell(5).StringCellValue);
			Assert.AreEqual(order.WriteTime.ToString("HH:mm:ss"), reportRow.GetCell(6).StringCellValue);
		}

		[Test]
		public void SetTotalSumTest()
		{
			var testReport = new SupplierMarketShareByUser();

			var table = new DataTable("testTable");
			table.Columns.Add("TotalSum");
			table.Columns.Add("SupplierSum");
			var dataRow = table.NewRow();

			var resultTable = new DataTable("resultTable");
			resultTable.Columns.Add("Share");
			resultTable.Columns.Add("SupplierSum");
			var resultRow = resultTable.NewRow();

			dataRow["TotalSum"] = 0;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual(resultRow["Share"], DBNull.Value);

			dataRow["TotalSum"] = 100000;
			dataRow["SupplierSum"] = 5000;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual(resultRow["Share"], "5");

			dataRow["SupplierSum"] = 20000;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual(resultRow["Share"], "20");
		}
	}
}