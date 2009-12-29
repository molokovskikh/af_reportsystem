﻿using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OptimizationEfficiencyFixture : BaseProfileFixture
	{
		[Test]
		public void OptimizationEfficiencyTest()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedProductName);
			var report = new OptimizationEfficiency(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OptimizationEfficiency);
		}
	}
}
