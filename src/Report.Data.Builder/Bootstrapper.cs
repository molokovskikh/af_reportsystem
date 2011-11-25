using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Jobs;
using log4net.Config;

namespace Report.Data.Builder
{
	public class Bootstrapper : Bootstrapper<JobRunner>
	{
		public void InitializeHostedService(IServiceConfigurator<JobRunner> cfg)
		{
			XmlConfigurator.Configure();
			ActiveRecordInitialize.Init("production", typeof(Job).Assembly);
			var config = new Config();
			ConfigReader.LoadSettings(config);

			cfg.HowToBuildService(n => {
				var runner = new JobRunner();
				runner.Jobs.Add(new CalculatorJob(config));
			});
			cfg.WhenStarted(s => s.DoStart());
			cfg.WhenStopped(s => s.Stop());
		}
	}
}