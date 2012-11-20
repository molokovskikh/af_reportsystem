using System;

namespace Inforoom.ReportSystem.ReportSettings
{
	public class OptimizationEfficiencySettings : BaseReportSettings
	{
		public OptimizationEfficiencySettings(ulong reportCode, string reportCaption, DateTime beginDate, DateTime endDate,
			int clientId, int optimizedCount, string concurents)
			: base(reportCode, reportCaption)
		{
			BeginDate = beginDate;
			EndDate = endDate;
			ClientId = clientId;
			OptimizedCount = optimizedCount;
			Concurents = concurents;
		}

		public DateTime BeginDate;
		public DateTime EndDate;
		public int ClientId;
		public int OptimizedCount;
		public string Concurents;
	}
}