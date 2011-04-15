using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.ReportSystem.Model
{
	[ActiveRecord("Regions", Schema = "Farm", Mutable = false)]
	public class Region : ActiveRecordLinqBase<Region>
	{
		[PrimaryKey("RegionCode")]
		public ulong Id { get; set; }

		[Property("Region")]
		public string Name { get; set; }
	}
}