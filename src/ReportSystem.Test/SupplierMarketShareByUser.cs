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
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SupplierMarketShareByUserFixture : BaseProfileFixture2
	{
		[SetUp]
		public void Setup()
		{
			Property("SupplierId", 5);
			Property("Begin", DateTime.Now.AddDays(-10));
			Property("End", DateTime.Now);
			Property("Regions", new List<long> { 1, 2, 4 });
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
			var order = MakeOrder();
			session.Save(order);
			var intersection = session.Query<TestIntersection>()
				.First(i => i.Price == order.Price && i.Client == order.Client);
			intersection.SupplierClientId = Guid.NewGuid().ToString();
			session.Save(intersection);

			var data = properties.Tables[0];
			var row = data.AsEnumerable().First(r => r["PropertyName"].Equals("SupplierId"));
			data.Rows.Remove(row);

			Property("SupplierId", order.Price.Supplier.Id);
			Property("Type", 3);

			var report = ReadReport<SupplierMarketShareByUser>();
			var result = ToText(report);
			Assert.That(result, Is.StringContaining(intersection.SupplierClientId));
			Assert.That(result, Is.StringContaining("Кол-во поставщиков"));
			var reportRow = report.GetRowEnumerator().Cast<IRow>()
				.First(r => r.GetCell(0).StringCellValue == intersection.SupplierClientId);
			Assert.That(Convert.ToUInt32(reportRow.GetCell(4).StringCellValue), Is.GreaterThan(0));
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