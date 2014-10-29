using Castle.ActiveRecord;
using Common.Web.Ui.Models;

namespace ReportTuner.Models
{
	[ActiveRecord(Schema = "Contacts")]
	public class ReportSubscriptionContact
	{
		public ReportSubscriptionContact()
		{
		}

		public ReportSubscriptionContact(Contact payerContact, Contact reportContact)
		{
			PayerContact = payerContact;
			ReportContact = reportContact;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual Contact PayerContact { get; set; }

		[BelongsTo]
		public virtual Contact ReportContact { get; set; }
	}
}