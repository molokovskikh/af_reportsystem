using System;
using System.Collections;
using System.Linq;
using Common.Tools;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace Report.Data.Builder.Test.Integration
{
	[TestFixture]
	public class CalculateCostFixture : IntegrationFixture
	{
		private CostCalculator calculator;
		private ClientRating[] ratings;

		[SetUp]
		public void Setup()
		{
			var supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore(session);
			supplier.Prices[0].Core.Where(c => c.Producer != null)
				.Each(c => TestAssortment.CheckAndCreate(session, c.Product, c.Producer));
			var client = TestClient.CreateNaked(session);
			var order = new TestOrder(client.Users[0], supplier.Prices[0]);
			order.Processed = false;
			order.WriteTime = DateTime.Today.AddDays(-1);
			order.AddItem(supplier.Prices[0].Core[0], 1);
			session.Save(order);
			session.Transaction.Commit();

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
