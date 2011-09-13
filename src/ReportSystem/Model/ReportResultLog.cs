using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.ReportSystem.Model
{
	[ActiveRecord("ReportsResLogs", Schema = "Logs", Mutable = false)]
	public class ReportResultLog : ActiveRecordLinqBase<ReportResultLog>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public ulong GeneralReportCode { get; set; }

		[Property]
		public ulong ReportCode { get; set; }

		[Property]
		public DateTime StartTime { get; set; }

		[Property]
		public DateTime StopTime { get; set; }

		[Property]
		public string ErrorMessage { get; set; }

		public static ReportResultLog Log(ulong generalReportCode, ulong reportCode, DateTime startTime, DateTime stopTime, string errorMessage)
		{
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				var log = new ReportResultLog {
					GeneralReportCode = generalReportCode,
					ReportCode = reportCode,
					StartTime = startTime,
					StopTime = stopTime,
					ErrorMessage = errorMessage
				};
				log.Create();
				scope.VoteCommit();
				return log;
			}
		}
	}
}
