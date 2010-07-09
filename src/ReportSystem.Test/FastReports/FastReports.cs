using NUnit.Framework;
using Inforoom.ReportSystem.FastReports;
using Inforoom.ReportSystem;

namespace ReportSystem.Test.FastReports
{
	[TestFixture]
	public class FastReports : BaseProfileFixture
	{
		[SetUp]
		public void Setup()
		{
			report = new PharmacyOffersReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, properties);
		}

		[Test]
		public void Build_report_with_producer()
		{
			AddProperty("IncludeQuantity", 1);
			AddProperty("IncludeProducer", 1);
			AddProperty("ClientCode", 2575);
			BuildReport();
		}

		[Test]
		public void Build_report_without_producer()
		{
			AddProperty("IncludeQuantity", 1);
			AddProperty("IncludeProducer", 0);
			AddProperty("ClientCode", 2575);
			BuildReport();
		}

		[Test]
		public void Build_report_with_cost_diff_threshold()
		{
			AddProperty("IncludeQuantity", 1);
			AddProperty("IncludeProducer", 1);
			AddProperty("CostDiffThreshold", 10);
			AddProperty("ClientCode", 2575);
			BuildReport();
		}

		[Test]
		public void Build_report_with_ignored_suppliers()
		{
			AddProperty("IncludeQuantity", 1);
			AddProperty("IncludeProducer", 1);
			AddProperty("ClientCode", 2575);
			AddProperty("IgnoredSuppliers", new [] {5, 7});
			BuildReport();
		}
	}
}
