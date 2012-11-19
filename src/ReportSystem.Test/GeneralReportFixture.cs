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
		public string OverideDefaultFilename;

		public FakeReport()
		{
			_dsReport = new DataSet();
		}

		public override void GenerateReport(ExecuteArgs e)
		{
		}

		public override void ReadReportParams()
		{
		}

		public override void ReportToFile(string fileName)
		{
			if (!String.IsNullOrEmpty(OverideDefaultFilename))
				fileName = OverideDefaultFilename;

			File.WriteAllBytes(fileName, new byte[0]);
		}
	}

	public class FakeGeneralReport : GeneralReport
	{
		public DataTable DataTable
		{
			set { _reports = value; }
			get { return _reports; }
		}
	}

	[TestFixture]
	public class GeneralReportFixture : IntegrationFixture
	{
		private GeneralReport report;

		[SetUp]
		public void Setup()
		{
			report = new GeneralReport();
			report.GeneralReportID = 1;
		}

		[Test]
		public void Archive_additional_files()
		{
			File.WriteAllBytes("description.xls", new byte[0]);

			report.FilesForReport = new Dictionary<string, string>();
			report.FilesForReport.Add("description.xls", "description.xls");
			report.Reports.Add(new FakeReport());
			var result = report.BuildResultFile();

			var files = LsZip(result);
			Assert.That(files.Count(), Is.EqualTo(2));
			Assert.That(files[1], Is.EqualTo("Rep1.xls"));
			Assert.That(files[0], Is.EqualTo("description.xls"));
		}

		[Test]
		public void Archive_additional_general_report_files()
		{
			if (File.Exists("description.xls"))
				File.Delete("description.xls");
			session.CreateSQLQuery("insert into reports.filessendwithreport (FileName, Report) value (\"123.txt\", 1)").ExecuteUpdate();
			var id = session.CreateSQLQuery("select LAST_INSERT_ID();").UniqueResult();
			File.WriteAllBytes(id.ToString(), new byte[0]);

			report.Reports.Add(new FakeReport());
			report.FilesForReport = new Dictionary<string, string> { { "123.txt", id.ToString() } };

			var result = report.BuildResultFile();
			var files = LsZip(result);
			Assert.That(files.Count(), Is.EqualTo(2));
			Assert.That(files[1], Is.EqualTo("Rep1.xls"));
			Assert.That(files[0], Is.EqualTo("123.txt"));
		}

		private static string[] LsZip(string result)
		{
			using(var zip = new ZipFile(result)) {
				var files = zip.Cast<ZipEntry>().Select(e => e.Name).ToArray();
				return files;
			}
		}

		[Test]
		public void Send_all_report_files_if_not_archive_option_set()
		{
			var fakeReport = new FakeReport();
			fakeReport.OverideDefaultFilename = Path.Combine(Path.GetTempPath(), "Rep1", "1.csv");

			var contacts = new DataTable();
			contacts.Columns.Add("Contact");
			contacts.Rows.Add("kvasovtest@analit.net");
			report.EMailSubject = "test";
			report.Contacts = contacts;
			report.NoArchive = true;
			report.Reports.Add(fakeReport);
			report.Testing = true;
			report.ProcessReports();

			Assert.That(report.Messages.Count, Is.EqualTo(1));
			var message = report.Messages[0];
			Assert.That(message.Attachments.Length, Is.EqualTo(1));
			Assert.That(message.Attachments[0].ContentDisposition_FileName, Is.EqualTo("1.csv"));
		}

		[Test, Description("Проверяет, что файлы, которые указаны для типа отчета добвалены к отчету")]
		public void Archive_files_for_report_type()
		{
			var report = new FakeGeneralReport();
			report.GeneralReportID = 1;
			report.SendDescriptionFile = true;
			report.Logger = LogManager.GetLogger(GetType());
			MySqlConnection connection = null;
			object reportTypeCode = null;
			try {
				connection = new MySqlConnection(ConnectionHelper.GetConnectionString());
				connection.Open();
				new MySqlCommand("update reports.general_reports r set r.SendDescriptionFile = true where generalreportcode = 1", connection).ExecuteNonQuery();
				new MySqlCommand("update reports.reports r set r.Enabled = true where generalreportcode = 1", connection).ExecuteNonQuery();
				new MySqlCommand("delete from reports.filessendwithreport;delete from reports.fileforreporttypes;", connection).ExecuteNonQuery();
				report.DataTable = MethodTemplate.ExecuteMethod(new ExecuteArgs(), report.GetReports, null, connection);
				foreach (DataRow row in report.DataTable.Rows) {
					reportTypeCode = row[BaseReportColumns.colReportTypeCode];
					new MySqlCommand(string.Format("insert into reports.fileforreporttypes (File, ReportType) value ('testFile{0}', {0})", reportTypeCode), connection).ExecuteNonQuery();
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