using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Castle.MonoRail.Framework.Routing;
using Common.Schedule;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using NHibernate;
using ReportTuner.Models;
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

	public class Global : WebApplication
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
			ConnectionHelper.DefaultConnectionStringName = "db";
			ActiveRecordInitialize(
				ConnectionHelper.DefaultConnectionStringName,
				new[] {
					Assembly.Load("ReportTuner"),
					Assembly.Load("Common.Web.Ui")
				});

			Initialize();

			RoutingModuleEx.Engine.Add(new RedirectRoute("/", "Reports/GeneralReports.aspx"));

			RoutingModuleEx.Engine.Add(
				new BugRoute(
					new PatternRoute("/<controller>/[action]")
						.DefaultForAction().Is("index")));

			if (!Path.IsPathRooted(Config.SavedFilesPath))
				Config.SavedFilesPath = FileHelper.MakeRooted(Config.SavedFilesPath);
			CreateDirectoryTree(Config.SavedFilesPath);

			if (!Path.IsPathRooted(Config.ReportHistoryPath))
				Config.ReportHistoryPath = FileHelper.MakeRooted(Config.ReportHistoryPath);
			CreateDirectoryTree(Config.ReportHistoryPath);

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

			var factory = ActiveRecordMediator.GetSessionFactoryHolder().GetSessionFactory(typeof(ActiveRecordBase));
			using (var session = factory.OpenSession())
			using (var trx = session.BeginTransaction()) {
				//Проверяем существование шаблонного отчета в базе, если нет, то приложение не запускаем
				ulong templateReportId;
				if (ulong.TryParse(ConfigurationManager.AppSettings["TemplateReportId"], out templateReportId)) {
					try {
						session.Load<GeneralReport>(templateReportId);
					}
					catch (ObjectNotFoundException ex) {
#if DEBUG
						var r = new GeneralReport();
						session.Save(r);
						ConfigurationManager.AppSettings["TemplateReportId"] = r.Id.ToString();
#else
						throw new ReportTunerException("В файле Web.Config параметр TemplateReportId указывает на несуществующую запись.", ex);
#endif
					}
				}
				else
					throw new ReportTunerException("В файле Web.Config параметр TemplateReportId не существует или настроен некорректно.");

				try {
					new UpdateReportConfig(session).Execute();
					trx.Commit();
				}
				catch(Exception ex) {
					Log.Error("Ошибка при обновлении конфигурации отчетов", ex);
				}
			}
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
						{ NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none" },
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
			var userName = HttpContext.Current.User.Identity.Name;
			if (userName.StartsWith("ANALIT\\", StringComparison.OrdinalIgnoreCase))
				userName = userName.Substring(7);
			Session["UserName"] = userName;
		}
	}
}