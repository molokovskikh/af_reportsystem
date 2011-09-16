using System;
using System.Net.Mail;
using Inforoom.ReportSystem.Properties;
using log4net;

namespace Inforoom.ReportSystem
{
	public class Mailer
	{
		private static ILog _log = LogManager.GetLogger(typeof(Mailer));
		//Вспомогательная функция отправки письма
		private static void Mail(string from, string messageTo, string subject, string body)
		{
			try
			{
				var message = new MailMessage(from, messageTo, subject, body);
				var client = new SmtpClient(Settings.Default.SMTPHost);
				message.IsBodyHtml = false;
				message.BodyEncoding = System.Text.Encoding.UTF8;
				client.Send(message);
			}
			catch (Exception e)
			{
				_log.Error("Ошибка при отправке уведомления", e);
			}
		}

		//Сообщение о глобальной ошибке, возникшей в результате работы программы
		public static void MailGlobalErr(string errDesc)
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
		public static void MailGeneralReportErr(string errDesc, string shortName, ulong generalReportCode)
		{
			Mail(Settings.Default.ErrorFrom, Settings.Default.ErrorReportMail, "Ошибка при запуске отчетa для " + shortName,
				String.Format("Код отчета : {0}\r\nОшибка : {1}", generalReportCode, errDesc));
		}
	}
}
