﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test.MinCostReport
{
	[TestFixture]
	public class DbfMinCostNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void DbfMinCostNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostNew);
			var report = new CombShortReport(0, "MinCostNewDbf", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostNew);
		}

		[Test]
		public void DbfMinCostNewDificult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostNewDificult);
			var report = new CombShortReport(0, "MinCostNewDificultDbf", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostNewDificult);
		}
	}
}