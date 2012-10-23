using System;
using System.Collections.Generic;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class DefectureProfileFixture : BaseProfileFixture
	{
		[Test, Ignore("Прайс-лист не актуален")]
		public void DefectureNameAndForm()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNameAndForm);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNameAndForm);
			DefecturePharmacie.TestReportResultOnPharmacie(report.DSResult);
		}

		[Test, Ignore("Прайс-лист не актуален")]
		public void DefectureNameAndFormWithProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNameAndFormWithProducer);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNameAndFormWithProducer);
			DefecturePharmacie.TestReportResultOnPharmacie(report.DSResult);
		}

		[Test, Ignore("Прайс-лист не актуален")]
		public void DefectureNameOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureNameOnly);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureNameOnly);
			DefecturePharmacie.TestReportResultOnPharmacie(report.DSResult);
		}

		[Test, Ignore("Прайс-лист не актуален")]
		public void DefectureProductsOnly()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureProductsOnly);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureProductsOnly);
			DefecturePharmacie.TestReportResultOnPharmacie(report.DSResult);
		}

		[Test, Ignore("Прайс-лист не актуален")]
		public void DefectureProductsWithProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.DefectureProductsWithProducer);
			var report = new DefReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.DefectureProductsWithProducer);
			DefecturePharmacie.TestReportResultOnPharmacie(report.DSResult);
		}

		[Test]
		public void DefectureByBaseCostNew()
		{
			var fileName = "DefectureByBaseCostNew.xls";
			Property("ReportType", 2);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 7160);
			Property("PriceCode", 4783);
			Property("ByBaseCosts", true);
			report = new DefReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}