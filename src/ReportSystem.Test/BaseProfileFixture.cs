﻿using System;
using System.Collections;
using System.Data;
using System.IO;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Web.Ui.ActiveRecordExtentions;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.FastReports;
using Inforoom.ReportSystem.Helpers;
using NHibernate;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Configuration;
using Test.Support;

namespace ReportSystem.Test
{
	public class BaseProfileFixture2 : IntegrationFixture
	{
		protected MySqlConnection Conn;
		protected int i;
		protected DataSet properties;
		protected BaseReport report;

		[SetUp]
		public void Start()
		{
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
			ProfileHelper.End();
		}

		public void Property(string name, object value, string type = null)
		{
			var row = properties.Tables[0].NewRow();
			row["ID"] = i;
			row["PropertyName"] = name;
			row["PropertyValue"] = value;
			if (value is int || value is uint)
				type = "INT";
			else if (value is bool)
				type = "BOOL";
			else if (value is DateTime) {
				type = "DATETIME";
				row["PropertyValue"] = ((DateTime)value).ToString(MySqlConsts.MySQLDateFormat);
			}
			if (value is string) {
				type = "STRING";
			}
			else if (value is IEnumerable) {
				row["PropertyValue"] = null;
				type = "LIST";
				var table = properties.Tables["ReportPropertyValues"];
				foreach (var item in (IEnumerable)value) {
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

		protected void BuildReport(string file = null, Type reportType = null, bool checkEmptyData = false)
		{
			Conn = (MySqlConnection)session.Connection;
			session.Flush();
			session.Transaction.Commit();
			if (reportType != null && report == null)
				report = (BaseReport)Activator.CreateInstance(reportType, 0ul, "Automate Created Report", Conn, ReportFormats.Excel, properties);

			if (file == null)
				file = "test.xls";
			if (File.Exists(file))
				File.Delete(file);
			ProfileHelper.Start();
			report.Session = session;
			report.CheckEmptyData = checkEmptyData;
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
			else if (value is IEnumerable) {
				row["PropertyValue"] = null;
				row["PropertyType"] = "LIST";
				var table = properties.Tables["ReportPropertyValues"];
				if (table == null) {
					var values = properties.Tables.Add("ReportPropertyValues");
					values.Columns.Add("ReportPropertyID");
					values.Columns.Add("Value");
					table = values;
				}
				foreach (var item in (IEnumerable)value) {
					var valueRow = table.NewRow();
					valueRow["ReportPropertyID"] = i;
					valueRow["Value"] = item;
					table.Rows.Add(valueRow);
				}
			}
			i++;
			properties.Tables[0].Rows.Add(row);
		}

		protected void BuildOrderReport(string file)
		{
			report.CheckEmptyData = false;
			report.From = DateTime.Today.AddDays(-10);
			report.To = DateTime.Today;
			report.Interval = true;
			BuildReport(file);
		}

		protected static HSSFWorkbook Load(string name)
		{
			using(var stream = File.OpenRead(name))
				return new HSSFWorkbook(stream);
		}

		protected ISheet ReadReport<T>()
		{
			var fileName = "test.xls";
			InitReport<T>(fileName);
			BuildReport(fileName);

			var book = Load(fileName);
			var sheet = book.GetSheetAt(0);
			return sheet;
		}

		protected void InitReport<T>(string fileName)
		{
			report = (BaseReport)Activator.CreateInstance(typeof(T), 1ul, fileName, (MySqlConnection)session.Connection, ReportFormats.Excel, properties);
		}

		public string ToText(ISheet sheet)
		{
			var writer = new StringWriter();
			for(var i = sheet.FirstRowNum; i < sheet.LastRowNum; i++) {
				var row = sheet.GetRow(i);
				writer.Write("|");
				for(var j = row.FirstCellNum; j < row.LastCellNum; j++) {
					var cell = row.GetCell(j);
					if (cell.CellType == CellType.NUMERIC) {
						writer.Write(cell.NumericCellValue);
					}
					else {
						writer.Write(cell.StringCellValue);
					}
					writer.Write("|");
				}
				writer.WriteLine();
			}
			return writer.ToString();
		}
	}

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
			if (value is int || value is uint)
				type = "INT";
			else if (value is bool)
				type = "BOOL";
			else if (value is DateTime) {
				type = "DATETIME";
				row["PropertyValue"] = ((DateTime)value).ToString(MySqlConsts.MySQLDateFormat);
			}
			if (value is string) {
				type = "STRING";
			}
			else if (value is IEnumerable) {
				row["PropertyValue"] = null;
				type = "LIST";
				var table = properties.Tables["ReportPropertyValues"];
				foreach (var item in (IEnumerable)value) {
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

		protected void BuildReport(string file = null, Type reportType = null, bool checkEmptyData = false)
		{
			if (reportType != null && report == null)
				report = (BaseReport)Activator.CreateInstance(reportType, 0ul, "Automate Created Report", Conn, ReportFormats.Excel, properties);

			if (file == null)
				file = "test.xls";
			if (File.Exists(file))
				File.Delete(file);
			ProfileHelper.Start();
			report.CheckEmptyData = checkEmptyData;
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
			else if (value is IEnumerable) {
				row["PropertyValue"] = null;
				row["PropertyType"] = "LIST";
				var table = properties.Tables["ReportPropertyValues"];
				if (table == null) {
					var values = properties.Tables.Add("ReportPropertyValues");
					values.Columns.Add("ReportPropertyID");
					values.Columns.Add("Value");
					table = values;
				}
				foreach (var item in (IEnumerable)value) {
					var valueRow = table.NewRow();
					valueRow["ReportPropertyID"] = i;
					valueRow["Value"] = item;
					table.Rows.Add(valueRow);
				}
			}
			i++;
			properties.Tables[0].Rows.Add(row);
		}

		protected void BuildOrderReport(string file)
		{
			report.CheckEmptyData = false;
			report.From = DateTime.Today.AddDays(-10);
			report.To = DateTime.Today;
			report.Interval = true;
			BuildReport(file);
		}
	}
}