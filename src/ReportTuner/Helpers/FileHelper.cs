using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using ReportTuner.Models;

namespace ReportTuner.Helpers
{
	public class FileHelper
	{
		public static string GetFileMaskForGeneralReport(GeneralReport report)
		{
			if (!report.NoArchive) {
				if (!string.IsNullOrEmpty(report.ReportArchName))
					return report.ReportArchName;
			}
			else
				if (!string.IsNullOrEmpty(report.ReportFileName))
				return report.ReportFileName;
			return report.Id.ToString(CultureInfo.InvariantCulture);
		}

		public static IEnumerable<string> GetFilesForSend(string ftpDirectory, GeneralReport report)
		{
			return Directory.GetFiles(ftpDirectory).Where(f => f.Contains(GetFileMaskForGeneralReport(report)));
		}
	}
}