using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;


namespace ReportTuner.Models
{
	public interface IClient
	{
		uint Id { get; set; }
		string ShortName { get; set; }
		string ShortNameAndId{ get; }
		string FullName { get; set; }
		int FirmType { get; }
	}

	[ActiveRecord("usersettings.clientsdata")]
	public class Client : ActiveRecordBase<Client>, IClient
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

		[Property]
		public virtual int FirmStatus { get; set; }
	}

	[ActiveRecord("future.Clients")]
	public class FutureClient : ActiveRecordBase<FutureClient>, IClient
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("Name")]
		public virtual string ShortName { get; set; }

		[Property]
		public virtual string FullName { get; set; }

		[BelongsTo("ContactGroupOwnerId")]
		public virtual ContactGroupOwner ContactGroupOwner { get; set; }

		[BelongsTo("PayerId")]
		public virtual Payer BillingInstance { get; set; }

		public string ShortNameAndId
		{
			get { return String.Format("{0} ({1})", ShortName, Id); }
		}

		public int FirmType
		{
			get { return 1; }
		}
	}
}
