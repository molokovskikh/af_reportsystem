using System.Collections.Generic;
using Castle.ActiveRecord;
using System.Linq;

namespace ReportTuner.Models
{
	[ActiveRecord("Payers", Schema = "Billing")]
	public class Payer : ActiveRecordBase<Payer>
	{
		[PrimaryKey("PayerID")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ShortName { get; set; }
		
		[HasAndBelongsToMany(typeof(Client), Schema = "Billing", Table = "PayerClients", 
													ColumnKey = "PayerID", ColumnRef = "ClientID")]
		public virtual IList<Client> FutureClients { get; set; }


		public List<Client> AllClients
		{
			get 
			{
				return FutureClients.OrderBy(rec => rec.ShortName).ToList();
			}
		}
	}
}
