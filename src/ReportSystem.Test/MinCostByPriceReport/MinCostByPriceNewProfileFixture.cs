using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MinCostByPriceNewProfileFixture : BaseProfileFixture
	{
		int i;

		[Test]
		public void MinCostByPriceNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNew);
		}

		[Test]
		public void MinCostByPriceNewWithClients()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewWithClients);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewWithClients);
		}

		[Test]
		public void With_ignored_suppliers()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			AddProperty(props, "IgnoredSuppliers", new [] {5, 7});
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNew);
		}

		public void AddProperty(DataSet properties, string name, object value)
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
				if (table == null)
				{
					var values = properties.Tables.Add("ReportPropertyValues");
					values.Columns.Add("ReportPropertyID");
					values.Columns.Add("Value");
					table = values;
				}
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


		[Test]
		public void MinCostByPriceNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewDifficult);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewDifficult);
		}
	}
}
