using System;
using System.Collections.Generic;
using System.Data;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using MySql.Data.MySqlClient;
using NUnit.Framework;

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
		public void SetTotalSumTest()
		{
			var testReport = new SupplierMarketShareByUser();

			var table = new DataTable("testTable");
			table.Columns.Add("TotalSum");
			table.Columns.Add("SupplierSum");
			var dataRow = table.NewRow();

			var resultTable = new DataTable("resultTable");
			resultTable.Columns.Add("Share");
			var resultRow = resultTable.NewRow();

			dataRow["TotalSum"] = 0;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual(resultRow["Share"], DBNull.Value);

			dataRow["TotalSum"] = 100000;
			dataRow["SupplierSum"] = 5000;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual(resultRow["Share"], "нет заказов");

			dataRow["SupplierSum"] = 20000;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual(resultRow["Share"], "20");
		}
	}
}