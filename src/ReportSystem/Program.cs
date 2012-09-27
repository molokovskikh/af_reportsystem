﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using ExecuteTemplate;
using Inforoom.Common;
using Inforoom.ReportSystem.Model;
using log4net;
using log4net.Config;
using MySql.Data.MySqlClient;

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
			var reportLog = new ReportExecuteLog();
			int generalReportId = 0;
			try {
				XmlConfigurator.Configure();
				ConnectionHelper.DefaultConnectionStringName = "Default";
				With.DefaultConnectionStringName = ConnectionHelper.GetConnectionName();
				if (!ActiveRecordStarter.IsInitialized)
					ActiveRecordInitialize.Init(ConnectionHelper.GetConnectionName(), typeof(Supplier).Assembly);

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

				if (generalReportId != -1) {
					var mc = new MySqlConnection(ConnectionHelper.GetConnectionString());
					mc.Open();
					try {
						reportLog.GeneralReportCode = generalReportId;
						reportLog.StartTime = DateTime.Now;
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

						if ((dtGeneralReports != null) && (dtGeneralReports.Rows.Count > 0)) {
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
									gr.ProcessReports();
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

									Mailer.MailGeneralReportErr(
										ex.ToString(),
										(string)drReport[GeneralReportColumns.ShortName],
										(ulong)drReport[GeneralReportColumns.GeneralReportCode]);
								}
							}
						}
						else
							Mailer.MailGlobalErr(String.Format("Отчет с кодом {0} не существует.", generalReportId));
					}
					finally {
						mc.Close();
						reportLog.EndTime = DateTime.Now;
						ArHelper.WithSession(s => s.SaveOrUpdate(reportLog));
					}
				}
				else
					Mailer.MailGlobalErr("Не указан код отчета для запуска в параметре gr.");
			}
			catch (Exception ex) {
				_log.Error(String.Format("Ошибка при запуске отчета {0}", generalReportId), ex);
				Mailer.MailGlobalErr(ex.ToString());
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