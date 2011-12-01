using System;
using Common.Tools.Calendar;
using Common.Web.Ui.Models.Jobs;
using log4net;

namespace Report.Data.Builder
{
	public class CalculatorJob : IJob
	{
		private ILog log = LogManager.GetLogger(typeof (CalculatorJob));
		public Config Config;
		public DateTime Date;

		public CalculatorJob(Config config)
		{
			Config = config;
		}

		public void Work()
		{
			Date = DateTime.Today.AddDays(-1);
			var ratings = RatingCalculator.CaclucatedAndSave(Date.AddMonths(-1).FirstDayOfMonth());

			var costCalculator = new CostCalculator();
			var offers = costCalculator.Offers(ratings, Config.ThreadCount);
			var averageCosts = costCalculator.Calculate(offers);
			log.DebugFormat("Начинаю сохранять средние цены");
			var inserted = costCalculator.Save(Date, averageCosts);
			log.DebugFormat("Закончил сохранять средние цены, всего {0}", inserted);
		}
	}
}