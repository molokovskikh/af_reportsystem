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
using Common.Schedule;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using log4net;
using log4net.Config;
using ReportTuner.Models;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;
using Common.MySql;

namespace ReportTuner
{
	public class Config
	{
		public Config()
		{
			ReportHistoryStorageInterval = 30;
		}

		public string SavedFilesPath { get; set; }
		public string SavedFilesReportTypePath { get; set; }
		public string ReportHistoryPath { get; set; }
		public double ReportHistoryStorageInterval { get; set; }
	}

	public class Global : WebApplication, IMonoRailConfigurationEvents
	{
		public static Config Config = new Config();

		public Global() : base(Assembly.Load("ReportTuner"))
		{
			Logger.ErrorSubject = "[ReportTuner] Ошибка в Интерфейсе настройки отчетов";
			LibAssemblies.Add(Assembly.Load("Common.Web.Ui"));
		}

		private void Application_Start(object sender, EventArgs e)
		{
			ConfigReader.LoadSettings(Config);
			ConnectionHelper.DefaultConnectionStringName = "Default";
			With.DefaultConnectionStringName = ConnectionHelper.GetConnectionName();
			ActiveRecordInitialize(
				ConnectionHelper.GetConnectionName(),
				new[] {
					Assembly.Load("ReportTuner"),
					Assembly.Load("Common.Web.Ui")
				});

			Initialize();

			RoutingModuleEx.Engine.Add(new RedirectRoute("/", @"Reports/GeneralReports.aspx"));

			RoutingModuleEx.Engine.Add(
				new BugRoute(
					new PatternRoute("/<controller>/[action]")
						.DefaultForAction().Is("index")));

			if (!Path.IsPathRooted(Config.SavedFilesPath))
				Config.SavedFilesPath = FileHelper.MakeRooted(Config.SavedFilesPath);

			CreateDirectoryTree(Config.SavedFilesPath);

			if (!Path.IsPathRooted(Config.ReportHistoryPath))
				Config.ReportHistoryPath = FileHelper.MakeRooted(Config.ReportHistoryPath);

			if (!Path.IsPathRooted(Config.SavedFilesReportTypePath))
				Config.SavedFilesReportTypePath = FileHelper.MakeRooted(Config.SavedFilesReportTypePath);

			CreateDirectoryTree(Config.SavedFilesReportTypePath);

			if (!Path.IsPathRooted(ScheduleHelper.ScheduleAppPath))
				ScheduleHelper.ScheduleAppPath = FileHelper.MakeRooted(ScheduleHelper.ScheduleAppPath);

			if (!Path.IsPathRooted(ScheduleHelper.ScheduleWorkDir))
				ScheduleHelper.ScheduleWorkDir = FileHelper.MakeRooted(ScheduleHelper.ScheduleWorkDir);


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
		private void ActiveRecordInitialize(string connectionName, Assembly[] assemblies)
		{
			if (!ActiveRecordStarter.IsInitialized) {
				var config = new InPlaceConfigurationSource();
				config.IsRunningInWebApp = true;
				config.PluralizeTableNames = true;
				config.Add(typeof(ActiveRecordBase),
					new Dictionary<string, string> {
						{ NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
						{ NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
						{ NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
						{ NHibernate.Cfg.Environment.ConnectionStringName, connectionName },
						{ NHibernate.Cfg.Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle" },
						{ NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none" },
						{ NHibernate.Cfg.Environment.ShowSql, "true" },
						{ NHibernate.Cfg.Environment.FormatSql, "true" },
						{ NHibernate.Cfg.Environment.Isolation, "ReadCommitted" }
					});
				ActiveRecordStarter.Initialize(assemblies, config);
			}
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