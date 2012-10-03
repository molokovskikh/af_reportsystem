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
		[Test]
		public void DefectureByWeight()
		{
			for(int i = 4; i < 6; i++) {
				properties.Tables[0].Rows.Clear();
				properties.Tables[1].Rows.Clear();
				var fileName = "DefectureByWeightCost" + i + ".xls";
				Property("ReportType", i);
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
}
