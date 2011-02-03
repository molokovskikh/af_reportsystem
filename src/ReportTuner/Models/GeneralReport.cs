using System;
using System.Collections.Generic;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord("reports.general_reports")]
	public class GeneralReport : ActiveRecordBase<GeneralReport>
	{
		[PrimaryKey("GeneralReportCode")]
		public virtual ulong Id { get; set; }

		[BelongsTo("PayerID")]
		public virtual Payer Payer { get; set; }

		[Property]
		public virtual uint FirmCode { get; set; }

		/*public IClient Client
		{
			get
			{
				var futureClient = FutureClient.TryFind(FirmCode);
				if (futureClient != null)
					return futureClient;

				var oldClient = ReportTuner.Models.Client.TryFind(FirmCode);
				return oldClient;
			}
		}*/
		[Property]
		public virtual bool Allow { get; set; }

		[Property]
		public virtual string EMailSubject { get; set; }

		[BelongsTo(Column = "ContactGroupId")]
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
		public virtual string Format { get; set; }
	}
}
