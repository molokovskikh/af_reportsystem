using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MinCostByPriceNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MinCostByPriceNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNew);
		}

		[Test]
		public void MinCostByPriceNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewDifficult);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewDifficult);
		}
	}
}
