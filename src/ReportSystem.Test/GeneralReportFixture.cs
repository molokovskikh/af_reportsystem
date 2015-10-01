using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Common.Web.Ui.Models;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Model;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;
using ContactType = Common.Web.Ui.Models.ContactType;

namespace ReportSystem.Test
{
	public class FakeReport : BaseReport
	{
		public string OverideDefaultFilename;

		public FakeReport()
		{
			_dsReport = new DataSet();
		}

		protected override void GenerateReport()
		{
		}

		public override void ReadReportParams()
		{
		}

		public override void ReportToFile(string fileName)
		{
			if (!String.IsNullOrEmpty(OverideDefaultFilename))
				fileName = Path.Combine(Path.GetDirectoryName(fileName), OverideDefaultFilename);

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
			report.Id = 1;

			report.EMailSubject = "test";
			report.Contacts = new[] { "kvasovtest@analit.net" };
			report.Testing = true;
			report.Connection = (MySqlConnection)session.Connection;
			FileHelper.InitDir("History");
		}

		[Test]
		public void Archive_additional_files()
		{
			AddFile();

			report.Reports.Enqueue(new FakeReport());
			var file = report.BuildResultFile()[0];

			var files = LsZip(file);
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

			report.Reports.Enqueue(new FakeReport());
			report.FilesForReport = new Dictionary<string, string> { { "123.txt", id.ToString() } };

			var result = report.BuildResultFile()[0];
			var files = LsZip(result);
			Assert.That(files.Count(), Is.EqualTo(2));
			Assert.That(files[1], Is.EqualTo("Rep1.xls"));
			Assert.That(files[0], Is.EqualTo("123.txt"));
		}

		[Test]
		public void Send_description_with_no_archive()
		{
			AddFile();

			report.NoArchive = true;
			report.Reports.Enqueue(new FakeReport());
			report.ProcessReports(new ReportExecuteLog(), (MySqlConnection)session.Connection, false, DateTime.Today, DateTime.Today, false);

			Assert.That(report.Messages.Count, Is.EqualTo(1));
			var message = report.Messages[0];
			var attachments = message.Attachments.Select(a => a.ContentDisposition_FileName).Implode();
			Assert.That(attachments, Is.EqualTo("description.xls, Rep1.xls"));
		}

		[Test]
		public void Mail_per_file()
		{
			report.NoArchive = true;
			report.MailPerFile = true;
			report.Reports.Enqueue(new FakeReport { OverideDefaultFilename = "1.dbf" });
			report.Reports.Enqueue(new FakeReport { OverideDefaultFilename = "2.dbf" });
			report.ProcessReports(new ReportExecuteLog(), (MySqlConnection)session.Connection, false, DateTime.Today, DateTime.Today, false);

			Assert.That(report.Messages.Count, Is.EqualTo(2));
		}

		[Test]
		public void Send_all_report_files_if_not_archive_option_set()
		{
			var fakeReport = new FakeReport();
			fakeReport.OverideDefaultFilename = "1.csv";

			report.NoArchive = true;
			report.Reports.Enqueue(fakeReport);
			report.ProcessReports(new ReportExecuteLog(), (MySqlConnection)session.Connection, false, DateTime.Today, DateTime.Today, false);

			Assert.That(report.Messages.Count, Is.EqualTo(1));
			var message = report.Messages[0];
			Assert.That(message.Attachments.Length, Is.EqualTo(1));
			Assert.That(message.Attachments[0].ContentDisposition_FileName, Is.EqualTo("1.csv"));
		}

		[Test, Description("Проверяет, что файлы, которые указаны для типа отчета добвалены к отчету")]
		public void Archive_files_for_report_type()
		{
			var connection = (MySqlConnection)session.Connection;
			try {
				var report = new FakeGeneralReport();
				report.Id = 1;
				report.SendDescriptionFile = true;
				report.Connection = connection;
				object reportTypeCode = null;

				new MySqlCommand("update reports.general_reports r set r.SendDescriptionFile = true where generalreportcode = 1", connection).ExecuteNonQuery();
				new MySqlCommand("update reports.reports r set r.Enabled = true where generalreportcode = 1", connection).ExecuteNonQuery();
				new MySqlCommand("delete from reports.filessendwithreport;delete from reports.fileforreporttypes;", connection).ExecuteNonQuery();
				report.DataTable = report.GetReports();
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
				var additionalFiles = report.GetFilesForReports();
				Assert.That(additionalFiles.Count, Is.GreaterThan(0));
				Assert.That(additionalFiles.Count, Is.EqualTo(filesNames.Count()));
				foreach (var file in files) {
					File.Delete(file.ToString());
				}
			}
			finally {
				new MySqlCommand("delete from reports.filessendwithreport;delete from reports.fileforreporttypes;", connection).ExecuteNonQuery();
			}
		}

		[Test]
		public void Do_not_copy_to_ftp_if_supplier_unknown()
		{
			report.Reports.Enqueue(new FakeReport());
			report.CopyFileToFtp(new[] { "" });
		}

		[Test]
		public void Save_report_file()
		{
			FileHelper.InitDir("history");
			report.Reports.Enqueue(new FakeReport());
			report.ProcessReports(new ReportExecuteLog { Id = 1 }, (MySqlConnection)session.Connection, false, DateTime.Today, DateTime.Today, false);

			var files = Directory.GetFiles("history");
			Assert.That(files.Length, Is.EqualTo(1), files.Implode());
		}

		[Test]
		public void Log_success()
		{
			report.LogSuccess();
		}

		[Test]
		public void Collect_contacts()
		{
			//моделируем ситуацию если не задана группа рассылки а есть только публичные контакты
			var report = new GeneralReport {
				PublicSubscriptions = new ContactGroup {
					Contacts = { new Contact(ContactType.Email, "test@analit.net") }
				}
			};
			report.CollectContacts();
			Assert.AreEqual(report.Contacts[0], "test@analit.net");
			Assert.AreEqual(1, report.Contacts.Length);
		}

		[Test]
		public void Collect_contacts_from_groups()
		{
			var report = new GeneralReport {
				ContactGroup = new ContactGroup {
					Contacts = { new Contact(ContactType.Email, "test1@analit.net") }
				},
				PublicSubscriptions = new ContactGroup {
					Contacts = { new Contact(ContactType.Email, "test@analit.net") }
				}
			};
			report.CollectContacts();
			var contacts = report.Contacts.Implode();
			Assert.AreEqual(contacts, "test1@analit.net, test@analit.net");
		}

		[Test]
		public void TestArchBase()
		{
			var gr = new GeneralReport();
			gr.NoArchive = true;
			gr.Reports.Enqueue(new FakeReport());
			var file = gr.BuildResultFile()[0];
			Assert.That(Path.GetExtension(file), Is.EqualTo(".xls"));
			gr = new GeneralReport();
			gr.Reports.Enqueue(new FakeReport());
			file = gr.BuildResultFile()[0];
			Assert.That(Path.GetExtension(file), Is.EqualTo(".zip"));
		}

		[Test]
		public void Release_report_references()
		{
			var report = new GeneralReport();
			report.NoArchive = true;
			report.Reports.Enqueue(new FakeReport());
			report.BuildResultFile();
			Assert.AreEqual(0, report.Reports.Count);
		}

		[Test]
		public void Arhive_per_file()
		{
			var files = new[] {
				"tmp/1.dbf", "tmp/2.dbf"
			};
			FileHelper.InitDir("tmp");
			FileHelper.Touch(files);
			var report = new GeneralReport();
			report.ReportArchName = "test.zip";
			report.WorkDir = "tmp";
			report.MailPerFile = true;
			var result = report.ArchFile(files);
			Assert.AreEqual("tmp/1.zip, tmp/2.zip", result.Implode());
		}

		private static string[] LsZip(string result)
		{
			using (var zip = new ZipFile(result)) {
				var files = zip.Cast<ZipEntry>().Select(e => e.Name).ToArray();
				return files;
			}
		}

		private void AddFile()
		{
			File.WriteAllBytes("description.xls", new byte[0]);
			report.FilesForReport.Add("description.xls", "description.xls");
		}
	}
}