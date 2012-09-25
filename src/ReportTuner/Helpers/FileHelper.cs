using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using Common.Tools;
using ReportTuner.Models;
using log4net;

namespace ReportTuner.Helpers
{
	public class FileHelper
	{
		private static ILog _log = LogManager.GetLogger(typeof(FileHelper));

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
			var mask = GetFileMaskForGeneralReport(report);
			var files = Directory.GetFiles(ftpDirectory).Where(f => f.Contains(mask));
			_log.InfoFormat("При подготовке отчета {0} по маске {1} были найдены файлы {2}", report.Id, mask, files.Implode());
			return files;
		}
	}
}