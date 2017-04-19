using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using Common.MySql;
using Common.Web.Ui.ActiveRecordExtentions;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Helpers;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Configuration;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	public class ReportFixture : IntegrationFixture
	{
		protected MySqlConnection Conn;
		protected int i;
		protected DataSet properties;
		protected BaseReport report;
		private string _fileName;

		[SetUp]
		public void Start()
		{
			//не удаляем файл после завершения теста что бы можно было посмотреть на него глазами
			if (!String.IsNullOrEmpty(_fileName) && File.Exists(_fileName))
				File.Delete(_fileName);
			File.Delete("test.xls");
			report = null;
			_fileName = null;
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

			Conn = (MySqlConnection)session.Connection;
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
			if (type == "PERCENT") {
			}
			else if (value is string) {
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
			CreateReport(reportType);

			report.Session = session;
			report.CheckEmptyData = checkEmptyData;
			file = file ?? _fileName ?? "test.xls";
			if (File.Exists(file))
				File.Delete(file);
			ProfileHelper.Start();
			report.Write(Path.GetFullPath(file));
			ProfileHelper.Stop();
		}

		private void CreateReport(Type reportType)
		{
			session.Flush();
			report = report ?? (BaseReport)
				Activator.CreateInstance(reportType, Conn, properties);
			report.ReportCaption = report.ReportCaption ?? "test report";
		}

		public void ProcessReport(Type reportType = null, bool checkEmptyData = false)
		{
			CreateReport(reportType);

			report.Session = session;
			report.CheckEmptyData = checkEmptyData;
			report.ReadReportParams();
			report.ProcessReport();
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

		protected void BuildOrderReport(string file = null)
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
			TryInitReport<T>();
			return ReadReport();
		}

		protected ISheet ReadReport(BaseReport report = null, string fileName = "test.xls")
		{
			if (report != null)
				this.report = report;
			BuildReport(fileName);

			var book = Load(fileName);
			var sheet = book.GetSheetAt(0);
			return sheet;
		}

		protected void TryInitReport<T>(string fileName = "test.xls", ReportFormats format = ReportFormats.Excel)
		{
			if (report == null) {
				report = (BaseReport)Activator.CreateInstance(typeof(T), (MySqlConnection)session.Connection, properties);
				report.Format = format;
				report.ReportCaption = "test report";
			}
		}

		public string ToText(ISheet sheet)
		{
			var writer = new StringWriter();
			for(var i = sheet.FirstRowNum; i <= sheet.LastRowNum; i++) {
				var row = sheet.GetRow(i);
				if (row == null)
					continue;
				writer.Write("|");
				for(var j = row.FirstCellNum; j < row.LastCellNum; j++) {
					var cell = row.GetCell(j);
					if (cell != null) {
						if (cell.CellType == CellType.Numeric) {
							writer.Write(cell.NumericCellValue);
						} else {
							writer.Write(cell.StringCellValue);
						}
					}
					writer.Write("|");
				}
				writer.WriteLine();
			}
			return writer.ToString();
		}


		protected TestOrder CreateOrder(TestClient client = null, TestSupplier supplier = null)
		{
			supplier = supplier ?? TestSupplier.CreateNaked(session);
			client = client ?? TestClient.CreateNaked(session);
			var order = new TestOrder(client.Users[0], supplier.Prices[0]);
			var product = session.Query<TestProduct>().First();
			order.WriteTime = order.WriteTime.AddDays(-1);
			order.AddItem(product, 10, 897.23f);
			return order;
		}
	}

	[Obsolete("Используй ReportFixture")]
	public class BaseProfileFixture
	{
		protected MySqlConnection Conn;
		protected int i;
		protected DataSet properties;
		protected BaseReport report;
		private string _fileName;

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
			if (File.Exists(_fileName))
				File.Delete(_fileName);
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
				report = (BaseReport)Activator.CreateInstance(reportType, Conn, properties);
			report.ReportCaption = report.ReportCaption ?? "test report";

			if (file == null)
				file = "test.xls";
			_fileName = file;
			if (File.Exists(file))
				File.Delete(file);
			ProfileHelper.Start();
			report.CheckEmptyData = checkEmptyData;
			ArHelper.WithSession(s => {
				report.Session = s;
				report.Write(Path.GetFullPath(file));
			});
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
