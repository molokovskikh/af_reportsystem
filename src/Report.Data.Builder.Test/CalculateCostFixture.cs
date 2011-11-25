using System;
using System.Collections;
using System.Data;
using System.Linq;
using NUnit.Framework;
using log4net.Config;

namespace Report.Data.Builder.Test
{
	[TestFixture]
	public class CalculateCostFixture
	{
		private CostCalculator calculator;
		private ClientRating[] ratings;
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
			BasicConfigurator.Configure();
			var result = calculator.Calculate(calculator.Offers(ratings, 5));
			Assert.That(result.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Save_costs()
		{
			var result = calculator.Calculate(calculator.Offers(ratings, 10));
			calculator.Save(DateTime.Today, result);
		}

		[Test]
		public void Get_offers()
		{
			var offers = calculator.GetOffers(1606);
			Assert.That(offers.Length, Is.GreaterThan(0));
		}
	}
}