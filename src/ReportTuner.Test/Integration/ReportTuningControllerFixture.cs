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
using Common.Web.Ui.Helpers;
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

	public class ControllerFixture : BaseControllerTest
	{
		protected string referer;

		protected override IMockResponse BuildResponse(UrlInfo info)
		{
			return new StubResponse(info,
				new DefaultUrlBuilder(),
				new StubServerUtility(),
				new RouteMatch(),
				referer);
		}
	}

	[TestFixture]
	public class ReportTuningControllerFixture : ControllerFixture
	{
		[Test]
		public void Loaa_file_test()
		{
			referer = "http://ya.ru";
			var reportType = 0u;
			using (var scope = new SessionScope()) {
				ArHelper.WithSession(s => {
					foreach (var fileType in s.QueryOver<FileForReportType>().List()) {
						s.Delete(fileType);
					}
					reportType = s.QueryOver<ReportType>().List().First().Id;
					var controller = new ReportsTuningController();
					File.WriteAllText("test.txt", "1234567890");
					var stream = File.OpenRead("test.txt");
					var file = new TestHttpFile("testFileName.txt", "application/octet-stream", stream);
					PrepareController(controller);
					Request.Files.Add(reportType, file);
					ConfigReader.LoadSettings(Global.Config);
					controller.DbSession = s;
					controller.SaveFilesForReportType();
					scope.Flush();
					var fileName = s.QueryOver<FileForReportType>().Where(f => f.File == "testFileName.txt").List().First().Id.ToString();
					Assert.IsTrue(File.Exists(Path.Combine(Global.Config.SavedFileForReportTypesPath, fileName)));
				});
			}
		}
	}
}
