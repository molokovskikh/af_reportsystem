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
			Property("ReportType", 2);
			Property("RegionEqual", new List<ulong> {
				4096
			});
			Property("FirmCodeEqual", new List<ulong> {
				7,
				196
			});
			Property("ReportIsFull", false);
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 196);
			Property("ByWeightCosts", true);
			report = new SpecReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void SpecialCountProducerByWeightCostAssort()
		{
			var fileName = "SpecialCountProducerByWeightCostAssort.xls";
			Property("ReportType", 2);
			Property("RegionEqual", new List<ulong> {
				4194304
			});
			Property("FirmCodeEqual", new List<ulong> {
				338,
				126
			});
			Property("ReportIsFull", false);
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 5699);
			Property("ByWeightCosts", true);
			report = new SpecReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}
