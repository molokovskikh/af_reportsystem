using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using ReportSystem.Profiling;

namespace ReportSystem.Test
{
	[TestFixture]
	public class DefectureProfileFixture : BaseProfileFixture
	{
		[Test]
		public void DefectureNameAndForm()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNameAndForm);
			var report = new DefReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNameAndForm);

		}

		[Test]
		public void DefectureNameAndFormWithProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNameAndFormWithProducer);
			var report = new DefReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNameAndFormWithProducer);
		}

		[Test]
		public void DefectureNameOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNameOnly);
			var report = new DefReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNameOnly);
		}

		[Test]
		public void DefectureProductsOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureProductsOnly);
			var report = new DefReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureProductsOnly);
		}

		[Test]
		public void DefectureProductsWithProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureProductsWithProducer);
			var report = new DefReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureProductsWithProducer);
		}
	}
}
