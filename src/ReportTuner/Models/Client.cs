using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Models;


namespace ReportTuner.Models
{

	[ActiveRecord("Clients", Schema = "Future")]
	public class FutureClient : ActiveRecordBase<FutureClient>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("Name")]
		public virtual string ShortName { get; set; }

		[Property]
		public virtual string FullName { get; set; }

		[Property]
		public virtual long MaskRegion { get; set; }

		[BelongsTo("ContactGroupOwnerId")]
		public virtual ContactGroupOwner ContactGroupOwner { get; set; }

		[HasAndBelongsToMany (typeof(Payer),  Schema = "Billing", Table = "PayerClients", ColumnKey = "ClientID", ColumnRef = "PayerID")]
		public virtual IList<Payer> Payers { get; set; }

		[HasMany(ColumnKey = "ClientId", Inverse = true, Lazy = true, Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<FutureUser> Users { get; set; }

		public string ShortNameAndId
		{
			get { return String.Format("{0} ({1})", ShortName, Id); }
		}

		public int FirmType
		{
			get { return 1; }
		}
	}

	public interface IUser
	{
		uint Id { get; set; }
		string ShortNameAndId { get; }
	}

	[ActiveRecord("Users", Schema = "Future")]	
	public class FutureUser : ActiveRecordLinqBase<FutureUser>, IUser
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Enabled { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo("ClientId", NotNull = true, Lazy = FetchWhen.OnInvoke)]
		public virtual FutureClient Client { get; set; }

		public string ShortNameAndId
		{
			get { return String.Format("{0}-{1}", Id, Name); }
		}
	}
}
