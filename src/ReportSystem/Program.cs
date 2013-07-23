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
		public static GeneralReport generalReport { get; private set; }
		//Выбираем отчеты из базы
		private static DataTable GetGeneralReports(ReportsExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = e.SQL;
			var res = new DataTable();
			e.DataAdapter.Fill(res);
			return res;
		}

		[STAThread]
		public static void Main(string[] args)
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

				ProcessReport(generalReportId, manual, interval, dtFrom, dtTo);
			}
			catch (Exception ex) {
				_log.Error(String.Format("Ошибка при запуске отчета {0}", generalReportId), ex);
				Mailer.MailGlobalErr(ex.ToString());
			}
		}

		public static void ProcessReport(int generalReportId, bool manual, bool interval, DateTime dtFrom, DateTime dtTo)
		{
			var reportLog = new ReportExecuteLog();
			var errorCount = 0;
			using (var mc = new MySqlConnection(ConnectionHelper.GetConnectionString())) {
				mc.Open();
				try {
					using (new ConnectionScope(mc)) {
						ArHelper.WithSession(s => {
							reportLog.GeneralReportCode = generalReportId;
							reportLog.StartTime = DateTime.Now;
							reportLog.EndTime = null;
							s.Save(reportLog);
						});
					}
					//Формируем запрос
					var sqlSelectReports = @"SELECT
cr.*,
p.ShortName
FROM    reports.general_reports cr,
billing.payers p
WHERE
p.PayerId = cr.PayerId
and cr.generalreportcode = " + generalReportId;

					//Выбирает отчеты согласно фильтру
					var dtGeneralReports = MethodTemplate.ExecuteMethod(new ReportsExecuteArgs(sqlSelectReports), GetGeneralReports, null, mc);

					if (dtGeneralReports == null || dtGeneralReports.Rows.Count == 0)
						throw new Exception(String.Format("Отчет с кодом {0} не существует.", generalReportId));

					foreach (DataRow drReport in dtGeneralReports.Rows) {
						if (!Convert.ToBoolean(drReport[GeneralReportColumns.Allow]) && !manual) {
							Mailer.MailGeneralReportErr(
								"Невозможно выполнить отчет, т.к. отчет выключен.",
								(string)drReport[GeneralReportColumns.ShortName],
								(ulong)drReport[GeneralReportColumns.GeneralReportCode]);
							continue;
						}

						try {
							var propertiesLoader = new ReportPropertiesLoader();

							//Создаем каждый отчет отдельно и пытаемся его сформировать
							var gr = new GeneralReport(
								(ulong)drReport[GeneralReportColumns.GeneralReportCode],
								(bool)drReport[GeneralReportColumns.Allow],
								ReadNullableUint32(drReport, GeneralReportColumns.FirmCode),
								ReadNullableUint32(drReport, GeneralReportColumns.ContactGroupId),
								drReport[GeneralReportColumns.EMailSubject].ToString(),
								mc,
								drReport[GeneralReportColumns.ReportFileName].ToString(),
								drReport[GeneralReportColumns.ReportArchName].ToString(),
								(ReportFormats)Enum.Parse(typeof(ReportFormats), drReport[GeneralReportColumns.Format].ToString()),
								propertiesLoader, interval, dtFrom, dtTo, drReport[GeneralReportColumns.ShortName].ToString(),
								Convert.ToBoolean(drReport[GeneralReportColumns.NoArchive]),
								Convert.ToBoolean(drReport[GeneralReportColumns.SendDescriptionFile]));
							generalReport = gr;
							_log.DebugFormat("Запуск отчета {0}", gr.GeneralReportID);
							gr.ProcessReports(reportLog);
							gr.LogSuccess();
							_log.DebugFormat("Отчет {0} выполнился успешно", gr.GeneralReportID);
						}
						catch (Exception ex) {
							var message = String.Format("Ошибка при запуске отчета {0}",
								drReport[GeneralReportColumns.ShortName]);
							_log.Error(message, ex);

							var reportEx = ex as ReportException;
							if (reportEx != null && reportEx.InnerException != null) {
								Mailer.MailReportErr(reportEx.InnerException.ToString(), reportEx.Payer, (ulong)drReport[GeneralReportColumns.GeneralReportCode], reportEx.SubreportCode, reportEx.ReportCaption);
								continue;
							}
							else {
								//Это долно быть именно тут, порядок строк важен
								errorCount++;
							}

							Mailer.MailGeneralReportErr(
								ex.ToString(),
								(string)drReport[GeneralReportColumns.ShortName],
								(ulong)drReport[GeneralReportColumns.GeneralReportCode]);
						}
					}
				}
				finally {
					using (new ConnectionScope(mc)) {
						ArHelper.WithSession(s => {
							reportLog = s.Get<ReportExecuteLog>(reportLog.Id);
							if (reportLog != null) {
								if (errorCount == 0)
									reportLog.EndTime = DateTime.Now;
								reportLog.EndError = errorCount > 0;
								s.Save(reportLog);
								s.Flush();
							}
						});
					}
					ScheduleHelper.SetTaskAction(generalReport.GeneralReportID, "/gr:" + generalReport.GeneralReportID);
					ScheduleHelper.SetTaskEnableStatus(generalReport.GeneralReportID, generalReport.Allow, "GR");
					var taskService = ScheduleHelper.GetService();
					var reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
					ScheduleHelper.DeleteTask(reportsFolder, generalReport.GeneralReportID, "temp_");
				}
			}
		}

		private static uint? ReadNullableUint32(DataRow drReport, string name)
		{
			return (Convert.IsDBNull(drReport[name])) ? null : (uint?)Convert.ToUInt32(drReport[name]);
		}

		//Аргументы для выбора отчетов из базы
		public class ReportsExecuteArgs : ExecuteArgs
		{
			public string SQL;

			public ReportsExecuteArgs(string sql)
			{
				SQL = sql;
			}
		}
	}
}