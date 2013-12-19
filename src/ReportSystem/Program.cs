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
using Common.Web.Ui.NHibernateExtentions;
using ExecuteTemplate;
using Inforoom.Common;
using Inforoom.ReportSystem.Model;
using NHibernate;
using NHibernate.Mapping.Attributes;
using log4net;
using log4net.Config;
using MySql.Data.MySqlClient;
using With = Common.MySql.With;

namespace Inforoom.ReportSystem
{
	public class Program
	{
		private static ILog _log = LogManager.GetLogger(typeof(Program));
		private static ISessionFactory factory;

		[STAThread]
		public static int Main(string[] args)
		{
			int generalReportId = 0;
			try {
				XmlConfigurator.Configure();
				ConnectionHelper.DefaultConnectionStringName = "Default";
				With.DefaultConnectionStringName = ConnectionHelper.GetConnectionName();
				if (!ActiveRecordStarter.IsInitialized) {
					ActiveRecordInitialize.Init(ConnectionHelper.GetConnectionName(), typeof(ReportExecuteLog).Assembly);

					foreach (NHibernate.Cfg.Configuration cfg in ActiveRecordMediator.GetSessionFactoryHolder().GetAllConfigurations()) {
						cfg.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Common.Models")));
					}
				}
				factory = ActiveRecordMediator.GetSessionFactoryHolder().GetSessionFactory(typeof(ActiveRecordBase));

				//Попытка получить код общего отчета в параметрах
				var interval = false;
				var dtFrom = new DateTime();
				var dtTo = new DateTime();
				var manual = false;
				generalReportId = Convert.ToInt32(CommandLineUtils.GetCode(@"/gr:", args));
				if (!string.IsNullOrEmpty(CommandLineUtils.GetStr(@"/manual:", args))) {
					manual = Convert.ToBoolean(CommandLineUtils.GetStr(@"/manual:", args));
				}

				if (!string.IsNullOrEmpty(CommandLineUtils.GetStr(@"/inter:", args))) {
					interval = Convert.ToBoolean(CommandLineUtils.GetStr(@"/inter:", args));
					dtFrom = Convert.ToDateTime(CommandLineUtils.GetStr(@"/dtFrom:", args));
					dtTo = Convert.ToDateTime(CommandLineUtils.GetStr(@"/dtTo:", args));
				}

				if (generalReportId == -1)
					throw new Exception("Не указан код отчета для запуска в параметре gr.");

				if (ProcessReport(generalReportId, manual, interval, dtFrom, dtTo))
					return 0;
				else
					return 1;
			}
			catch (Exception ex) {
				_log.Error(String.Format("Ошибка при запуске отчета {0}", generalReportId), ex);
				Mailer.MailGlobalErr(ex);
				return 1;
			}
		}

		public static bool ProcessReport(int generalReportId, bool manual, bool interval, DateTime dtFrom, DateTime dtTo)
		{
			var result = false;
			var reportLog = new ReportExecuteLog();
			GeneralReport report = null;
			using (var session = factory.OpenSession())
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
							throw new Exception("Невозможно выполнить отчет, т.к. отчет выключен.");

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
					if (reportEx != null && reportEx.InnerException != null && report.Reports.Count > 1) {
						Mailer.MailReportErr(reportEx.InnerException.ToString(), reportEx.Payer, report.Id, reportEx.SubreportCode, reportEx.ReportCaption);
						result = true;
					}

					Mailer.MailGeneralReportErr(report, e);
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