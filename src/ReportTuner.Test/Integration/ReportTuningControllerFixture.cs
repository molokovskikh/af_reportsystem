using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Framework.Test;
using Castle.MonoRail.TestSupport;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Test.Controllers;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ReportTuner.Controllers;
using ReportTuner.Models;

namespace ReportTuner.Test.Integration
{
	public class TestHttpFile : HttpPostedFileBase
	{
		public TestHttpFile(string fileName, string fileFormat, Stream fileStream)
		{
			TestFileFormat = fileFormat;
			TestFileName = fileName;
			TestFileStream = fileStream;
		}

		public string TestFileName;
		public Stream TestFileStream;
		public string TestFileFormat;

		public override string ContentType
		{
			get { return TestFileFormat; }
		}

		public override string FileName
		{
			get { return TestFileName; }
		}

		public override Stream InputStream
		{
			get { return TestFileStream; }
		}

		public override int ContentLength
		{
			get { return (int)TestFileStream.Length; }
		}
	}

	[TestFixture]
	public class ReportTuningControllerFixture : ControllerFixture
	{
		[Test]
		public void Load_file_test()
		{
			referer = "http://localhost";
			var controller = new ReportsTuningController();
			Prepare(controller);

			var reportType = 0u;
			foreach (var fileType in session.QueryOver<FileForReportType>().List()) {
				session.Delete(fileType);
			}
			reportType = session.Query<ReportType>().First().Id;
			File.WriteAllText("test.txt", "1234567890");
			var stream = File.OpenRead("test.txt");
			var file = new TestHttpFile("testFileName.txt", "application/octet-stream", stream);
			Request.Files.Add(reportType, file);
			ConfigReader.LoadSettings(Global.Config);
			controller.SaveFilesForReportType();
			session.Flush();

			var fileName = session.QueryOver<FileForReportType>().Where(f => f.File == "testFileName.txt").List().First().Id.ToString();
			Assert.IsTrue(File.Exists(Path.Combine(Global.Config.SavedFilesReportTypePath, fileName)));
		}
	}
}
