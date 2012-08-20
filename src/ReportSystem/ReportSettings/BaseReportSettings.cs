using Inforoom.ReportSystem.Writers;

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

		public string ListName
		{
			get
			{
				var length = (ReportCaption.Length < BaseExcelWriter.MaxListName) ? ReportCaption.Length : BaseExcelWriter.MaxListName;
				return ReportCaption.Substring(0, length);
			}
		}
	}
}