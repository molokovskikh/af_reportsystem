using System.Collections.Generic;
using Castle.ActiveRecord;
using System.Linq;

namespace ReportTuner.Models
{
	[ActiveRecord("Payers", Schema = "Billing")]
	public class Payer : ActiveRecordBase<Payer>
	{
		public Payer()
		{
		}

		public Payer(string shortName)
		{
			ShortName = shortName;
		}

		[PrimaryKey("PayerID")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ShortName { get; set; }

		[HasAndBelongsToMany(typeof(Client), Schema = "Billing", Table = "PayerClients",
			ColumnKey = "PayerID", ColumnRef = "ClientID")]
		public virtual IList<Client> Clients { get; set; }

		public List<Client> AllClients
		{
			get { return Clients.OrderBy(rec => rec.ShortName).ToList(); }
		}
	}
}