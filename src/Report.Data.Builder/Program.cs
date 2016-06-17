using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using Common.MySql;
using Common.Tools;
using Common.Tools.Threading;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Jobs;
using log4net;
using log4net.Config;

namespace Report.Data.Builder
{
	public class Program
	{
		private static ILog log = LogManager.GetLogger(typeof(Program));

		public static int Main(string[] args)
		{
			try {
				XmlConfigurator.Configure();
				ConnectionHelper.DefaultConnectionStringName = "production";
				ActiveRecordInitialize.Init("production", typeof(Job).Assembly);
				var config = new Config();
				ConfigReader.LoadSettings(config);

				var runner = new JobRunner();
				runner.Jobs.Add(new CalculatorJob(config));

				var cmd = args.FirstOrDefault();

				if (cmd.Match("uninstall")) {
					CommandService.Uninstall();
					return 0;
				}

				if (cmd.Match("install")) {
					CommandService.Install();
					return 0;
				}
				if (cmd.Match("console")) {
					runner.Start();
					if (Console.IsInputRedirected) {
						Console.WriteLine("Для завершения нажмите ctrl-c");
						Console.CancelKeyPress += (e, a) => runner.Stop();
						runner.Cancellation.WaitHandle.WaitOne();
					}
					else {
						Console.WriteLine("Для завершения нажмите любую клавишу");
						Console.ReadLine();
						runner.Stop();
					}
					runner.Join();
					return 0;
				}

				ServiceBase.Run(new CommandService(runner));
				return 0;
			}
			catch(Exception e) {
				log.Error("Ошибка при запуске приложения", e);
				return 1;
			}
		}
	}
}