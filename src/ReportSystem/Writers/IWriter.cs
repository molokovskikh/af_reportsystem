using System.Data;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem.Writers
{
	public interface IWriter
	{
		void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings);
	}
}