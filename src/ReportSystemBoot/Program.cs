using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.WindowsService;
using log4net;
using log4net.Config;

namespace ReportSystemBoot
{
	class Program
	{
		static void Main(string[] args)
		{
			XmlConfigurator.Configure();
			ILog logger = LogManager.GetLogger(typeof(Program));

			System.Reflection.Assembly ass = System.Reflection.Assembly.GetExecutingAssembly();
			var bootAppName = System.IO.Path.GetFileNameWithoutExtension(ass.Location).Replace("Boot", null);
			var arg = args.Aggregate(string.Empty, (current, s) => current + " " + s);
			if (args.Length >= 1)
				bootAppName += arg;
			logger.InfoFormat("Попытка запуска отчета: {0}", bootAppName);
			try
			{
				ProcessStarter.StartProcessInteractivly(bootAppName, "Zolotarev", "GhtpbltyNAnalit", "analit");
				logger.Info("Отчет отработал успешно");
			}
			catch (Exception exception)
			{
				logger.Error("Ошибка при запуске отчета : " + bootAppName, exception);
			}
		}
	}
}
