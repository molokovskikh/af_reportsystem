﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using ExecuteTemplate;
using Inforoom.Common;
using Inforoom.ReportSystem.Model;
using log4net;
using log4net.Config;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem
{
	class Program
	{
		private static ILog _log = LogManager.GetLogger(typeof(Program));
		public static GeneralReport generalReport { get; private set; }
		//Выбираем отчеты из базы
		static DataTable GetGeneralReports(ReportsExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = e.SQL;
			var res = new DataTable();
			e.DataAdapter.Fill(res);
			return res;
		}

		[STAThread]
		static void Main(string[] args)
		{
			int generalReportId = 0;
			try
			{
				XmlConfigurator.Configure();
				InitActiveRecord();
				//Попытка получить код общего отчета в параметрах
				var interval = false;
				var dtFrom = new DateTime();
				var dtTo = new DateTime();
				generalReportId = Convert.ToInt32(CommandLineUtils.GetCode(@"/gr:"));
				if (!string.IsNullOrEmpty(CommandLineUtils.GetStr(@"/inter:")))
				{
					interval = Convert.ToBoolean(CommandLineUtils.GetStr(@"/inter:"));
					dtFrom = Convert.ToDateTime(CommandLineUtils.GetStr(@"/dtFrom:"));
					dtTo = Convert.ToDateTime(CommandLineUtils.GetStr(@"/dtTo:"));
				}

				if (generalReportId != -1)
				{
					var mc = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
					mc.Open();
					try
					{

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

						if ((dtGeneralReports != null) && (dtGeneralReports.Rows.Count > 0))
						{
							foreach (DataRow drReport in dtGeneralReports.Rows)
							{
								if (!Convert.ToBoolean(drReport[GeneralReportColumns.Allow]))
								{
									Mailer.MailGeneralReportErr(
										"Невозможно выполнить отчет, т.к. отчет выключен.",
										(string)drReport[GeneralReportColumns.ShortName],
										(ulong)drReport[GeneralReportColumns.GeneralReportCode]);
									continue;
								}

								try
								{
									var propertiesLoader = new ReportPropertiesLoader();

									//Создаем каждый отчет отдельно и пытаемся его сформировать
									var gr = new GeneralReport(
										(ulong)drReport[GeneralReportColumns.GeneralReportCode],
										Convert.ToInt32(drReport[GeneralReportColumns.FirmCode]),
										(Convert.IsDBNull(drReport[GeneralReportColumns.ContactGroupId])) ? null : (uint?)Convert.ToUInt32(drReport[GeneralReportColumns.ContactGroupId]),
										drReport[GeneralReportColumns.EMailSubject].ToString(),
										mc,
										drReport[GeneralReportColumns.ReportFileName].ToString(),
										drReport[GeneralReportColumns.ReportArchName].ToString(),
										Convert.ToBoolean(drReport[GeneralReportColumns.Temporary]),
										(ReportFormats)Enum.Parse(typeof(ReportFormats), drReport[GeneralReportColumns.Format].ToString()),
										propertiesLoader, interval, dtFrom, dtTo, drReport[GeneralReportColumns.ShortName].ToString());
									generalReport = gr;
									_log.DebugFormat("Запуск отчета {0}", gr._generalReportID);
									gr.ProcessReports();
									_log.DebugFormat("Отчет {0} выполнился успешно", gr._generalReportID);
								}
								catch (Exception ex)
								{
									Mailer.MailGeneralReportErr(
										ex.ToString(),
										(string)drReport[GeneralReportColumns.ShortName],
										(ulong)drReport[GeneralReportColumns.GeneralReportCode]);
									_log.DebugFormat("В процессе выполнения отчета {0} произошла ошибка: {1}", 
										(ulong)drReport[GeneralReportColumns.GeneralReportCode], ex.ToString());
								}
							}
						}
						else
							Mailer.MailGlobalErr(String.Format("Отчет с кодом {0} не существует.", generalReportId));

					}
					finally
					{
						mc.Close();
					}
				}
				else
					Mailer.MailGlobalErr("Не указан код отчета для запуска в параметре gr.");
			}
			catch (Exception ex)
			{
				_log.Error(String.Format("Ошибка при запуске отчета {0}", generalReportId), ex);
				Mailer.MailGlobalErr(ex.ToString());
			}
		}

		private static void InitActiveRecord()
		{
			var config = new InPlaceConfigurationSource();
			config.Add(typeof(ActiveRecordBase),
				new Dictionary<string, string> {
					{NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
                    {NHibernate.Cfg.Environment.ConnectionStringName, "DB"},
					{NHibernate.Cfg.Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"},
					{NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none"}
				});
			ActiveRecordStarter.Initialize(new[] { typeof(Supplier).Assembly }, config);
		}

		//Аргументы для выбора отчетов из базы
		internal class ReportsExecuteArgs : ExecuteArgs
		{
			internal string SQL;

			public ReportsExecuteArgs(string sql)
			{
				SQL = sql;
			}
		}
	}
}
