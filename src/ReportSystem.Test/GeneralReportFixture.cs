using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using ExecuteTemplate;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.ReportSystem;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	public class FakeReport : BaseReport
	{
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
			File.WriteAllBytes(fileName, new byte[0]);
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
			report.FilesForReport = new Dictionary<string, string> { { "123.txt", id.ToString() } };
			var result = report.BuildResultFile();
			var zip = new ZipFile(result);
			var files = zip.Cast<ZipEntry>().Select(e => e.Name).ToArray();
			Assert.That(files.Count(), Is.EqualTo(2));
			Assert.That(files[1], Is.EqualTo("Rep1.xls"));
			Assert.That(files[0], Is.EqualTo("123.txt"));
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