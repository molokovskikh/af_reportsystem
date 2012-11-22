using System;

namespace Inforoom.ReportSystem.ReportSettings
{
	public class OptimizationEfficiencySettings : BaseReportSettings
	{
		public OptimizationEfficiencySettings(ulong reportCode, string reportCaption, DateTime beginDate, DateTime endDate,
			int clientId, int optimizedCount, string concurents, string supplierName)
			: base(reportCode, reportCaption)
		{
			BeginDate = beginDate;
			EndDate = endDate;
			ClientId = clientId;
			OptimizedCount = optimizedCount;
			Concurents = concurents;
			SupplierName = supplierName;
		}

		public DateTime BeginDate;
		public DateTime EndDate;
		public int ClientId;
		public int OptimizedCount;
		public string Concurents;
		public string SupplierName;
	}
}