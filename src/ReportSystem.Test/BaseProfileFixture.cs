using System;
using System.Collections;
using System.Data;
using System.IO;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.FastReports;
using Inforoom.ReportSystem.Helpers;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace ReportSystem.Test
{
	public class BaseProfileFixture
	{
		protected MySqlConnection Conn;
		protected int i;
		protected DataSet properties;
		protected BaseReport report;

		[SetUp]
		public void Start()
		{
			Conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
			Conn.Open();

			i = 1;
			ProfileHelper.Start();
			properties = new DataSet();
			var table = properties.Tables.Add("ReportProperties");
			table.Columns.Add("PropertyName");
			table.Columns.Add("PropertyValue", typeof(object));
			table.Columns.Add("PropertyType");
			table.Columns.Add("ID");
			var values = properties.Tables.Add("ReportPropertyValues");
			values.Columns.Add("ReportPropertyID");
			values.Columns.Add("Value");

		}

		[TearDown]
		public void Stop()
		{
			Conn.Close();
			Conn.Dispose();

			ProfileHelper.End();
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
            else if (value is DateTime)
            {
                row["PropertyType"] = "DATETIME";
                row["PropertyValue"] = ((DateTime)value).ToString("yyyy-MM-dd");
            }
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

		protected void BuildReport()
		{
			if (File.Exists("test.xls"))
				File.Delete("test.xls");
			ProfileHelper.Start();
			report.ReadReportParams();
			report.ProcessReport();
			report.ReportToFile(Path.GetFullPath("test.xls"));
			ProfileHelper.Stop();

		}
	}
}
