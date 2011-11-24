using System;
using System.Collections;
using System.Data;
using System.Linq;
using NUnit.Framework;

namespace Report.Data.Builder.Test
{
	public struct OfferId
	{
		public uint SupplierId;
		public ulong RegionId;

		public OfferId(uint supplierId, ulong regionId)
		{
			SupplierId = supplierId;
			RegionId = regionId;
		}
	}

	public class AvgCost
	{
		public OfferId Id;

		public uint AssortmentId;
		public DateTime Date;
		public decimal Cost;
	}

	public class Rating
	{
		public uint ClientId;
		public ulong RegionId;
		public decimal Value;

		public Rating(uint clientId, ulong regionId, decimal value)
		{
			ClientId = clientId;
			RegionId = regionId;
			Value = value;
		}
	}

	public class Offer
	{
		public OfferId Id;

		public uint AssortmentId;
		public decimal Cost;

		public Offer(OfferId id, uint assortmentId, decimal cost)
		{
			Id = id;
			AssortmentId = assortmentId;
			Cost = cost;
		}
	}

	[TestFixture]
	public class CalculateCostFixture
	{
		private CostCalculator calculator;
		private Rating[] ratings;
		private uint[] clients;

		[SetUp]
		public void Setup()
		{
			calculator = new CostCalculator();

			ratings = RatingCalculator
				.Caclucated(DateTime.Today.AddDays(-10), DateTime.Today)
				.ToArray();

			clients = new[] {1606u, 369u};
		}

		[Test]
		public void Calculate_average_costs()
		{
			var result = calculator.Calculate(clients, ratings);
			Assert.That(result.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Save_costs()
		{
			var result = calculator.Calculate(clients, ratings);
			calculator.Save(DateTime.Today, result);
		}
	}
}