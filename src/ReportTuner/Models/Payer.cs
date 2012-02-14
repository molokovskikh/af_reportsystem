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
		
		[HasAndBelongsToMany(typeof(FutureClient), Schema = "Billing", Table = "PayerClients", 
													ColumnKey = "PayerID", ColumnRef = "ClientID")]
		public virtual IList<FutureClient> FutureClients { get; set; }


		public List<FutureClient> AllClients
		{
			get 
			{
				return FutureClients.OrderBy(rec => rec.ShortName).ToList();
			}
		}
	}
}
