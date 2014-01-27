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
			var supplierCost = producerCost + producerCost * supplierMarkup / 100 * (1 + nds / 100);
			return CalculateRetailCost(supplierCost, producerCost, nds, drugstoreMarkup);
		}

		public static decimal CalculateRetailCost(decimal supplierCost, decimal producerCost, decimal nds, decimal markup)
		{
			var result = supplierCost  + producerCost * markup / 100 * (1 + nds / 100);
			return Math.Floor(result * 10) / 10;
		}

		public static decimal RetailCost(decimal supplierCost, decimal producerCost, decimal nds, IEnumerable<Markup> markups)
		{
			if (producerCost == 0)
				return 0;
			var drugstoeMarkup = markups.FirstOrDefault(m => m.Type == MarkupType.Drugstore);
			if (drugstoeMarkup == null)
				return 0;
			if (markups.All(m => m.Type != MarkupType.Supplier))
				return 0;

			var markup = drugstoeMarkup.Value - 2;
			var retailCost = CalculateRetailCost(supplierCost, producerCost, nds, markup);

			var maxCost = MaxCost(producerCost, nds, markups);
			if (retailCost > maxCost) {
				markup = (maxCost - supplierCost) / (producerCost * (1 + nds / 100)) * 100;
				if (markup < 5)
					return 0;
				retailCost = CalculateRetailCost(supplierCost, producerCost, nds, markup);
			}
			return retailCost;
		}
	}
}