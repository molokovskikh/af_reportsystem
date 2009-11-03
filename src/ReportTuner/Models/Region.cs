using System;
using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord("farm.regions")]
	public class Region : ActiveRecordBase<Region>
	{
		[PrimaryKey("RegionCode")]
		public virtual ulong RegionCode {get; set;}

		[Property("Region")]
		public virtual string Name {get; set;}
	
		[Property]
		public virtual string LongAliase {get; set;}
	
		[Property]
		public virtual string ShortAliase {get; set;}
	
		[Property]
		public virtual ulong DefaultRegionMask {get; set;}
	
		[Property]
		public virtual ulong DefaultShowRegionMask {get; set;}
	
		[Property]
		public virtual string Comment {get; set;}
	
		[Property]
		public virtual DateTime AccessTime {get; set;}
	
		[Property]
		public virtual int MoscowBias {get; set;}
	}
}
