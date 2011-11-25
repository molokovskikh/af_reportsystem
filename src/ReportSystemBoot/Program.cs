using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inforoom.WindowsService;
using log4net;
using log4net.Config;

namespace ReportSystemBoot
{
	class Program
	{
		public static string NormalizeDir(string InputDir)
		{
			string result = Path.GetFullPath(InputDir);
			if ((result.Length > 0) && (result[result.Length - 1] != Path.DirectorySeparatorChar))
				result += Path.DirectorySeparatorChar;
			return result;
		}

		static void DeployFiles(ILog logger)
		{
			try {
				var releasePath = NormalizeDir(Settings.Default.ReleasePath);				
				var toPath = NormalizeDir(".");
				if(!Directory.Exists(releasePath)) Directory.CreateDirectory(releasePath);
				var files = Directory.GetFiles(releasePath).ToList();
				var releaseFiles = files.Where(f => !f.Contains("ReportSystemBoot") && !f.Contains("log4net") && !f.Contains("ProcessPrivileges")).ToList();
				if (releaseFiles.Count == 0) return;
				logger.Info("Обновление файлов...");
				foreach (var file in releaseFiles) {
					File.Copy(file, toPath + Path.GetFileName(file), true);
				}
				foreach (var file in files) {
					File.Delete(file);
				}
				logger.Info("Файлы обновлены");
			}
			catch(Exception e) {
				logger.Info("Не удалось обновить файлы: ", e);
				return;
			}
		}

		static void Main(string[] args)
		{
			
			XmlConfigurator.Configure();
			ILog logger = LogManager.GetLogger(typeof(Program));

			DeployFiles(logger);

			System.Reflection.Assembly ass = System.Reflection.Assembly.GetExecutingAssembly();
			var bootAppName = System.IO.Path.GetFileNameWithoutExtension(ass.Location).Replace("Boot", null);
			var arg = args.Aggregate(string.Empty, (current, s) => current + " " + s);
			if (args.Length >= 1)
				bootAppName += arg;
			logger.InfoFormat("Попытка запуска отчета: {0}", bootAppName);
			try
			{	
#if !DEBUG			
				ProcessStarter.StartProcessInteractivly(bootAppName,"runer", "zcxvcb", "analit");
#else
				ProcessStarter.StartProcessInteractivly(bootAppName, "tyutin", "*****", "analit");
#endif
				logger.InfoFormat("Отчет {0} отработал успешно", bootAppName);
			}
			catch (Exception exception)
			{
				logger.Error("Ошибка при запуске отчета : " + bootAppName, exception);
			}
		}
	}
}
