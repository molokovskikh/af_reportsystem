using System;
using System.Collections;
using System.Data;
using System.IO;
using Common.MySql;
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
			Conn = new MySqlConnection(ConfigurationManager.ConnectionStrings[FixtureSetup.ConnectionStringName].ConnectionString);
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
			Conn.Dispose();

			ProfileHelper.End();
		}

		public void Property(string name, object value, string type = null)
		{
			var row = properties.Tables[0].NewRow();
			row["ID"] = i;
			row["PropertyName"] = name;
			row["PropertyValue"] = value;
			if (value is int)
				type = "INT";
			else if (value is bool)
				type = "BOOL";
			else if (value is DateTime)
			{
				type = "DATETIME";
				row["PropertyValue"] = ((DateTime)value).ToString(MySqlConsts.MySQLDateFormat);
			}
			if (value is string)
			{
				type = "STRING";
			}
			else if (value is IEnumerable)
			{
				row["PropertyValue"] = null;
				type = "LIST";
				var table = properties.Tables["ReportPropertyValues"];
				foreach (var item in (IEnumerable)value)
				{
					var valueRow = table.NewRow();
					valueRow["ReportPropertyID"] = i;
					valueRow["Value"] = item;
					table.Rows.Add(valueRow);
				}
			}
			row["PropertyType"] = type;
			i++;
			properties.Tables[0].Rows.Add(row);
		}

		protected void BuildReport(string file = null, Type reportType = null)
		{
			if (reportType != null && report == null)
				report = (BaseReport)Activator.CreateInstance(reportType, 0ul, "Automate Created Report", Conn, false, ReportFormats.Excel, properties);

			if (file == null)
				file = "test.xls";
			if (File.Exists(file))
				File.Delete(file);
			ProfileHelper.Start();
			report.ReadReportParams();
			report.ProcessReport();
			report.ReportToFile(Path.GetFullPath(file));
			ProfileHelper.Stop();
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
	}
}
