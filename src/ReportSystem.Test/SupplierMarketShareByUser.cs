using System;
using System.Collections.Generic;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SupplierMarketShareByUserFixture : BaseProfileFixture
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
	}
}