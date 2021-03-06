﻿using System;
using System.Linq;
using Common.Tools.Calendar;
using NUnit.Framework;

namespace Report.Data.Builder.Test.Integration
{
	[TestFixture]
	public class ClientRatingFixture
	{
		[Test]
		public void Calculate_client_rating()
		{
			var calculator = new RatingCalculator();

			var regional = new[] { Tuple.Create(500m, 1ul) };
			var clients = new[] { new ClientRating(100u, 1ul, 100m), };
			var rating = calculator.Calculate(regional, clients);
			var result = new[] { new ClientRating(100u, 1ul, 0.2m) };
			Assert.That(rating.ToArray(), Is.EquivalentTo(result));
		}

		[Test]
		public void Save_rating()
		{
			var calculator = new RatingCalculator(DateTime.Today.AddDays(-7), DateTime.Today);
			var ratings = calculator.Ratings();
			RatingCalculator.Save(DateTime.Today.FirstDayOfMonth(), ratings);
		}

		[Test]
		public void Calculate_and_save_rating()
		{
			var runTime = DateTime.Now.FirstDayOfMonth();
			if(DateTime.Today == DateTime.Today.FirstDayOfMonth())
				runTime = runTime.AddMonths(-1);
			var ratings = RatingCalculator.CaclucatedAndSave(runTime);
			Assert.That(ratings.Count(), Is.GreaterThan(0));
		}
	}
}