using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ReportTuner.Helpers;
using ReportTuner.Models;

namespace ReportTuner.Test.Unit
{
	[TestFixture]
	public class GeneralReportFixture
	{
		[Test]
		public void File_mask_created_report_test()
		{
			var generalReport = new GeneralReport();
			generalReport.Id = 111;
			var mask = FileHelper.GetFileMaskForGeneralReport(generalReport);
			Assert.AreEqual(mask, "111");
			generalReport.NoArchive = true;
			generalReport.ReportFileName = "testReportFile";
			mask = FileHelper.GetFileMaskForGeneralReport(generalReport);
			Assert.AreEqual(mask, "testReportFile");
			generalReport.NoArchive = false;
			mask = FileHelper.GetFileMaskForGeneralReport(generalReport);
			Assert.AreEqual(mask, "111");
			generalReport.ReportArchName = "testArchReportFile";
			mask = FileHelper.GetFileMaskForGeneralReport(generalReport);
			Assert.AreEqual(mask, "testArchReportFile");
		}

		[Test]
		public void Files_for_reports_created()
		{
			var filesDir = "TestFilesCreated";
			if (!Directory.Exists(filesDir))
				Directory.CreateDirectory(filesDir);
			File.WriteAllText(Path.Combine(filesDir, "123.rar"), string.Empty);
			File.WriteAllText(Path.Combine(filesDir, "111.rar"), string.Empty);
			var generalReport = new GeneralReport();
			generalReport.Id = 111;
			var files = FileHelper.GetFilesForSend(filesDir, generalReport);
			Assert.AreEqual(files.Count(), 1);
			Assert.AreEqual(files.First(), Path.Combine(filesDir, "111.rar"));
			generalReport.ReportArchName = "123";
			files = FileHelper.GetFilesForSend(filesDir, generalReport);
			Assert.AreEqual(files.Count(), 1);
			Assert.AreEqual(files.First(), Path.Combine(filesDir, "123.rar"));
			generalReport.NoArchive = true;
			generalReport.ReportFileName = "999.xls";
			files = FileHelper.GetFilesForSend(filesDir, generalReport);
			Assert.AreEqual(files.Count(), 0);
		}
	}
}
