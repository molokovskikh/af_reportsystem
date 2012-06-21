using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord("Addresses", Schema = "Customers", Lazy = true), Auditable]
	public class Address : ActiveRecordLinqBase<Address>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("Address"), Description("Адрес"), Auditable, ValidateNonEmpty]
		public virtual string Value { get; set; }

		[BelongsTo("ClientId"), Description("Клиент"), Auditable]
		public virtual Client Client { get; set; }

		[BelongsTo("ContactGroupId", Lazy = FetchWhen.OnInvoke, Cascade = CascadeEnum.All)]
		public virtual ContactGroup ContactGroup { get; set; }

		[Property]
		public virtual bool Enabled { get; set; }

		[BelongsTo("PayerId"), Description("Плательщик"), Auditable]
		public virtual Payer Payer { get; set; }
	}
}