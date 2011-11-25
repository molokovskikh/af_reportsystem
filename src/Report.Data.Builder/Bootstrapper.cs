using Common.Web.Ui.Helpers;
using log4net.Config;

namespace Report.Data.Builder
{
	public class Bootstrapper : Bootstrapper<JobRunner>
	{
		public void InitializeHostedService(IServiceConfigurator<JobRunner> cfg)
		{
			XmlConfigurator.Configure();
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