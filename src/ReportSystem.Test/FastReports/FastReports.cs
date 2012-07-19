using System.Data;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Inforoom.ReportSystem.FastReports;
using Inforoom.ReportSystem;

namespace ReportSystem.Test.FastReports
{
	[TestFixture]
	public class FastReports : BaseProfileFixture
	{
		private void CreateReport()
		{
			report = new PharmacyOffersReport(0, "Automate Created Report", Conn, ReportFormats.Excel, properties);
		}

		[Test]
		public void Build_report_with_producer()
		{
			Property("IncludeQuantity", 1);
			Property("IncludeProducer", 1);
			Property("ClientCode", 2575);
			CreateReport();
			BuildReport();
		}

		[Test]
		public void Build_report_without_producer()
		{
			Property("IncludeQuantity", 1);
			Property("IncludeProducer", 0);
			Property("ClientCode", 2575);
			CreateReport();
			BuildReport();
		}

		[Test]
		public void Build_report_with_cost_diff_threshold()
		{
			Property("IncludeQuantity", 1);
			Property("IncludeProducer", 1);
			Property("CostDiffThreshold", 10);
			Property("ClientCode", 2575);
			CreateReport();
			BuildReport();
		}

		[Test]
		public void Build_report_with_ignored_suppliers()
		{
			Property("IncludeQuantity", 1);
			Property("IncludeProducer", 1);
			Property("ClientCode", 2575);
			Property("IgnoredSuppliers", new [] {5, 7});
			CreateReport();
			BuildReport();
		}

		[Test]
		public void Build_report_with_pricelist()
		{
			Property("IncludeQuantity", 1);
			Property("IncludeProducer", 1);
			Property("ClientCode", 2575);
			Property("PriceCode", 4649);
			CreateReport();
			BuildReport();
		}

		[Test]
		public void Build_report_with_pricelist_without_producer_and_quantity()
		{
			Property("IncludeQuantity", 0);
			Property("IncludeProducer", 0);
			Property("ClientCode", 2575);
			Property("PriceCode", 4649);
			CreateReport();
			BuildReport();
		}

		[Test]
		public void Build_full_report_with_pricelist()
		{
			Property("IncludeQuantity", 1);
			Property("IncludeProducer", 1);
			Property("ClientCode", 2575);
			Property("PriceCode", 4649);
			Property("ReportIsFull", 1);
			CreateReport();
			BuildReport();
		}

		[Test]
		public void Build_full_report_with_pricelist_without_producer_and_quantity()
		{
			Property("IncludeQuantity", 0);
			Property("IncludeProducer", 0);
			Property("ClientCode", 2575);
			Property("PriceCode", 4649);
			Property("ReportIsFull", 1);
			CreateReport();
			BuildReport();
		}

		[Test]
		[ExpectedException(typeof(ReportException), ExpectedMessage = "Не найден прайс-лист с кодом: 0.")]
		public void Build_report_with_non_exists_pricelist()
		{
			Property("IncludeQuantity", 0);
			Property("IncludeProducer", 0);
			Property("ClientCode", 2575);
			Property("PriceCode", 0);
			CreateReport();
			BuildReport();
		}

		[Test]
		[ExpectedException(typeof(ReportException), ExpectedMessage = "(1) нет предложений.", MatchType = MessageMatch.Contains)]
		public void Build_report_with_pricelist_without_offers()
		{
			Property("IncludeQuantity", 0);
			Property("IncludeProducer", 0);
			Property("ClientCode", 2575);
			Property("PriceCode", 1);
			CreateReport();
			BuildReport();
		}
	}
}
