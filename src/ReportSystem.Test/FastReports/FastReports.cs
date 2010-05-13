using System.Data;
using Inforoom.ReportSystem.Helpers;
using NUnit.Framework;
using Inforoom.ReportSystem.FastReports;
using Inforoom.ReportSystem;

namespace ReportSystem.Test.FastReports
{
	[TestFixture]
	public class FastReports : BaseProfileFixture
	{
		private DataSet properties;

		[SetUp]
		public void Setup()
		{
			ProfileHelper.Start();
			properties = new DataSet();
			var table = properties.Tables.Add("ReportProperties");
			table.Columns.Add("PropertyName");
			table.Columns.Add("PropertyValue");
			table.Columns.Add("PropertyType");
		}

		[TearDown]
		public void TearDown()
		{
			ProfileHelper.End();
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
			AddProperty("CostDiffTheshold", 10);
			AddProperty("ClientCode", 2575);
			BuildReport();
		}

		public void AddProperty(string name, object value)
		{
			var row = properties.Tables[0].NewRow();
			row["PropertyName"] = name;
			row["PropertyValue"] = value;
			if (value is int)
				row["PropertyType"] = "INT";
			else if (value is bool)
				row["PropertyType"] = "BOOL";
			properties.Tables[0].Rows.Add(row);
		}

		private void BuildReport()
		{
			var report = new PharmacyOffersReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, properties);
			TestHelper.ProcessReport(report, ReportsTypes.PharmacyOffers);
		}
	}
}
