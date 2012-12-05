using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OffersReportFixture : BaseProfileFixture
	{
		[Test, Ignore("Временно, выполняется слишком долго")]
		public void Offers_report_to_excel()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OffersReport);
			var report = new OffersReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.OffersReport);
		}

		[Test, Ignore("Временно, выполняется слишком долго")]
		public void Offers_report_to_dbf()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.OffersReport);
			var report = new OffersReport(0, "Automate Created Report", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.OffersReport);
		}

		[Test]
		public void OffersReportByProducerCount()
		{
			var fileName = "OffersReportByProducerCount.xls";
			Property("ReportType", 4);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 4);
			report = new OffersReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OffersReportByProducer()
		{
			var fileName = "OffersReportByProducer.xls";
			Property("ReportType", 3);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 4);
			report = new OffersReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
		[Test]
		public void OffersReportByCount()
		{
			var fileName = "OffersReportByCount.xls";
			Property("ReportType", 2);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 4);
			report = new OffersReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OffersReport()
		{
			var fileName = "OffersReport.xls";
			Property("ReportType", 1);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 4);
			report = new OffersReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OffersReportByProducerCountBaseCost()
		{
			var fileName = "OffersReportByProducerCountBaseCost.xls";
			Property("ReportType", 4);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 6);
			Property("ByBaseCosts", true);
			Property("RegionEqual", new List<ulong> {
				4194304
			});
			report = new OffersReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OffersReportByProducerCountWeightCost()
		{
			var fileName = "OffersReportByProducerCountWeightCost.xls";
			Property("ReportType", 4);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 5699);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 6);
			Property("ByWeightCosts", true);
			Property("RegionEqual", new List<ulong> {
				1
			});
			report = new OffersReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}