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
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(1, 1, 0.1m),
					},
					new List<Offer> {
						new Offer(new OfferId(1, 1), 1, 1000, false)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.2m),
					},
					new List<Offer> {
						new Offer(new OfferId(1, 1), 1, 900, false)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(new OfferId(1, 1), 1, 800, false)
					})
			};

			var averageCosts = calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[new OfferId(1, 1)];
			Assert.That(costs.Count, Is.EqualTo(1));
			Assert.That(((OfferAggregates)costs[1u]).Cost, Is.EqualTo(840m));
		}

		[Test]
		public void Ignore_cost_greater_than_threshold()
		{
			calculator.CostThreshold = 90000;
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.5m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 100000, false)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 800, false)
					})
			};

			var averageCosts = calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[offerId];
			Assert.That(costs.Count, Is.EqualTo(1));
			Assert.That(((OfferAggregates)costs[1u]).Cost, Is.EqualTo(560m));
		}

		[Test]
		public void Calculate_quantity_and_junk_cost()
		{
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.5m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 100000, false, 10)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 800, true, 10, 1)
					})
			};
			var averageCosts = calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[offerId];
			Assert.That(costs.Count, Is.EqualTo(1));
			var aggregates = ((OfferAggregates)costs[1u]);
			Assert.That(aggregates.Quantity, Is.EqualTo(20));
			Assert.That(aggregates.Cost, Is.EqualTo(50000));
		}

		[Test]
		public void CalculateQuantityWithSameCore()
		{
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.5m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 100000, false, 15)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 800, true, 10, 1)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 800, false, 10, 1)
					})
			};
			var averageCosts = calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[offerId];
			Assert.That(costs.Count, Is.EqualTo(1));
			var aggregates = ((OfferAggregates)costs[1u]);
			Assert.That(aggregates.Quantity, Is.EqualTo(25));
			Assert.That(aggregates.Cost, Is.EqualTo(50560));
		}

		[Test]
		public void Calculate_no_junk_cost()
		{
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.5m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 100000, false, 10)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.5m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 100000, false, 10)
					})
			};
			var averageCosts = calculator.Calculate(list);
			var costs = (Hashtable)averageCosts[offerId];
			var aggregates = ((OfferAggregates)costs[1u]);
			Assert.That(aggregates.Cost, Is.EqualTo(100000));
		}
	}
}