using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.Tools;
using Inforoom.WindowsService;
using log4net;
using log4net.Config;

namespace ReportSystemBoot
{
	internal class Program
	{
		private const string AcceessKey = "/access:";

		private static void DeployFiles(ILog logger)
		{
			try {
				var releasePath = Settings.Default.ReleasePath;
				var toPath = ".";
				if (!Directory.Exists(releasePath))
					Directory.CreateDirectory(releasePath);
				var files = Directory.GetFiles(releasePath).ToList();
				var releaseFiles = files.Where(f => !f.Contains("ReportSystemBoot") && !f.Contains("log4net") && !f.Contains("ProcessPrivileges")).ToList();
				if (releaseFiles.Count == 0)
					return;
				logger.Info("Обновление файлов...");
				foreach (var file in releaseFiles) {
					File.Copy(file, Path.Combine(toPath, Path.GetFileName(file)), true);
				}
				foreach (var file in files) {
					File.Delete(file);
				}
				logger.Info("Файлы обновлены");
			}
			catch (Exception e) {
				logger.Info("Не удалось обновить файлы: ", e);
				return;
			}
		}

		private static void Main(string[] args)
		{
			XmlConfigurator.Configure();
			var logger = LogManager.GetLogger(typeof(Program));
			var accessArgument = args.FirstOrDefault(a => a.StartsWith(AcceessKey));
			var accessModified = accessArgument != null && Convert.ToBoolean(accessArgument.Substring(AcceessKey.Length));
			if (!accessModified)
				DeployFiles(logger);

			var ass = Assembly.GetExecutingAssembly();
			var location = ass.Location;
			var bootAppName = Path.GetFileNameWithoutExtension(location);
			var appName = bootAppName;
#if DEBUG
			location = @"D:\Projects\ReportSystem\src\ReportSystem\bin\Debug";
#else
			location = Path.GetDirectoryName(ass.Location);
#endif
			var arg = args.Aggregate(string.Empty, (current, s) => current + " " + s);
			if (args.Length >= 1)
				bootAppName += arg;
			logger.InfoFormat("Попытка запуска отчета: {0}", bootAppName);
			try {
				if (!accessModified) {
					bootAppName += string.Format(" {0}true", AcceessKey);
#if !DEBUG
					ProcessStarter.StartProcessInteractivly(bootAppName, "runer", "zcxvcb", "analit");
#else
					ProcessStarter.StartProcessInteractivly(bootAppName, "Zolotarev", "*****", "analit");
#endif
				}
				else {
					AppDomain domain = null;
					try {
						var setup = new AppDomainSetup {
							ApplicationBase = location,
							ShadowCopyFiles = "true",
							ShadowCopyDirectories = location,
							ConfigurationFile = "ReportSystem.exe.config"
						};
						domain = AppDomain.CreateDomain("freeReportDomain", null, setup);
						domain.ExecuteAssembly(Path.Combine(location, appName.Replace("Boot", ".exe")), args);
					}
					finally {
						if (domain != null)
							AppDomain.Unload(domain);
					}
				}
				logger.InfoFormat("Отчет {0} отработал успешно", bootAppName);
			}
			catch (Exception exception) {
				logger.Error("Ошибка при запуске отчета : " + bootAppName, exception);
			}
		}
	}
}