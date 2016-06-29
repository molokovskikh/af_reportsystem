using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord("Clients", Schema = "Customers")]
	public class Client : ActiveRecordBase<Client>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("Name")]
		public virtual string ShortName { get; set; }

		[Property]
		public virtual string FullName { get; set; }

		[Property]
		public virtual long MaskRegion { get; set; }

		[BelongsTo("RegionCode")]
		public Region HomeRegion { get; set; }

		[BelongsTo("ContactGroupOwnerId")]
		public virtual ContactGroupOwner ContactGroupOwner { get; set; }

		[HasAndBelongsToMany(typeof(Payer), Schema = "Billing", Table = "PayerClients", ColumnKey = "ClientID", ColumnRef = "PayerID")]
		public virtual IList<Payer> Payers { get; set; }

		[HasMany(ColumnKey = "ClientId", Inverse = true, Lazy = true, Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<User> Users { get; set; }

		[HasMany(ColumnKey = "ClientId", Lazy = true, Inverse = true, OrderBy = "Address", Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<Address> Addresses { get; set; }

		public string ShortNameAndId
		{
			get { return String.Format("{0} ({1})", ShortName, Id); }
		}

		public int FirmType
		{
			get { return 1; }
		}
	}

	[ActiveRecord("Users", Schema = "Customers")]
	public class User : ActiveRecordLinqBase<User>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Enabled { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo("ClientId", NotNull = true, Lazy = FetchWhen.OnInvoke)]
		public virtual Client Client { get; set; }

		public string ShortNameAndId
		{
			get { return String.Format("{0}-{1}", Id, Name); }
		}
	}
}