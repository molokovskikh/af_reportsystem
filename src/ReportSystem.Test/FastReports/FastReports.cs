using System.Collections;
using System.Collections.Generic;
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
		private int i;

		[SetUp]
		public void Setup()
		{
			i = 1;
			ProfileHelper.Start();
			properties = new DataSet();
			var table = properties.Tables.Add("ReportProperties");
			table.Columns.Add("PropertyName");
			table.Columns.Add("PropertyValue");
			table.Columns.Add("PropertyType");
			table.Columns.Add("ID");
			var values = properties.Tables.Add("ReportPropertyValues");
			values.Columns.Add("ReportPropertyID");
			values.Columns.Add("Value");
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

		public void AddProperty(string name, object value)
		{
			var row = properties.Tables[0].NewRow();
			row["ID"] = i;
			row["PropertyName"] = name;
			row["PropertyValue"] = value;
			if (value is int)
				row["PropertyType"] = "INT";
			else if (value is bool)
				row["PropertyType"] = "BOOL";
			else if (value is IEnumerable)
			{
				row["PropertyValue"] = null;
				row["PropertyType"] = "LIST";
				var table = properties.Tables["ReportPropertyValues"];
				foreach (var item in (IEnumerable)value)
				{
					var valueRow = table.NewRow();
					valueRow["ReportPropertyID"] = i;
					valueRow["Value"] = item;
					table.Rows.Add(valueRow);
				}
			}
			i++;
			properties.Tables[0].Rows.Add(row);
		}

		private void BuildReport()
		{
			var report = new PharmacyOffersReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, properties);
			TestHelper.ProcessReport(report, ReportsTypes.PharmacyOffers);
		}
	}
}
