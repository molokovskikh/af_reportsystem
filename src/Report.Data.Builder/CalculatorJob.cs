using System;
using Common.Web.Ui.Models.Jobs;

namespace Report.Data.Builder
{
	public class CalculatorJob : IJob
	{
		public Config Config;
		public DateTime Date = DateTime.Today.AddDays(-1);

		public CalculatorJob(Config config)
		{
			Config = config;
		}

		public void Work()
		{
			var ratingCalculator = new RatingCalculator(Date, Date.AddDays(1));
			var rating = ratingCalculator.Ratings();
			ratingCalculator.Save(Date, rating);

			var costCalculator = new CostCalculator();
			var offers = costCalculator.Offers(rating, Config.ThreadCount);
			var averageCosts = costCalculator.Calculate(offers);
			costCalculator.Save(Date, averageCosts);
		}
	}
}