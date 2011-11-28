﻿using System;
using Common.Tools.Calendar;
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
			var ratings = RatingCalculator.CaclucatedAndSave(Date.AddMonths(-1).FirstDayOfMonth());

			var costCalculator = new CostCalculator();
			var offers = costCalculator.Offers(ratings, Config.ThreadCount);
			var averageCosts = costCalculator.Calculate(offers);
			costCalculator.Save(Date, averageCosts);
		}
	}
}