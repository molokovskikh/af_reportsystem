﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using Castle.ActiveRecord;
using Common.Schedule;
using Common.Tools;
using Common.Web.Ui.Models;
using Microsoft.Win32.TaskScheduler;
using NHibernate;
using NHibernate.Linq;
using ReportTuner.Helpers;

namespace ReportTuner.Models
{
	[ActiveRecord("general_reports", Schema = "reports")]
	public class GeneralReport : ActiveRecordBase<GeneralReport>
	{
		public bool UnderTest;
		public List<MailMessage> Messages = new List<MailMessage>();

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

		[BelongsTo("PublicSubscriptionsId")]
		public virtual ContactGroup PublicSubscriptions { get; set; }

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

		[Property]
		public virtual bool MailPerFile { get; set; }

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

		public string ResendReport(ISession session, List<string> mails)
		{
			var log = session.Query<ReportLog>()
				.Where(l => l.Result != null && l.Report == this)
				.OrderByDescending(l => l.LogTime)
				.Take(1)
				.FirstOrDefault();
			var files = new string[0];
			if (log != null) {
				files = Directory.GetFiles(Global.Config.ReportHistoryPath, log.Result.Id + ".*");
			}
			if (files.Length == 0) {
				return "Файл отчета не найден";
			}

			var message = new MailMessage();
			message.From = new MailAddress("report@analit.net", "АналитФармация");
			foreach (var mail in mails) {
				message.To.Add(mail);
			}
			message.Subject = EMailSubject;
			message.Attachments.Add(new Attachment(files[0]) {
				Name = Filename(files[0])
			});
			var client = new SmtpClient();
			message.BodyEncoding = System.Text.Encoding.UTF8;
			if (UnderTest) {
				Messages.Add(message);
			}
			else {
				client.Send(message);
			}
			return null;
		}
	}
}