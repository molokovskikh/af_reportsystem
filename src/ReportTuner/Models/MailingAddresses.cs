using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord("Mailing_Addresses", Schema = "reports")]
	public class MailingAddresses : ActiveRecordBase<MailingAddresses>
	{
		[PrimaryKey]
		public virtual ulong Id { get; set; }

		[Property]
		public virtual string Mail { get; set; }

		[BelongsTo("GeneralReport")]
		public virtual GeneralReport GeneralReport { get; set; }
	}
}