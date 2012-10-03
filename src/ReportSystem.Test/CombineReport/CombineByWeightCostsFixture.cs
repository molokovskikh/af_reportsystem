using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test.CombineReport
{
	[TestFixture]
	public class CombineByWeightCostsFixture : BaseProfileFixture
	{
		[Test]
		public void CombineCountProducerByWeightCost()
		{
			Property("ReportType", 4);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", false);
			Property("CalculateByCatalog", false);
			Property("ByWeightCosts", true);
			report = new CombReport(1, "CombineCountProducerByWeightCost.xls", Conn, ReportFormats.Excel, properties);
			BuildReport("CombineCountProducerByWeightCost.xls");
		}

		[Test]
		public void CombineProducerByWeightCost()
		{
			Property("ReportType", 3);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", false);
			Property("CalculateByCatalog", false);
			Property("ByWeightCosts", true);
			report = new CombReport(1, "CombineProducerByWeightCost.xls", Conn, ReportFormats.Excel, properties);
			BuildReport("CombineProducerByWeightCost.xls", typeof(CombReport));
		}

		[Test]
		public void CombineCountByWeightCost()
		{
			Property("ReportType", 2);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", false);
			Property("CalculateByCatalog", false);
			Property("ByWeightCosts", true);
			report = new CombReport(1, "CombineCountByWeightCost.xls", Conn, ReportFormats.Excel, properties);
			BuildReport("CombineCountByWeightCost.xls", typeof(CombReport));
		}

		[Test]
		public void CombineByWeightCost()
		{
			Property("ReportType", 1);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", false);
			Property("CalculateByCatalog", false);
			Property("ByWeightCosts", true);
			report = new CombReport(1, "CombineByWeightCost.xls", Conn, ReportFormats.Excel, properties);
			BuildReport("CombineByWeightCost.xls", typeof(CombReport));
		}
	}
}
