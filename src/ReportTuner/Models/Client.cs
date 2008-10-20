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
	}
}
