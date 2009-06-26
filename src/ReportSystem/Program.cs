using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using MySql.Data.MySqlClient;
using Inforoom.ReportSystem.Filters;
using System.Configuration;
using System.Net.Mail;
using Inforoom.Common;
using ExecuteTemplate;
using Inforoom.ReportSystem.Properties;

namespace Inforoom.ReportSystem
{
	class Program
	{
		//Вспомогательная функция отправки письма
		static void Mail(string From, string MessageTo, string Subject, string Body)
		{
			try
			{
				MailMessage message = new MailMessage(From,
#if (TESTING)
 "s.morozov@analit.net",
#else
					MessageTo, 
#endif
 Subject, Body);
				SmtpClient Client = new SmtpClient(Settings.Default.SMTPHost);
				message.IsBodyHtml = false;
				message.BodyEncoding = System.Text.Encoding.UTF8;
				Client.Send(message);
			}
			catch
			{
			}
		}

		//Сообщение о глобальной ошибке, возникшей в результате работы программы
		static void MailGlobalErr(string ErrDesc)
		{
			Mail(Properties.Settings.Default.ErrorFrom, Properties.Settings.Default.ErrorReportMail, "Ошибка при запуске программы отчетов",
				String.Format("Параметры запуска : {0}\r\nОшибка : {1}", String.Join("  ", Environment.GetCommandLineArgs()), ErrDesc));
		}

		//Сообщение об ошибке, возникшей в результате построения общего отчета
		static void MailGeneralReportErr(string ErrDesc, string ShortName, ulong GeneralReportCode)
		{
			Mail(Properties.Settings.Default.ErrorFrom, Properties.Settings.Default.ErrorReportMail, "Ошибка при запуске отчетa для " + ShortName,
				String.Format("Код отчета : {0}\r\nОшибка : {1}", GeneralReportCode, ErrDesc));
		}

		//Выбираем отчеты из базы
		static DataTable GetGeneralReports(ReportsExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = e.SQL;
			DataTable res = new DataTable();
			e.DataAdapter.Fill(res);
			return res;
		}

		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				//Попытка получить код общего отчета в параметрах
				int GeneralReportID = CommandLineUtils.GetCode(@"/gr:");

				string sqlSelectReports;

				if (GeneralReportID != -1)
				{
					MySqlConnection mc = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
					mc.Open();
					try
					{

						//Формируем запрос
						sqlSelectReports =
@"SELECT  
  cr.*, 
  min(cd.FirmCode) As FirmCode,
  p.ShortName  
FROM    reports.general_reports cr,
        billing.payers p, 
        usersettings.clientsdata cd  
WHERE   
     p.PayerId = cr.PayerId
and cd.BillingCode = cr.PayerId
and cr.generalreportcode = " + GeneralReportID +
" group by cr.generalreportcode";

						//Выбирает отчеты согласно фильтру
						DataTable dtGeneralReports = MethodTemplate.ExecuteMethod<ReportsExecuteArgs, DataTable>(new ReportsExecuteArgs(sqlSelectReports), GetGeneralReports, null, mc, true, null, false, null);

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
									//Создаем каждый отчет отдельно и пытаемся его сформировать
									GeneralReport gr = new GeneralReport(
										(ulong)drReport[GeneralReportColumns.GeneralReportCode],
										Convert.ToInt32(drReport[GeneralReportColumns.FirmCode]),
										(Convert.IsDBNull(drReport[GeneralReportColumns.ContactGroupId])) ? null : (uint?)Convert.ToUInt32(drReport[GeneralReportColumns.ContactGroupId]),
										drReport[GeneralReportColumns.EMailSubject].ToString(),
										mc,
										drReport[GeneralReportColumns.ReportFileName].ToString(),
										drReport[GeneralReportColumns.ReportArchName].ToString(),
										Convert.ToBoolean(drReport[GeneralReportColumns.Temporary]));
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
							MailGlobalErr(String.Format("Отчет с кодом {0} не существует.", GeneralReportID));

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
