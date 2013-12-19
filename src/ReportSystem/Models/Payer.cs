using Castle.ActiveRecord;

namespace Inforoom.ReportSystem.Model
{
	[ActiveRecord("Payers", Schema = "Billing")]
	public class Payer
	{
		[PrimaryKey("PayerId")]
		public virtual uint Id { get; set; }

		[Property("ShortName")]
		public virtual string Name { get; set; }
	}
}