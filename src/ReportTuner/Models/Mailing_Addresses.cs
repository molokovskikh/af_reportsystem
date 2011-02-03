using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord("reports.Mailing_Addresses")]
	public class MailingAddresses : ActiveRecordBase<MailingAddresses>
	{
		[PrimaryKey]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual string Mail { get; set; }

		[BelongsTo("GeneralReport")]
		public virtual GeneralReport GeneralReport { get; set; }

	}
}