using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem.Writers
{
	public interface IWriter
	{
		List<string> Warnings { get; }
		void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings);
	}
}