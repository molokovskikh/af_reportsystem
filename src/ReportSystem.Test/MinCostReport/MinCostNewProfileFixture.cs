using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test.MinCostReport
{
	[TestFixture]
	public class MinCostNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MinCostNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostNew);
			var report = new CombShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostNew);
		}

		[Test]
		public void MinCostNewDificult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostNewDificult);
			var report = new CombShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostNewDificult);
		}
	}
}
