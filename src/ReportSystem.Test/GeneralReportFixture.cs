using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExecuteTemplate;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	public class FakeReport : BaseReport
	{
		public FakeReport()
		{
			_dsReport = new DataSet();
			AdditionalFiles.Add("description.xls", "description.xls");
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

	[TestFixture]
	public class GeneralReportFixture
	{
		[Test]
		public void Archive_additional_files()
		{
			File.WriteAllBytes("description.xls", new byte[0]);

			var report = new GeneralReport();
			report.GeneralReportID = 1;
			report.Reports.Add(new FakeReport());
			var result = report.BuildResultFile();
			var zip = new ZipFile(result);
			var files = zip.Cast<ZipEntry>().Select(e => e.Name).ToArray();
			Assert.That(files.Count(), Is.EqualTo(2));
			Assert.That(files[1], Is.EqualTo("Rep1.xls"));
			Assert.That(files[0], Is.EqualTo("description.xls"));
		}
	}
}