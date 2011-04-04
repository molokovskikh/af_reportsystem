using System;
using System.Data;
using log4net;
using log4net.Config;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Net.Mail;
using Inforoom.Common;
using ExecuteTemplate;
using Inforoom.ReportSystem.Properties;

namespace Inforoom.ReportSystem
{
	class Program
	{
		private static ILog _log = LogManager.GetLogger(typeof(Program));

		//Вспомогательная функция отправки письма
		static void Mail(string from, string messageTo, string subject, string body)
		{
			try
			{
				var message = new MailMessage(from, messageTo, subject, body);
				var client = new SmtpClient(Settings.Default.SMTPHost);
				message.IsBodyHtml = false;
				message.BodyEncoding = System.Text.Encoding.UTF8;
				client.Send(message);
			}
			catch(Exception e)
			{
				_log.Error("Ошибка при отправке уведомления", e);
			}
		}

		//Сообщение о глобальной ошибке, возникшей в результате работы программы
		static void MailGlobalErr(string errDesc)
		{
			try
			{
				Mail(Settings.Default.ErrorFrom, Settings.Default.ErrorReportMail, "Ошибка при запуске программы отчетов",
					String.Format("Параметры запуска : {0}\r\nОшибка : {1}", String.Join("  ", Environment.GetCommandLineArgs()), errDesc));
			}
			catch (Exception e)
			{
				_log.Error("Ошибка при отправке уведомления", e);
			}
		}

		//Сообщение об ошибке, возникшей в результате построения общего отчета
		static void MailGeneralReportErr(string errDesc, string shortName, ulong generalReportCode)
		{
			Mail(Settings.Default.ErrorFrom, Settings.Default.ErrorReportMail, "Ошибка при запуске отчетa для " + shortName,
				String.Format("Код отчета : {0}\r\nОшибка : {1}", generalReportCode, errDesc));
		}

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
									MailGeneralReportErr(
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
										propertiesLoader, interval, dtFrom, dtTo);
									gr.ProcessReports();
								}
								catch (Exception ex)
								{
									MailGeneralReportErr(
										ex.ToString(),
										(string)drReport[GeneralReportColumns.ShortName],
										(ulong)drReport[GeneralReportColumns.GeneralReportCode]);
								}
							}
						}
						else
							MailGlobalErr(String.Format("Отчет с кодом {0} не существует.", generalReportId));

					}
					finally
					{
						mc.Close();
					}
				}
				else
					MailGlobalErr("Не указан код отчета для запуска в параметре gr.");
			}
			catch (Exception ex)
			{
				_log.Error(String.Format("Ошибка при запуске отчета {0}", generalReportId), ex);
				MailGlobalErr(ex.ToString());
			}
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
