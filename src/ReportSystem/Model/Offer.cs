using System;
using System.Data;
using System.Threading;

namespace Inforoom.ReportSystem.Model
{
	public class Offer
	{
		public uint CatalogId { get; set; }
		public uint ProductId { get; set; }
		public uint ProducerId { get; set; }
		public string ProductName { get; set; }
		public string ProducerName { get; set; }

		public ulong CoreId { get; set; }
		public string Code { get; set; }
		public string CodeCr { get; set; }
		public uint SupplierId { get; set; }
		public uint PriceId { get; set; }
		public ulong RegionId { get; set; }
		public string Quantity { get; set; }
		public float RealCost { get; set; }
		public float Cost { get; set; }

		public ulong? AssortmentCoreId { get; set; }
		public string AssortmentCode { get; set; }
		public string AssortmentCodeCr { get; set; }
		public uint? AssortmentSupplierId { get; set; }
		public uint? AssortmentPriceId { get; set; }
		public ulong? AssortmentRegionId { get; set; }
		public string AssortmentQuantity { get; set; }
		public float? AssortmentRealCost { get; set; }
		public float? AssortmentCost { get; set; }

		public string CodeWithoutProducer { get; set; }

		public Offer(){}

		public Offer(IDataRecord row, uint? noiseSupplierId, Random random)
		{
			if (row == null)
				throw new ArgumentNullException("row");

			if (noiseSupplierId.HasValue && random == null)
				throw new ArgumentNullException("random", "При установленном параметре noiseSupplierId не установлен параметр random: генератор случайных чисел");

			CatalogId = Convert.ToUInt32(row["CatalogId"]);
			ProductId = Convert.ToUInt32(row["ProductId"]);
			ProducerId = Convert.ToUInt32(row["ProducerId"]);
			ProductName = Convert.ToString(row["ProductName"]);
			ProducerName = Convert.ToString(row["ProducerName"]);


			CoreId = Convert.ToUInt64(row["CoreId"]);
			Code = Convert.ToString(row["Code"]);
			SupplierId = Convert.ToUInt32(row["SupplierId"]);
			PriceId = Convert.ToUInt32(row["PriceId"]);
			RegionId = Convert.ToUInt64(row["RegionId"]);
			Quantity = Convert.ToString(row["Quantity"]);
			RealCost = Convert.ToSingle(row["Cost"]);
			Cost = NoiseCost(noiseSupplierId, SupplierId, random, RealCost);

			if (!Convert.IsDBNull(row["AssortmentCoreId"]))
			{
				AssortmentCoreId = Convert.ToUInt64(row["AssortmentCoreId"]);
				AssortmentCode = Convert.ToString(row["AssortmentCode"]);
				AssortmentCodeCr = Convert.ToString(row["AssortmentCodeCr"]);
				AssortmentSupplierId = Convert.ToUInt32(row["AssortmentSupplierId"]);
				AssortmentPriceId = Convert.ToUInt32(row["AssortmentPriceId"]);
				AssortmentRegionId = Convert.ToUInt64(row["AssortmentRegionId"]);
				AssortmentQuantity = Convert.ToString(row["AssortmentQuantity"]);

				if (!Convert.IsDBNull(row["AssortmentCost"]))
				{
					AssortmentRealCost = Convert.ToSingle(row["AssortmentCost"]);
					AssortmentCost = NoiseCost(noiseSupplierId, AssortmentSupplierId.Value, random, AssortmentRealCost.Value);
				}

			}
		}

		public float NoiseCost(uint? noiseSupplierId, uint costSupplierId, Random random, float cost)
		{
			if (noiseSupplierId.HasValue && noiseSupplierId != costSupplierId)
				return Convert.ToSingle((1 + (random.NextDouble() * (random.NextDouble() > 0.5 ? 2 : -2) / 100)) * cost);
			else
				return cost;
		}
	}
}