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
		public void Get_offers()
		{
			var clientId = ratings.First().ClientId;
			var offers = calculator.GetOffers(clientId);
			Assert.That(offers.Length, Is.GreaterThan(0));
		}

		[Test]
		public void Calculate_cost()
		{
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>> (
					new List<ClientRating> {
						new ClientRating(1, 1, 0.1m),
					},
					new List<Offer> {
						new Offer(new OfferId(1, 1), 1, 1000)
					}
				),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>> (
					new List<ClientRating> {
						new ClientRating(2, 1, 0.2m),
					},
					new List<Offer> {
						new Offer(new OfferId(1, 1), 1, 900)
					}
				),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>> (
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(new OfferId(1, 1), 1, 800)
					}
				)
			};

			var averageCosts = calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[new OfferId(1, 1)];
			Assert.That(costs.Count, Is.EqualTo(1));
			Assert.That(costs[1u], Is.EqualTo(840m));
		}
	}
}