using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test.DefectureReport
{
	public class DefectureByWeightCostsFixture : BaseProfileFixture
	{
		[Test, Ignore("Готовит пустой набор данных")]
		public void DefectureByWeight()
		{
			var fileName = "DefectureByWeightCost.xls";
			Property("ReportType", 5);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 7160);
			Property("PriceCode", 196);
			Property("ByWeightCosts", true);
			report = new DefReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}
