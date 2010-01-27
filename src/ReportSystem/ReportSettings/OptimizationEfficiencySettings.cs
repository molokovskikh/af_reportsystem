using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inforoom.ReportSystem.ReportSettings
{
	public class OptimizationEfficiencySettings : BaseReportSettings
	{
		public OptimizationEfficiencySettings(ulong reportCode, string reportCaption, DateTime beginDate, DateTime endDate, int clientId)
			: base(reportCode, reportCaption)
		{
			BeginDate = beginDate;
			EndDate = endDate;
			ClientId = clientId;
		}

		public DateTime BeginDate;
		public DateTime EndDate;
		public int ClientId;
	}
}
