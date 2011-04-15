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
			AddProperty("SupplierId", 5);
			AddProperty("Begin", DateTime.Now.AddDays(-10));
			AddProperty("End", DateTime.Now);
			AddProperty("Regions", new List<long> {1,2,4});
			AddProperty("ByPreviousMonth", false);
			AddProperty("ReportInterval", 12); 
		}

		[Test]
		public void Build_report()
		{
			AddProperty("Type", 0);
			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, false, ReportFormats.Excel, properties);
			BuildReport();
		}

		[Test]
		public void Build_report_by_address()
		{
			AddProperty("Type", 1);
			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, false, ReportFormats.Excel, properties);
			BuildReport("SupplierMarketShareByUserByAddress.xls");
		}

		[Test]
		public void Build_report_by_client()
		{
			AddProperty("Type", 2);
			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, false, ReportFormats.Excel, properties);
			BuildReport("SupplierMarketShareByUserByClient.xls");
		}

		[Test]
		public void Build_report_by_legal_entity()
		{
			AddProperty("Type", 3);
			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, false, ReportFormats.Excel, properties);
			BuildReport("SupplierMarketShareByUserByLegalEntity.xls");
		}
	}
}