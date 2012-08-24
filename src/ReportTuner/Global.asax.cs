using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Text;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Container;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Framework.Views.Aspx;
using Castle.MonoRail.Views.Brail;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using log4net;
using log4net.Config;
using ReportTuner.Models;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;

namespace ReportTuner
{
	public class Config
	{
		public string SavedFilesPath { get; set; }
		public string SavedFileForReportTypesPath { get; set; }
	}

	public class Global : WebApplication, IMonoRailConfigurationEvents
	{
		public static Config Config = new Config();

		public Global() : base(Assembly.Load("ReportTuner"))
		{
			Logger.ErrorSubject = "[ReportTuner] Ошибка в Интерфейсе настройки отчетов";
			Logger.SmtpHost = "box.analit.net";
			LibAssemblies.Add(Assembly.Load("Common.Web.Ui"));
		}

		private void Application_Start(object sender, EventArgs e)
		{
			ConfigReader.LoadSettings(Config);
			ActiveRecordStarter.Initialize(new[] {
				Assembly.Load("ReportTuner"),
				Assembly.Load("Common.Web.Ui")
			},
				ActiveRecordSectionHandler.Instance);

			Initialize();

			RoutingModuleEx.Engine.Add(new RedirectRoute("/", @"Reports/GeneralReports.aspx"));

			RoutingModuleEx.Engine.Add(
				new BugRoute(
					new PatternRoute("/<controller>/[action]")
						.DefaultForAction().Is("index")));

			if (!Path.IsPathRooted(Config.SavedFilesPath))
				Config.SavedFilesPath =
					Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.SavedFilesPath));

			CreateDirectoryTree(Config.SavedFilesPath);

			if (!Path.IsPathRooted(ScheduleHelper.ScheduleAppPath))
				ScheduleHelper.ScheduleAppPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScheduleHelper.ScheduleAppPath));

			if (!Path.IsPathRooted(ScheduleHelper.ScheduleWorkDir))
				ScheduleHelper.ScheduleWorkDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScheduleHelper.ScheduleWorkDir));


#if DEBUG
			var taskService = ScheduleHelper.GetService();
			ScheduleHelper.CreateFolderIfNeeded(taskService);
#endif

			//Проверяем существование шаблонного отчета в базе, если нет, то приложение не запускаем
			ulong _TemplateReportId;
			if (ulong.TryParse(System.Configuration.ConfigurationManager.AppSettings["TemplateReportId"], out _TemplateReportId)) {
				try {
					GeneralReport.Find(_TemplateReportId);
				}
				catch (NotFoundException exp) {
					throw new ReportTunerException("В файле Web.Config параметр TemplateReportId указывает на несуществующую запись.", exp);
				}
			}
			else
				throw new ReportTunerException("В файле Web.Config параметр TemplateReportId не существует или настроен некорректно.");
		}

		private static void CreateDirectoryTree(string dir)
		{
			if (String.IsNullOrEmpty(dir))
				return;

			var parentDir = Path.GetDirectoryName(dir);
			CreateDirectoryTree(parentDir);

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
		}

		private void Session_Start(object sender, EventArgs e)
		{
			//Это имя пользователя добавляем для того, чтобы корректно редактировались контакты
			string UserName = HttpContext.Current.User.Identity.Name;
			if (UserName.StartsWith("ANALIT\\", StringComparison.OrdinalIgnoreCase))
				UserName = UserName.Substring(7);
			Session["UserName"] = UserName;
		}


		private void Session_End(object sender, EventArgs e)
		{
			//Проходим по всем объектам в сессии и если объект поддерживает интефейс IDisposable, то вызываем Dispose()
			for (int i = 0; i < Session.Count; i++)
				if (Session[i] is IDisposable)
					((IDisposable)Session[i]).Dispose();
			//Очищаем коллекцию
			Session.Clear();
			//Производим сборку мусора
			GC.Collect();
		}

		public new void Configure(IMonoRailConfiguration configuration)
		{
			configuration.ControllersConfig.AddAssembly("ReportTuner");
			configuration.ControllersConfig.AddAssembly("Common.Web.Ui");
			configuration.ViewComponentsConfig.Assemblies = new[] {
				"ReportTuner",
				"Common.Web.Ui"
			};
			configuration.ViewEngineConfig.ViewPathRoot = "Views";
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(WebFormsViewEngine), false));
			configuration.ViewEngineConfig.AssemblySources.Add(new AssemblySourceInfo("Common.Web.Ui", "Common.Web.Ui.Views"));
			configuration.ViewEngineConfig.VirtualPathRoot = configuration.ViewEngineConfig.ViewPathRoot;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration.ViewEngineConfig.ViewPathRoot);

			base.Configure(configuration);
		}
	}
}