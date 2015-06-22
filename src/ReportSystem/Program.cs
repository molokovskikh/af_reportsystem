using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.Models;
using Common.MySql;
using Common.Schedule;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Models;
using Common.Web.Ui.NHibernateExtentions;

using Inforoom.Common;
using Inforoom.ReportSystem.Model;
using NDesk.Options;
using NHibernate;
using NHibernate.Mapping.Attributes;
using log4net;
using log4net.Config;
using MySql.Data.MySqlClient;
using With = Common.MySql.With;

namespace Inforoom.ReportSystem
{
	public class AppArgs
	{
		public bool Interval;
		public DateTime From;
		public DateTime To;
		public bool Manual;
		public int ReportId;

		public AppArgs()
		{
			ReportId = -1;
		}
	}

	public class Program
	{
		private static ILog _log = LogManager.GetLogger(typeof(Program));
		public static ISessionFactory Factory;

		[STAThread]
		public static int Main(string[] args)
		{
			var appArgs = new AppArgs();
			try {
				XmlConfigurator.Configure();

				if (Parse(args, appArgs))
					return 0;

				ConnectionHelper.DefaultConnectionStringName = "local";
				With.DefaultConnectionStringName = "local";
				if (!ActiveRecordStarter.IsInitialized) {
					ActiveRecordInitialize.Init(ConnectionHelper.GetConnectionName(),
						typeof(ReportExecuteLog).Assembly, typeof(ContactGroup).Assembly);

					HbmSerializer.Default.HbmAutoImport = false;
					foreach (var cfg in ActiveRecordMediator.GetSessionFactoryHolder().GetAllConfigurations()) {
						cfg.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Common.Models")));
					}
				}
				Factory = ActiveRecordMediator.GetSessionFactoryHolder().GetSessionFactory(typeof(ActiveRecordBase));

				if (appArgs.ReportId == -1)
					throw new Exception("Не указан код отчета для запуска в параметре gr.");

				if (ProcessReport(appArgs.ReportId, appArgs.Manual, appArgs.Interval, appArgs.From, appArgs.To))
					return 0;
				else
					return 1;
			}
			catch (Exception ex) {
				_log.Error(String.Format("Ошибка при запуске отчета {0}", appArgs.ReportId), ex);
				Mailer.MailGlobalErr(ex);
				return 1;
			}
		}

		public static bool Parse(string[] args, AppArgs appArgs)
		{
			var help = false;
			var options = new OptionSet {
				{ "help", "Выводит справку", v => help = v != null },
				{ "gr=", "Код отчета", v => appArgs.ReportId = int.Parse(v) },
				{ "manual=", "Флаг ручного запуска, в случае ручного запуска не производится проверка сотояния отчета", v => appArgs.Manual = bool.Parse(v) },
				{ "inter=", "Флаг сигнализирующей что отчет готовится за период", v => appArgs.Interval = bool.Parse(v) },
				{ "dtFrom=", "Начало периода за который готовится отчет", v => appArgs.From = DateTime.Parse(v) },
				{ "dtTo=", "Окончание периода за который готовится отчет", v => appArgs.To = DateTime.Parse(v) },
			};

			options.Parse(args);
			if (help) {
				Win32.AttachConsole(Win32.ATTACH_PARENT_PROCESS);
				options.WriteOptionDescriptions(Console.Out);
				return true;
			}
			return false;
		}

		public static bool ProcessReport(int generalReportId, bool manual, bool interval, DateTime dtFrom, DateTime dtTo)
		{
			var result = false;
			var reportLog = new ReportExecuteLog();
			GeneralReport report = null;
			using (var session = Factory.OpenSession())
			using (var mc = new MySqlConnection(ConnectionHelper.GetConnectionString())) {
				mc.Open();
				try {
					using(var trx = session.BeginTransaction()) {
						reportLog.GeneralReportCode = generalReportId;
						reportLog.StartTime = DateTime.Now;
						session.Save(reportLog);
						trx.Commit();
					}

					using(var trx = session.BeginTransaction()) {
						report = session.Get<GeneralReport>((uint)generalReportId);
						if (report == null)
							throw new Exception(String.Format("Отчет с кодом {0} не существует.", generalReportId));
						if (!report.Enabled && !manual)
							throw new ReportException("Невозможно выполнить отчет, т.к. отчет выключен.");

						_log.DebugFormat("Запуск отчета {0}", report.Id);
						report.ProcessReports(reportLog, mc, interval, dtFrom, dtTo);
						report.LogSuccess();
						_log.DebugFormat("Отчет {0} выполнился успешно", report.Id);
						reportLog.EndTime = DateTime.Now;
						trx.Commit();
					}
					result = true;
				}
				catch(Exception e) {
					_log.Error(String.Format("Ошибка при запуске отчета {0}", report), e);

					try {
						using(var trx = session.BeginTransaction()) {
							reportLog.EndError = true;
							session.Save(reportLog);
							trx.Commit();
						}
					}
					catch(Exception ex) {
						_log.Error("Не удалось запротоколировать ошибку", ex);
					}

					var reportEx = e as ReportException;
					if (reportEx != null) {
						Mailer.MailReportErr(reportEx.ToString(), reportEx.Payer, report.Id, reportEx.SubreportCode, reportEx.ReportCaption);
						result = true;
					}
					else {
						Mailer.MailGeneralReportErr(report, e);
					}
				}
				finally {
					//не уверен почему так но восстанавливаем состояние задачи только если отчет не выключен
					//этого требует тест ProgramTest но логика мне не понятна
					//подозрительно тк раньше это работало тк переменная была null и блок валился с исключением
					if (report != null && report.Enabled) {
						ScheduleHelper.SetTaskAction(report.Id, "/gr:" + report.Id);
						ScheduleHelper.SetTaskEnableStatus(report.Id, report.Enabled, "GR");
						var taskService = ScheduleHelper.GetService();
						var reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
						ScheduleHelper.DeleteTask(reportsFolder, report.Id, "temp_");
					}
				}
				return result;
			}
		}
	}
}