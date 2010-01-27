
namespace Inforoom.ReportSystem.ReportSettings
{
	public class BaseReportSettings
	{
		public BaseReportSettings(ulong reportCode, string reportCaption)
		{
			ReportCode = reportCode;
			ReportCaption = reportCaption;
		}

		public ulong ReportCode;
		public string ReportCaption;
	}
}
