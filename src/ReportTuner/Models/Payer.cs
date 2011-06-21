using System.Collections.Generic;
using Castle.ActiveRecord;
using System.Linq;


namespace ReportTuner.Models
{
	//[ActiveRecord("billing.payers")]
	[ActiveRecord("Payers", Schema = "Billing")]
	public class Payer : ActiveRecordBase<Payer>
	{
		[PrimaryKey("PayerID")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ShortName { get; set; }

		/*[HasMany(typeof(Client), Lazy = true, Inverse = true, OrderBy = "ShortName")]
		public virtual IList<Client> Clients { get; set; }*/
		
		[HasAndBelongsToMany(typeof(FutureClient), Schema = "Billing", Table = "PayerClients", 
													ColumnKey = "PayerID", ColumnRef = "ClientID")]
		public virtual IList<FutureClient> FutureClients { get; set; }


		public List<IClient> AllClients
		{			
			get 
			{ // Объединяем старых и новых клиентов 
				//return Clients.Cast<IClient>().Concat(FutureClients.Cast<IClient>()).OrderBy(rec => rec.ShortName).ToList();
                return FutureClients.Cast<IClient>().OrderBy(rec => rec.ShortName).ToList();
			}
		}
	}
}
