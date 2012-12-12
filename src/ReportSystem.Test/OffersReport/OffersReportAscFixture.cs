using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OffersReportAscFixture : BaseProfileFixture
	{
		[Test]
		public void OffersReportAscByProducerCount()
		{
			var fileName = "OffersReportByProducerCount.xls";
			Property("ReportType", 4);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 4);
			report = new OffersReportAsc(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OffersReportAscByProducer()
		{
			var fileName = "OffersReportAscByProducer.xls";
			Property("ReportType", 3);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 4);
			report = new OffersReportAsc(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
		[Test]
		public void OffersReportAscByCount()
		{
			var fileName = "OffersReportAscByCount.xls";
			Property("ReportType", 2);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 4);
			report = new OffersReportAsc(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OffersReportAsc()
		{
			var fileName = "OffersReportAsc.xls";
			Property("ReportType", 1);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 4);
			report = new OffersReportAsc(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OffersReportAscByProducerCountBaseCost()
		{
			var fileName = "OffersReportAscByProducerCountBaseCost.xls";
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
			report = new OffersReportAsc(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void OffersReportAscByProducerCountWeightCost()
		{
			var fileName = "OffersReportAscByProducerCountWeightCost.xls";
			Property("ReportType", 4);
			Property("ClientCode", 5101);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 5699);
			Property("ReportIsFull", false);
			Property("MaxCostCount", 6);
			Property("ByWeightCosts", true);
			Property("RegionEqual", new List<ulong> {
				4194304
			});
			report = new OffersReportAsc(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}
