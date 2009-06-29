using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;


namespace ReportTuner.Models
{
	[ActiveRecord("billing.payers")]
	public class Payer : ActiveRecordBase<Payer>
	{
		[PrimaryKey("PayerID")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ShortName { get; set; }

		[HasMany(typeof(Client), Lazy = true, Inverse = true, OrderBy = "ShortName")]
		public virtual IList<Client> Clients { get; set; }
	}
}
