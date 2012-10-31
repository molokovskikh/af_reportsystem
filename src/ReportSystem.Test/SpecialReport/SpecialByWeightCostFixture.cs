using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test.SpecialReport
{
	public class SpecialByWeightCostFixture : BaseProfileFixture
	{
		[Test]
		public void SpecialCountProducerByWeightCost()
		{
			var fileName = "SpecialCountProducerByWeightCost.xls";
			Property("ReportType", 4);
			Property("RegionEqual", new List<ulong> {
				1,
				16
			});
			Property("SupplierNoise", 5);
			Property("FirmCodeEqual", new List<ulong> {
				196,
				5
			});
			Property("ReportIsFull", false);
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 4816);
			Property("ByWeightCosts", true);
			report = new SpecReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}
