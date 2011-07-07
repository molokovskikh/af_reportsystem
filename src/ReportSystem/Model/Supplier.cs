using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.ReportSystem.Model
{	
    [ActiveRecord("Suppliers", Schema = "Future", Mutable = false)]
	public class Supplier : ActiveRecordLinqBase<Supplier>
	{
        [PrimaryKey]
		public uint Id { get; set; }
		
        [Property]
		public string Name { get; set; }
	}
}