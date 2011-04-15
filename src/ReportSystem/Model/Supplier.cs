using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.ReportSystem.Model
{
	[ActiveRecord("ClientsData", Schema = "Usersettings", Mutable = false)]
	public class Supplier : ActiveRecordLinqBase<Supplier>
	{
		[PrimaryKey("FirmCode")]
		public uint Id { get; set; }

		[Property("ShortName")]
		public string Name { get; set; }
	}
}