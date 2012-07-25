using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;

namespace ReportTuner.Models
{
	[ActiveRecord("general_reports", Schema = "reports")]
	public class GeneralReport : ActiveRecordBase<GeneralReport>
	{
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
		public virtual string Format { get; set; }

		[HasMany]
		public virtual IList<Report> Reports {get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<FileSendWithReport> Files { get; set; }

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
	}
}
