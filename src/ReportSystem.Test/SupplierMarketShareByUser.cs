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
		[Test]
		public void Build_report()
		{
			AddProperty("SupplierId", 5);
			AddProperty("Begin", DateTime.Now.AddDays(-10));
			AddProperty("End", DateTime.Now);
			AddProperty("Regions", new List<long> {1l});
		    AddProperty("ByPreviousMonth", false);
            AddProperty("ReportInterval", 12); 
			report = new SupplierMarketShareByUser(1, "SupplierMarketShareByUser.xls", Conn, false, ReportFormats.Excel, properties);
			BuildReport();
		}
	}
}