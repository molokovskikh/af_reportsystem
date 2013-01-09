using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Models;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;

namespace ReportTuner.Models
{
	[ActiveRecord("general_reports", Schema = "reports")]
	public class GeneralReport : ActiveRecordBase<GeneralReport>
	{
		public GeneralReport(Payer payer)
			: this()
		{
			Payer = payer;
		}

		public GeneralReport()
		{
			Reports = new List<Report>();
			Format = "Excel";
		}

		[PrimaryKey("GeneralReportCode")]
		public virtual ulong Id { get; set; }

		[BelongsTo("PayerID")]
		public virtual Payer Payer { get; set; }


		/// <summary>
		/// идентификатор поставщика в папку которого на нашем ftp кладется отчет
		/// </summary>
		[Property]
		public virtual uint? FirmCode { get; set; }

		[Property]
		public virtual bool Allow { get; set; }

		[Property]
		public virtual string EMailSubject { get; set; }

		[BelongsTo(Column = "ContactGroupId", Cascade = CascadeEnum.Delete)]
		public virtual ContactGroup ContactGroup { get; set; }

		[Property]
		public virtual bool Temporary { get; set; }

		[Property]
		public virtual DateTime? TemporaryCreationDate { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property]
		public virtual string ReportFileName { get; set; }

		[Property]
		public virtual string ReportArchName { get; set; }

		[Property]
		public virtual bool NoArchive { get; set; }

		[Property]
		public virtual bool SendDescriptionFile { get; set; }

		[Property]
		public virtual string Format { get; set; }

		[Property]
		public virtual DateTime? LastSuccess { get; set; }

		[HasMany]
		public virtual IList<Report> Reports { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<FileSendWithReport> Files { get; set; }

		public virtual bool IsSuccessfulyProcessed
		{
			get
			{
				return LastSuccess != null
					&& LastSuccess + TimeSpan.FromDays(Global.Config.ReportHistoryStorageInterval) > DateTime.Now;
			}
		}

		public string ActualReportName
		{
			get
			{
				if (!String.IsNullOrEmpty(ReportFileName))
					return Path.ChangeExtension(ReportFileName, ".xls");
				return String.Format("Rep{0}.xls", Id);
			}
		}

		public string ActualArchiveName
		{
			get
			{
				if (!String.IsNullOrEmpty(ReportArchName))
					return ReportArchName;
				return Path.ChangeExtension(ActualReportName, ".zip");
			}
		}

		public bool IsOrderReport()
		{
			return Reports.Any(r => r.ReportType.IsOrderReport);
		}

		public void RemoveTask()
		{
			using (var helper = new ScheduleHelper()) {
				helper.DeleteReportTask(Id);
			}
		}

		public Report AddReport(ReportType type)
		{
			var report = new Report(this, type);
			Reports.Add(report);
			return report;
		}

		public string Filename(string name)
		{
			if (Path.GetExtension(name).Match(".zip")) {
				return ActualArchiveName;
			}
			return ActualReportName;
		}
	}
}