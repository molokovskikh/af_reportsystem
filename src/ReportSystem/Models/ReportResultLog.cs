using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.ReportSystem.Model
{
	[ActiveRecord("ReportsResLogs", Schema = "Logs", Mutable = false)]
	public class ReportResultLog
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
			using (var session = GeneralReport.Factory.OpenSession())
			using (var trx = session.BeginTransaction()) {
				var log = new ReportResultLog {
					GeneralReportCode = generalReportCode,
					ReportCode = reportCode,
					StartTime = startTime,
					StopTime = stopTime,
					ErrorMessage = errorMessage
				};
				session.Save(log);
				trx.Commit();
				return log;
			}
		}
	}
}