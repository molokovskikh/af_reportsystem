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
			return Math.Round(producerCost * (1  + supplierMarkup / 100 + drugstoreMarkup / 100 * (1 + nds / 100)), 2);
		}

		public static decimal CalculateRetailCost(decimal supplierCostWithoutNds, decimal producerCost, decimal nds, decimal markup)
		{
			return Math.Round(supplierCostWithoutNds  + producerCost * markup / 100 * (1 + nds / 100), 2);
		}

		public static decimal RetailCost(decimal supplierCostWithoutNds, decimal producerCost, decimal nds, IEnumerable<Markup> markups)
		{
			var drugstoeMarkup = markups.FirstOrDefault(m => m.Type == MarkupType.Drugstore);
			if (drugstoeMarkup == null)
				return 0;
			if (markups.All(m => m.Type != MarkupType.Supplier))
				return 0;

			var markup = drugstoeMarkup.Value - 5;
			var retailCost = CalculateRetailCost(supplierCostWithoutNds, producerCost, nds, markup);

			var maxCost = MaxCost(producerCost, nds, markups);
			if (retailCost > maxCost) {
				markup = (maxCost - supplierCostWithoutNds) / (producerCost * (1 + nds / 100)) * 100;
				if (markup < 5)
					return 0;
				retailCost = CalculateRetailCost(supplierCostWithoutNds, producerCost, nds, markup);
			}
			return retailCost;
		}
	}
}