using System.Collections.Generic;
using Castle.ActiveRecord;
using System.Linq;


namespace ReportTuner.Models
{
	[ActiveRecord("billing.payers")]
	public class Payer : ActiveRecordBase<Payer>
	{
		[PrimaryKey("PayerID")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ShortName { get; set; }

		[HasMany(typeof(Client), Lazy = true, Inverse = true, OrderBy = "ShortName")]
		public virtual IList<Client> Clients { get; set; }

		[HasMany(typeof(FutureClient), Lazy = true, Inverse = true, OrderBy = "Name")]
		public virtual IList<FutureClient> FutureClients { get; set; }

		public List<IClient> AllClients
		{
			get
			{ // Объединяем старых и новых клиентов
				return Clients.Cast<IClient>().Concat(FutureClients.Cast<IClient>()).OrderBy(rec => rec.ShortName).ToList();
			}
		}
	}
}
