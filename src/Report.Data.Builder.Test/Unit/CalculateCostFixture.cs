using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Report.Data.Builder.Test.Unit
{
	[TestFixture]
	public class CalculateCostFixture
	{
		private CostCalculator _calculator;

		[SetUp]
		public void Setup()
		{
			_calculator = new CostCalculator();
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
						new Offer(new OfferId(1, 1), 1, 1, 1000, false)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.2m),
					},
					new List<Offer> {
						new Offer(new OfferId(1, 1), 1, 1, 900, false)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(new OfferId(1, 1), 1, 1, 800, false)
					})
			};

			var averageCosts = _calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[new OfferId(1, 1)];
			Assert.That(costs.Count, Is.EqualTo(1));
			Assert.That(((OfferAggregates)costs["1|1"]).Cost, Is.EqualTo(840m));
		}

		[Test]
		public void Ignore_cost_greater_than_threshold()
		{
			_calculator.CostThreshold = 90000;
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.5m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 100000, false)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 800, false)
					})
			};

			var averageCosts = _calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[offerId];
			Assert.That(costs.Count, Is.EqualTo(1));
			Assert.That(((OfferAggregates)costs["1|1"]).Cost, Is.EqualTo(800m));
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
						new Offer(offerId, 1, 1, 100000, false, 10)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.7m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 800, true, 10, 1)
					})
			};
			var averageCosts = _calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[offerId];
			Assert.That(costs.Count, Is.EqualTo(1));
			var aggregates = ((OfferAggregates)costs["1|1"]);
			Assert.That(aggregates.Quantity, Is.EqualTo(10));
			Assert.That(aggregates.Cost, Is.EqualTo(100000));
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
						new Offer(offerId, 1, 1, 100000, false, 15)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.3m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 800, true, 10, 1)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.3m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 800, false, 10, 1)
					})
			};
			var averageCosts = _calculator.Calculate(list);
			Assert.That(averageCosts.Count, Is.EqualTo(1));
			var costs = (Hashtable)averageCosts[offerId];
			Assert.That(costs.Count, Is.EqualTo(1));
			var aggregates = ((OfferAggregates)costs["1|1"]);
			Assert.That(aggregates.Quantity, Is.EqualTo(15));
			Assert.That(aggregates.Cost, Is.EqualTo(62800));
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
						new Offer(offerId, 1, 1, 100000, false, 10)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.5m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 100000, false, 10)
					})
			};
			var averageCosts = _calculator.Calculate(list);
			var costs = (Hashtable)averageCosts[offerId];
			var aggregates = ((OfferAggregates)costs["1|1"]);
			Assert.That(aggregates.Cost, Is.EqualTo(100000));
		}

		[Test]
		public void CalculateOnlyJunkCost()
		{
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.5m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 100000, true, 10)
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(3, 1, 0.5m)
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 100000, true, 10)
					})
			};
			var averageCosts = _calculator.Calculate(list);
			var costs = (Hashtable)averageCosts[offerId];
			var aggregates = ((OfferAggregates)costs["1|1"]);
			Assert.That(aggregates.Cost, Is.EqualTo(0));
			Assert.That(aggregates.Quantity, Is.EqualTo(10));
		}

		[Test]
		public void CalculateWithSameAssortment()
		{
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 1m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 100, false, 10),
						new Offer(offerId, 1, 1, 125, false, 10, 1),
						new Offer(offerId, 1, 1, 150, false, 1, 2)
					})
			};
			var averageCosts = _calculator.Calculate(list);
			var costs = (Hashtable)averageCosts[offerId];
			var aggregates = ((OfferAggregates)costs["1|1"]);
			Assert.That(aggregates.Cost, Is.EqualTo(125));
			Assert.That(aggregates.Quantity, Is.EqualTo(21));
		}

		[Test]
		public void CalculateWithNoFullRating()
		{
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 0.2m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 100, false, 10),
						new Offer(offerId, 2, 1, 50, false, 10, 1),
					}),
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(1, 1, 0.6m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 20, false, 10),
						new Offer(offerId, 2, 1, 50, false, 10, 1),
					})
			};
			var averageCosts = _calculator.Calculate(list);
			var costs = (Hashtable)averageCosts[offerId];
			var aggregates = ((OfferAggregates)costs["1|1"]);
			Assert.That(aggregates.Cost, Is.EqualTo(40));
			Assert.That(aggregates.Quantity, Is.EqualTo(10));
			aggregates = ((OfferAggregates)costs["2|1"]);
			Assert.That(aggregates.Cost, Is.EqualTo(50));
			Assert.That(aggregates.Quantity, Is.EqualTo(10));
		}

		[Test]
		public void CalculateWithSameProductInOtherPrice()
		{
			var offerId = new OfferId(1, 1);
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 1m),
					},
					new List<Offer> {
						new Offer(offerId, 1, 1, 100, false, 10, 0, "Code"),
						new Offer(offerId, 1, 1, 50, false, 10, 1, "Code"),
						new Offer(offerId, 1, 1, 150, false, 10, 2, "Code", priceId: 1)
					})
			};
			var averageCosts = _calculator.Calculate(list);
			var costs = (Hashtable)averageCosts[offerId];
			var aggregates = ((OfferAggregates)costs["1|1"]);
			Assert.That(aggregates.Cost, Is.EqualTo(100));
			Assert.That(aggregates.Quantity, Is.EqualTo(20));
		}

		[Test]
		public void Get_max_quiantity_from_all_prices()
		{
			var offerId = new OfferId(1, 1);
			var offers = new List<Offer>();
			var list = new List<Tuple<IEnumerable<ClientRating>, IEnumerable<Offer>>> {
				Tuple.Create<IEnumerable<ClientRating>, IEnumerable<Offer>>(
					new List<ClientRating> {
						new ClientRating(2, 1, 1m),
					},
					offers)
			};

			offers.Add(new Offer(offerId, 1, 1, 100, false, 50, priceId: 1));
			offers.Add(new Offer(offerId, 1, 1, 101, false, 200, priceId: 1));
			offers.Add(new Offer(offerId, 1, 1, 100, false, 50, priceId: 2));

			var averageCosts = _calculator.Calculate(list);
			var costs = (Hashtable)averageCosts[offerId];
			var aggregates = ((OfferAggregates)costs["1|1"]);
			Assert.That(aggregates.Quantity, Is.EqualTo(250));
		}
	}
}
