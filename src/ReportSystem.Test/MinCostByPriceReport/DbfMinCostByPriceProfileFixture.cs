﻿using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using ReportSystem.Profiling;

namespace ReportSystem.Test
{
	[TestFixture]
	public class DbfMinCostByPriceProfileFixture : BaseProfileFixture
	{
		[Test]
		public void DbfMinCostByPrice()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPrice);
			var report = new SpecShortReport(0, "MinCostByPrice", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPrice);
		}

		[Test]
		public void DbfMinCostByPriceCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceCount);
			var report = new SpecShortReport(0, "MinCostByPriceCount", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceCount);
		}

		[Test]
		public void DbfMinCostByPriceCountProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceCountProducer);
			var report = new SpecShortReport(0, "MinCostByPriceCountProducer", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceCountProducer);
		}

		[Test]
		public void DbfMinCostByPriceProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceProducer);
			var report = new SpecShortReport(0, "MinCostByPriceProducer", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceProducer);
		}
	}
}