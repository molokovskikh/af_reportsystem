using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord("PayerOwnerContacts", Schema = "contacts")]
	public class PayerOwnerContact
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual Contact Contact { get; set; }

		[BelongsTo]
		public virtual Payer Payer { get; set; }
	}
}