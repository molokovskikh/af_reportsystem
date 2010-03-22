﻿using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class RatingProfileFixture : BaseProfileFixture
	{
		[Test]
		public void Rating()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Rating);
			var report = new RatingReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Rating);
		}

		[Test]
		public void RatingJunkOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingJunkOnly);
			var report = new RatingReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingJunkOnly);
		}

		[Test]
		public void RatingNotJunkOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingNotJunkOnly);
			var report = new RatingReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingNotJunkOnly);
		}

		[Test]
		public void RatingFull()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingFull);
			var report = new RatingReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingFull);
		}
	}
}
