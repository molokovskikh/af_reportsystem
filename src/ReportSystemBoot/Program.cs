using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.Tools;
using log4net;
using log4net.Config;

namespace ReportSystemBoot
{
	public class Program
	{
		private const string AcceessKey = "/access:";

		private static int Main(string[] args)
		{
			XmlConfigurator.Configure();
			var logger = LogManager.GetLogger(typeof(Program));
			string cmd = null;
			int exitCode;
			try {
				var user = ConfigurationManager.AppSettings["user"];
				var password = ConfigurationManager.AppSettings["password"];
				var domainname = ConfigurationManager.AppSettings["domain"];
				var bin = ConfigurationManager.AppSettings["bin"];
				if (String.IsNullOrWhiteSpace(user))
					throw new Exception("Не задано имя пользователя для интерактивного запуска");
				if (String.IsNullOrWhiteSpace(bin))
					throw new Exception("Не задан исполняемый фай");

				cmd = Assembly.GetExecutingAssembly().Location;
				if (args.Length >= 1)
					cmd += " " + args.Implode(" ");
				logger.InfoFormat("Попытка запуска отчета: {0}", cmd);

				if (!args.Any(a => a.StartsWith(AcceessKey))) {
					cmd += string.Format(" {0}true", AcceessKey);
					exitCode = ProcessStarter.StartProcessInteractivly(cmd, user, password, domainname);
				}
				else {
					AppDomain domain = null;
					try {
						if (!Path.IsPathRooted(bin))
							bin = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, bin));
						var config = bin + ".config";
						var setup = new AppDomainSetup {
							ApplicationBase = Path.GetDirectoryName(bin),
							ShadowCopyFiles = "true",
							ConfigurationFile = config
						};
						domain = AppDomain.CreateDomain("freeReportDomain", null, setup);
						exitCode = domain.ExecuteAssembly(bin, args);
					}
					finally {
						if (domain != null)
							AppDomain.Unload(domain);
					}
				}
				logger.InfoFormat("Отчет {0} отработал успешно", cmd);
			}
			catch (Exception exception) {
				logger.Error("Ошибка при запуске отчета : " + cmd, exception);
				exitCode = 1;
			}
			return exitCode;
		}
	}
}