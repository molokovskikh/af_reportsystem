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
			for (int j = 1; j < 5; j++) {
				properties.Tables[0].Rows.Clear();
				properties.Tables[1].Rows.Clear();
				properties.Tables[0].AcceptChanges();
				properties.Tables[1].AcceptChanges();
				var fileName = String.Format("SpecialByWeightCost{0}.xls", j);
				Property("ReportType", j);
				Property("RegionEqual", new List<ulong> {
					1
				});
				Property("ReportIsFull", true);
				Property("ClientCode", 5101);
				Property("ReportSortedByPrice", false);
				Property("ShowPercents", true);
				Property("CalculateByCatalog", false);
				Property("PriceCode", 196);
				Property("ByWeightCosts", true);
				report = new SpecReport(1, fileName, Conn, ReportFormats.Excel, properties);
				BuildReport(fileName);
			}
		}
	}
}
