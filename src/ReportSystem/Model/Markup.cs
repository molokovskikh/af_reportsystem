using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;

namespace Inforoom.ReportSystem.Model
{
	public enum MarkupType
	{
		Supplier,
		Drugstore,
	}

	[ActiveRecord(Schema = "Reports")]
	public class Markup
	{
		public Markup()
		{
		}

		public Markup(MarkupType type, decimal value)
		{
			Type = type;
			Value = value;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual MarkupType Type { get; set; }

		[Property]
		public virtual decimal Begin { get; set; }

		[Property]
		public virtual decimal End { get; set; }

		[BelongsTo("RegionId")]
		public virtual Region Region { get; set; }

		[Property]
		public virtual decimal Value { get; set; }

		public static decimal MaxCost(decimal producerCost, decimal nds, IEnumerable<Markup> markups)
		{
			var supplierMarkup = markups.First(m => m.Type == MarkupType.Supplier).Value;
			var drugstoreMarkup = markups.First(m => m.Type == MarkupType.Drugstore).Value;
			return Math.Round(producerCost + producerCost * supplierMarkup / 100 + producerCost * drugstoreMarkup / 100 * (1 + nds / 100), 2);
		}
	}
}