using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Report.Data.Builder.Test
{
	[TestFixture]
	public class CalculateCostFixture
	{
		private CostCalculator calculator;
		private ClientRating[] ratings;

		[SetUp]
		public void Setup()
		{
			calculator = new CostCalculator();

			ratings = RatingCalculator
				.Caclucated(DateTime.Today.AddDays(-10), DateTime.Today)
				.Take(3)
				.ToArray();
		}

		[Test]
		public void Calculate_average_costs()
		{
			var result = calculator.Calculate(calculator.Offers(ratings, 2));
			Assert.That(result.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Save_costs()
		{
			var result = calculator.Calculate(calculator.Offers(ratings, 2));
			calculator.Save(DateTime.Today, result);
		}

		[Test]
		public void SaveWithZeroCost()
		{
			var result = new Hashtable();
			var offerId = new OfferId(1, 1);
			var aggregator = new OfferAggregates {
				Cost = 0,
				Quantity = 1
			};
			var costs = new Hashtable();
			costs[new AggregateId(0, 0)] = aggregator;
			aggregator = new OfferAggregates {
				Cost = 2,
				Quantity = 1
			};
			costs[new AggregateId(1, 1)] = aggregator;
			result[offerId] = costs;
			var count = calculator.Save(DateTime.Today, result);
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void Get_offers()
		{
			var clientId = ratings.First().ClientId;
			var offers = calculator.GetOffers(clientId);
			Assert.That(offers.Length, Is.GreaterThan(0));
		}
	}
}