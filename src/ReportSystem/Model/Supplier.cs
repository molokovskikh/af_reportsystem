using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.ReportSystem.Model
{
	//[ActiveRecord("ClientsData", Schema = "Usersettings", Mutable = false)]
    [ActiveRecord("Suppliers", Schema = "Future", Mutable = false)]
	public class Supplier : ActiveRecordLinqBase<Supplier>
	{
		//[PrimaryKey("FirmCode")]
        [PrimaryKey]
		public uint Id { get; set; }

		//[Property("ShortName")]
        [Property]
		public string Name { get; set; }
	}
}