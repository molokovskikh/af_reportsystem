using System;
using System.Data;
using System.IO;
using System.Reflection;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture, Ignore("Что бы подебажить отчет")]
	public class Troubleshoot
	{
		private string connectionString = "Database=usersettings;Data Source=localhost;User Id=root;Password=;pooling=false; default command timeout=0;Allow user variables=true;convert zero datetime=yes;";

		[SetUp]
		public void Setup()
		{
			if (File.Exists("test.xls"))
				File.Delete("test.xls");
		}

		[Test]
		public void shoot_it()
		{
			uint reportcode = 1215;
			using (var connection = new MySqlConnection(connectionString)) {
				connection.Open();
				var loader = new ReportPropertiesLoader();
				var prop = loader.LoadProperties(connection, reportcode);
				prop.WriteXml("TestData\\OrderOutAllowedAssortment.xml");
/*				var row = prop.Tables[0].NewRow();
				row["PropertyName"] = "StartDate";
				row["PropertyValue"] = "2010-05-01";
				row["PropertyType"] = "DATETIME";
				prop.Tables[0].Rows.Add(row);
				row = prop.Tables[0].NewRow();
				row["PropertyName"] = "EndDate";
				row["PropertyValue"] = "2010-06-01";
				row["PropertyType"] = "DATETIME";
				prop.Tables[0].Rows.Add(row);*/
				/*var report = new SpecShortReport(reportcode, "test", connection, false, ReportFormats.Excel, prop);
				report.ProcessReport();
				report.ReportToFile(Path.GetFullPath("test.xls"));*/
			}
		}

		[Test]
		public void Troubleshoot_general_report()
		{
			uint id = 2850;
			var dataAdapter = new MySqlDataAdapter("", connectionString);
			dataAdapter.SelectCommand.CommandText = @"
select
  * 
from
  reports.Reports r,
  reports.reporttypes rt
where
    r.GeneralReportCode = ?reportcode
and rt.ReportTypeCode = r.ReportTypeCode";
			dataAdapter.SelectCommand.Parameters.AddWithValue("?reportcode", id);
			var res = new DataTable();
			dataAdapter.Fill(res);

			using (var connection = new MySqlConnection(connectionString)) {
				foreach (DataRow drGReport in res.Rows) {
					if (Convert.ToBoolean(drGReport[BaseReportColumns.colEnabled])) {
						var loader = new ReportPropertiesLoader();

						//Создаем отчеты и добавляем их в список отчетов
						var reportcode = (ulong)drGReport[BaseReportColumns.colReportCode];
						Console.WriteLine("Отчет {0}", reportcode);
						var prop = loader.LoadProperties(connection, reportcode);
						var bs = (BaseReport)Activator.CreateInstance(
							GetReportTypeByName(drGReport[BaseReportColumns.colReportClassName].ToString()),
							new object[] {
								reportcode,
								drGReport[BaseReportColumns.colReportCaption].ToString(), connection,
								false, ReportFormats.Excel,
								prop
							});
						bs.ReadReportParams();
						bs.ProcessReport();
						bs.ReportToFile(Path.GetFullPath("test.xls"));
					}
				}
			}
		}

		private Type GetReportTypeByName(string ReportTypeClassName)
		{
			Type t = Assembly.Load("ReportSystem").GetType(ReportTypeClassName);
			if (t == null)
				throw new ReportException(String.Format("Неизвестный тип отчета : {0}", ReportTypeClassName));
			return t;
		}
	}
}