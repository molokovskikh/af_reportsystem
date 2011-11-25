using System;
using System.Linq;
using System.Security.Policy;
using Common.Tools.Calendar;
using NUnit.Framework;

namespace Report.Data.Builder.Test
{
	[TestFixture]
	public class PrepareClientRatingFixture
	{
		[Test]
		public void Calculate_client_rating()
		{
			var calculator = new RatingCalculator();

			var regional = new[] {Tuple.Create(500m, 1ul)};
			var clients = new[] {new ClientRating(100u, 1ul, 100m), };
			var rating = calculator.Calculate(regional, clients);
			var result = new [] { Tuple.Create(0.2m, 100u, 1ul) };
			Assert.That(rating.ToArray(), Is.EquivalentTo(result));
		}

		[Test, Ignore]
		public void Save_rating()
		{
			var calculator = new RatingCalculator(DateTime.Today.AddDays(-7), DateTime.Today);
			var ratings = calculator.Ratings();
			calculator.Save(DateTime.Today.FirstDayOfMonth(), ratings);
		}
	}
}