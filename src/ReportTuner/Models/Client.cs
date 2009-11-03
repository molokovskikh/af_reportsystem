using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;


namespace ReportTuner.Models
{
	[ActiveRecord("usersettings.clientsdata")]
	public class Client : ActiveRecordBase<Client>
	{
		[PrimaryKey("FirmCode")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ShortName { get; set; }

		[Property]
		public virtual string FullName { get; set; }

		[BelongsTo("ContactGroupOwnerId")]
		public virtual ContactGroupOwner ContactGroupOwner { get; set; }

		[BelongsTo("BillingCode")]
		public virtual Payer BillingInstance { get; set; }

		public string ShortNameAndId
		{
			get { return String.Format("{0} ({1})", ShortName, Id); }
		}

		[Property]
		public virtual ulong RegionCode { get; set; }

		[Property]
		public virtual ulong MaskRegion { get; set; }

		[Property]
		public virtual int FirmType { get; set; }
	}
}
