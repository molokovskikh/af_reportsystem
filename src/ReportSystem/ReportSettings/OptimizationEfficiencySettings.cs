using System;

namespace Inforoom.ReportSystem.ReportSettings
{
	public class OptimizationEfficiencySettings : BaseReportSettings
	{
		public OptimizationEfficiencySettings(ulong reportCode, string reportCaption, DateTime beginDate, DateTime endDate, 
			int clientId, int optimizedCount)
			: base(reportCode, reportCaption)
		{
			BeginDate = beginDate;
			EndDate = endDate;
			ClientId = clientId;
			OptimizedCount = optimizedCount;
		}

		public DateTime BeginDate;
		public DateTime EndDate;
		public int ClientId;
		public int OptimizedCount;
	}
}
