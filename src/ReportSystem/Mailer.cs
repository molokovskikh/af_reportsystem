using System;
using System.Net.Mail;
using Inforoom.ReportSystem.Properties;
using log4net;
using NHibernate.Properties;

namespace Inforoom.ReportSystem
{
	public class Mailer
	{
		private static ILog _log = LogManager.GetLogger(typeof(Mailer));
		//Вспомогательная функция отправки письма
		private static void Mail(string from, string messageTo, string subject, string body)
		{
			try {
				var message = new MailMessage(from, messageTo, subject, body);
				var client = new SmtpClient(Settings.Default.SMTPHost);
				message.IsBodyHtml = false;
				message.BodyEncoding = System.Text.Encoding.UTF8;
				client.Send(message);
			}
			catch (Exception e) {
				_log.Error("Ошибка при отправке уведомления", e);
			}
		}

		//Сообщение о глобальной ошибке, возникшей в результате работы программы
		public static void MailGlobalErr(Exception ex)
		{
			try {
				Mail(Settings.Default.ErrorFrom, Settings.Default.ErrorReportMail, "Ошибка при запуске программы отчетов",
					String.Format("Параметры запуска : {0}\r\nОшибка : {1}", String.Join("  ", Environment.GetCommandLineArgs()), ex));
			}
			catch (Exception e) {
				_log.Error("Ошибка при отправке уведомления", e);
			}
		}

		//Сообщение об ошибке, возникшей в результате построения общего отчета
		public static void MailGeneralReportErr(GeneralReport report, Exception ex)
		{
			if (report != null) {
				var subject = "Ошибка при запуске отчетa для ";
				if (report != null && report.Payer != null) {
					subject += report.Payer.Name;
				}
				Mail(Settings.Default.ErrorFrom, Settings.Default.ErrorReportMail, subject,
					String.Format("Код отчета : {0}\r\nОшибка : {1}", report.Id, ex));
			}
			else {
				MailGlobalErr(ex);
			}
		}

		//Сообщение об ошибке, возникшей в результате построения одного из отчетов (листов)
		public static void MailReportErr(string errDesc, string shortName, ulong generalReportCode, ulong reportCode, string reportCaption)
		{
			Mail(Settings.Default.ErrorFrom, Settings.Default.ErrorReportMail, "Ошибка при формировании одного из подотчетов для " + shortName,
				String.Format("Код отчета : {0}\r\nКод подотчета: {1}, имя: {3}\r\nПри формировании подотчета возникла ошибка : {2}", generalReportCode, reportCode, errDesc, reportCaption));
		}

		public static void MailReportNotify(string msg, string shortName, ulong generalReportCode, ulong reportCode)
		{
			Mail(Settings.Default.ErrorFrom, Settings.Default.ErrorReportMail, "Уведомление о событии при формировании отчета для " + shortName,
				String.Format("Код отчета : {0}\r\nКод подотчета: {1}\r\nУведомление : {2}", generalReportCode, reportCode, msg));
		}
	}
}