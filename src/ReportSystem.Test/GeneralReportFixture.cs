using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using ExecuteTemplate;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using NHibernate.AdoNet;
using NUnit.Framework;
using Test.Support;
using log4net;

namespace ReportSystem.Test
{
	public class FakeReport : BaseReport
	{
		public FakeReport()
		{
			_dsReport = new DataSet();
		}

		public override void GenerateReport(ExecuteArgs e)
		{}

		public override void ReadReportParams()
		{}

		public override void ReportToFile(string fileName)
		{
			File.WriteAllBytes(fileName, new byte[0]);
		}
	}

	public class FakeGeneralReport : GeneralReport
	{
		public DataTable DataTable
		{
			set { _dtReports = value; }
			get { return _dtReports; }
		}
	}

	[TestFixture]
	public class GeneralReportFixture : IntegrationFixture
	{
		[Test]
		public void Archive_additional_files()
		{
			File.WriteAllBytes("description.xls", new byte[0]);

			var report = new GeneralReport();
			report.FilesForReport = new Dictionary<string, string>();
			report.FilesForReport.Add("description.xls", "description.xls");
			report.GeneralReportID = 1;
			report.Reports.Add(new FakeReport());
			var result = report.BuildResultFile();
			var zip = new ZipFile(result);
			var files = zip.Cast<ZipEntry>().Select(e => e.Name).ToArray();
			Assert.That(files.Count(), Is.EqualTo(2));
			Assert.That(files[1], Is.EqualTo("Rep1.xls"));
			Assert.That(files[0], Is.EqualTo("description.xls"));
			zip.Close();
		}

		[Test]
		public void Archive_additional_general_report_files()
		{
			if (File.Exists("description.xls"))
				File.Delete("description.xls");
			session.CreateSQLQuery("insert into reports.filessendwithreport (FileName, Report) value (\"123.txt\", 1)").ExecuteUpdate();
			var id = session.CreateSQLQuery("select LAST_INSERT_ID();").UniqueResult();
			File.WriteAllBytes(id.ToString(), new byte[0]);
			var report = new GeneralReport();
			report.GeneralReportID = 1;
			report.Reports.Add(new FakeReport());
			report.FilesForReport = new Dictionary<string, string>{{"123.txt", id.ToString()}};
			var result = report.BuildResultFile();
			var zip = new ZipFile(result);
			var files = zip.Cast<ZipEntry>().Select(e => e.Name).ToArray();
			Assert.That(files.Count(), Is.EqualTo(2));
			Assert.That(files[1], Is.EqualTo("Rep1.xls"));
			Assert.That(files[0], Is.EqualTo("123.txt"));
		}

		[Test, Description("Проверяет, что файлы, которые указаны для типа отчета добвалены к отчету")]
		public void Archive_files_for_report_type()
		{
			var report = new FakeGeneralReport();
			report.GeneralReportID = 1;
			report.Logger = LogManager.GetLogger(GetType());
			MySqlConnection connection = null;
			object reportTypeCode = null;
			try {
				connection = new MySqlConnection(ConnectionHelper.GetConnectionString());
				connection.Open();
				new MySqlCommand("update reports.reports r set r.SendFile = true where generalreportcode = 1", connection).ExecuteNonQuery();
				new MySqlCommand("delete from reports.filessendwithreport;delete from reports.fileforreporttypes;", connection).ExecuteNonQuery();
				report.DataTable = MethodTemplate.ExecuteMethod(new ExecuteArgs(), report.GetReports, null, connection);
				foreach (DataRow row in report.DataTable.Rows) {
					if (Convert.ToBoolean(row[BaseReportColumns.colSendFile])) {
						reportTypeCode = row[BaseReportColumns.colReportTypeCode];
						new MySqlCommand(string.Format("insert into reports.fileforreporttypes (File, ReportType) value ('testFile{0}', {0})", reportTypeCode), connection).ExecuteNonQuery();
					}
				}
				var files = session.CreateSQLQuery("select id from reports.fileforreporttypes;").List<uint>();
				var filesNames = session.CreateSQLQuery("select File from reports.fileforreporttypes group by File;").List<string>();
				foreach (var file in files) {
					var create = File.Create(file.ToString());
					create.Close();
				}
				Assert.IsNotNull(reportTypeCode);
				var additionalFiles = MethodTemplate.ExecuteMethod(new ExecuteArgs(), report.GetFilesForReports, null, connection);
				Assert.That(additionalFiles.Count, Is.GreaterThan(0));
				Assert.That(additionalFiles.Count, Is.EqualTo(filesNames.Count()));
				foreach (var file in files) {
					File.Delete(file.ToString());
				}
			}
			finally {
				new MySqlCommand("delete from reports.filessendwithreport;delete from reports.fileforreporttypes;", connection).ExecuteNonQuery();
				if (connection != null) connection.Close();
			}
		}

		[Test]
		public void Do_not_copy_to_ftp_if_supplier_unknown()
		{
			var report = new GeneralReport();
			report.GeneralReportID = 1;
			report.Reports.Add(new FakeReport());
			report.CopyFileToFtp("", "");
		}
	}
}