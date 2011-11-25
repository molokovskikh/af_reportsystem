﻿using System;
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
		public void Get_offers()
		{
			var clientId = ratings.First().ClientId;
			var offers = calculator.GetOffers(clientId);
			Assert.That(offers.Length, Is.GreaterThan(0));
		}
	}
}