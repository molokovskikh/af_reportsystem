using System;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Models;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.ReportSystem.Model;
using log4net;
using LumiSoft.Net.SMTP.Client;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IO;
using LumiSoft.Net.Mime;
using Inforoom.ReportSystem.Properties;
using Common.MySql;
using NHibernate;

namespace Inforoom.ReportSystem
{
	[ActiveRecord("general_reports", Schema = "reports")]
	public class GeneralReport
	{
		public static ISessionFactory Factory;
		public string[] Contacts = new string[0];

		private string _mainFileName;

		//таблица отчетов, которая существует в общем отчете
		protected DataTable _reports;

		public bool Testing;
		public List<Mime> Messages = new List<Mime>();

		public ILog Logger;

		public MySqlConnection Connection;

		public IDictionary<string, string> FilesForReport = new Dictionary<string, string>();

		//таблица контактов, по которым надо отправить отчет
		public Queue<BaseReport> Reports = new Queue<BaseReport>();
		public string WorkDir;

		public GeneralReport() // конструктор для возможности тестирования
		{
			FilesForReport = new Dictionary<string, string>();
			Logger = LogManager.GetLogger(GetType());
		}

		[PrimaryKey("GeneralReportCode")]
		public virtual uint Id { get; set; }

		[BelongsTo("PayerId")]
		public virtual Payer Payer { get; set; }

		[Property("FirmCode")]
		public virtual uint? SupplierId { get; set; }

		[Property("Allow")]
		public virtual bool Enabled { get; set; }

		[Property]
		public virtual string ReportFileName { get; set; }

		[Property]
		public virtual string ReportArchName { get; set; }

		[Property]
		public virtual string EMailSubject { get; set; }

		[Property]
		public virtual bool NoArchive { get; set; }

		[Property]
		public virtual bool SendDescriptionFile { get; set; }

		[Property(ColumnType = "NHibernate.Type.EnumStringType`1[[Inforoom.ReportSystem.ReportFormats, ReportSystem.Lib]], NHibernate")]
		public virtual ReportFormats Format { get; set; }

		[Property]
		public virtual bool MailPerFile { get; set; }

		[BelongsTo("ContactGroupId")]
		public virtual ContactGroup ContactGroup { get; set; }

		[BelongsTo("PublicSubscriptionsId")]
		public virtual ContactGroup PublicSubscriptions { get; set; }

		// Проверка списка отчетов
		private void CheckReports()
		{
			foreach (DataRow drGReport1 in _reports.Rows) // Проверяем чтобы не было
				foreach (DataRow drGReport2 in _reports.Rows) // двух листов с одинаковыми названиями
					if (Convert.ToBoolean(drGReport1[BaseReportColumns.colEnabled]) &&
						Convert.ToBoolean(drGReport2[BaseReportColumns.colEnabled]) &&
						Convert.ToUInt32(drGReport1[BaseReportColumns.colReportCode]) !=
							Convert.ToUInt32(drGReport2[BaseReportColumns.colReportCode]) &&
						Convert.ToString(drGReport1[BaseReportColumns.colReportCaption]) ==
							Convert.ToString(drGReport2[BaseReportColumns.colReportCaption])) {
						throw new ReportException(
							String.Format("В отчете {0} содержатся листы с одинаковым названием {1}.",
								Id, drGReport1[BaseReportColumns.colReportCaption]));
					}
		}

		private void Load(bool interval, DateTime dtFrom, DateTime dtTo)
		{
			_reports = GetReports();
			FilesForReport = GetFilesForReports();

			if (!interval) {
				CollectContacts();
			}
			else {
				Contacts = Connection.Fill(@"
select Mail FROM reports.Mailing_Addresses M
where GeneralReport = ?GeneralReport;", new { GeneralReport = Id })
					.AsEnumerable().Select(r => r[0].ToString())
					.ToArray();
			}
			if ((_reports != null) && (_reports.Rows.Count > 0)) {
				CheckReports(); // Проверяем отчеты, если что-то не нравится выдаем исключение
				foreach (DataRow drGReport in _reports.Rows) {
					if (Convert.ToBoolean(drGReport[BaseReportColumns.colEnabled])) {
						//Создаем отчеты и добавляем их в список отчетов
						var bs = (BaseReport)Activator.CreateInstance(
							GetReportTypeByName(drGReport[BaseReportColumns.colReportClassName].ToString()),
							new object[] {
								(ulong)drGReport[BaseReportColumns.colReportCode],
								drGReport[BaseReportColumns.colReportCaption].ToString(), Connection,
								Format,
								LoadProperties(Connection, (ulong)drGReport[BaseReportColumns.colReportCode])
							});
						bs.Interval = interval;
						bs.From = dtFrom;
						bs.To = dtTo;
						Reports.Enqueue(bs);

						//Если у общего отчета не выставлена тема письма, то берем ее у первого попавшегося отчета
						if (String.IsNullOrEmpty(EMailSubject) && !String.IsNullOrEmpty(drGReport[BaseReportColumns.colAlternateSubject].ToString()))
							EMailSubject = drGReport[BaseReportColumns.colAlternateSubject].ToString();
					}
				}
			}
			else
				throw new ReportException("У комбинированного отчета нет дочерних отчетов.");
		}

		public static DataSet LoadProperties(MySqlConnection conn, ulong reportCode)
		{
			var reportcode = reportCode;
			var ds = new DataSet();

			var adapter = new MySqlDataAdapter("", conn);
			adapter.SelectCommand.CommandText = String.Format(@"
select
  *
from
  reports.Report_Properties rp,
  reports.report_type_properties rtp
where
    rp.{0} = ?{0}
and rtp.ID = rp.PropertyID", BaseReportColumns.colReportCode);
			adapter.SelectCommand.Parameters.Clear();
			adapter.SelectCommand.Parameters.AddWithValue("?" + BaseReportColumns.colReportCode, reportcode);
			DataTable res = new DataTable("ReportProperties");
			adapter.Fill(res);
			ds.Tables.Add(res);

			adapter.SelectCommand.CommandText = String.Format(@"
select
  rpv.*
from
  reports.Report_Properties rp,
  reports.report_property_values rpv
where
    rp.{0} = ?{0}
and rpv.ReportPropertyID = rp.ID", BaseReportColumns.colReportCode);
			adapter.SelectCommand.Parameters.Clear();
			adapter.SelectCommand.Parameters.AddWithValue("?" + BaseReportColumns.colReportCode, reportcode);
			res = new DataTable("ReportPropertyValues");
			adapter.Fill(res);
			ds.Tables.Add(res);

			return ds;
		}

		public void CollectContacts()
		{
			var contacts = new string[0];
			if (ContactGroup != null) {
				contacts = contacts.Concat(ContactGroup.Contacts
					.Concat(ContactGroup.Persons.SelectMany(p => p.Contacts))
					.Where(c => c.Type == ContactType.Email).Select(c => c.ContactText))
					.ToArray();
			}
			if (PublicSubscriptions != null) {
				contacts = contacts.Concat(PublicSubscriptions.Contacts
					.Concat(PublicSubscriptions.Persons.SelectMany(p => p.Contacts))
					.Where(c => c.Type == ContactType.Email).Select(c => c.ContactText))
					.ToArray();
			}
			Contacts = contacts.Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
		}

		//Производится построение отчетов
		public void ProcessReports(ReportExecuteLog log, MySqlConnection connection, bool interval, DateTime begin, DateTime end, bool load = true)
		{
			Connection = connection;
			if (load)
				Load(interval, begin, end);
			try {
				var files = ArchFile(BuildResultFile());
				SafeCopyFileToFtp(files);
				SendReport(files, log);
				Historify(files, log);
			}
			finally {
				Clean();
			}
		}

		public void LogSuccess()
		{
			MySql.Data.MySqlClient.MySqlHelper.ExecuteScalar(Connection,
				"update Reports.general_reports set LastSuccess = now() where GeneralReportCode = ?id",
				new MySqlParameter("id", Id));
		}

		public void SendReport(string[] files, ReportExecuteLog log)
		{
			var mails = Contacts;
#if TESTING
			mails = new[] { Settings.Default.ErrorReportMail };
#endif
			if (MailPerFile) {
				foreach (var file in files) {
					foreach (var mail in mails)
						MailWithAttach(log, mail, new[] { file });
				}
			}
			else {
				foreach (var mail in mails)
					MailWithAttach(log, mail, files);
			}

			Connection.Execute("delete FROM reports.Mailing_Addresses where GeneralReport = ?GeneralReport",
				new { GeneralReport = Id });
		}

		private void Clean()
		{
			if (Directory.Exists(WorkDir))
				Directory.Delete(WorkDir, true);
		}

		private void Historify(string[] files, ReportExecuteLog log)
		{
			if (files.Length == 1) {
				var reportFile = files[0];
				var historyFile = Path.Combine(Settings.Default.HistoryPath, log.Id + Path.GetExtension(reportFile));
				File.Copy(reportFile, historyFile);
			}
			else {
				var historyFile = Path.Combine(Settings.Default.HistoryPath, log.Id + ".zip");
				WithTempArchive(WorkDir, f => File.Copy(f, historyFile));
			}
		}

		public string[] BuildResultFile()
		{
			WorkDir = Path.Combine(Path.GetTempPath(), "Rep" + Id);
			FileHelper.InitDir(WorkDir);
			_mainFileName = Path.Combine(WorkDir,
				String.IsNullOrEmpty(ReportFileName) ? "Rep" + Id + ".xls" : ReportFileName);

			//будь бдителен очередь используется тк после обработки память занятую отчетом нужно освободить
			bool emptyReport = true;
			while (Reports.Count > 0) {
				var report = Reports.Dequeue();
				try {
					using (new SessionScope()) {
						ArHelper.WithSession(s => {
							report.Session = s;
							report.Write(_mainFileName);
						});
					}
					report.ToLog(Id); // протоколируем успешное выполнение отчета
					foreach (var warning in report.Warnings) {
						Mailer.MailReportNotify(warning, Payer != null ? Payer.Name : "", Id, report.ReportCode);
					}
					emptyReport = false;
				}
				catch (Exception ex) {
					report.ToLog(Id, ex.ToString()); // протоколируем ошибку при выполнении отчета
					if (ex is ReportException) {
						// уведомление об ошибке при формировании одного из подотчетов
						Mailer.MailReportErr(ex.ToString(), Payer != null ? Payer.Name : "", Id, report.ReportCode, report.ReportCaption);
						continue; // выполняем следующий отчет
					}
					throw new ReportException(ex.Message, ex, report.ReportCode, report.ReportCaption, Payer != null ? Payer.Name : ""); // передаем наверх
				}
			}

			foreach (var file in FilesForReport.Keys) {
				var source = FilesForReport[file];
				if (File.Exists(source))
					File.Copy(source, Path.Combine(WorkDir, file), true);
			}

			if (emptyReport)
				throw new ReportException("Отчет пуст.");

			return Directory.GetFiles(WorkDir);
		}

		private void MailWithAttach(ReportExecuteLog log, string address, string[] files)
		{
			var message = new Mime();
			var mainEntry = message.MainEntity;

			mainEntry.From = new AddressList { new MailboxAddress("АналитФармация", "report@analit.net") };

			mainEntry.To = new AddressList();
			mainEntry.To.Parse(address);

			mainEntry.Subject = EMailSubject;

			mainEntry.ContentType = MediaType_enum.Multipart_mixed;

			var textEntity = mainEntry.ChildEntities.Add();
			textEntity.ContentType = MediaType_enum.Text_plain;
			textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
			textEntity.DataText = String.Empty;

			foreach (var file in files) {
				AttachFile(mainEntry, file);
			}

			if (Testing) {
				Messages.Add(message);
			}
			else {
				var smtpId = SmtpClientEx.QuickSendSmartHostSMTPID(Settings.Default.SMTPHost, null, null, message);
				ProcessLog(smtpId, message.MainEntity.MessageID, address, log);
			}
		}

		private void AttachFile(MimeEntity mainEntry, string file)
		{
			var entity = mainEntry.ChildEntities.Add();
			entity.ContentType = MediaType_enum.Application_octet_stream;
			entity.ContentDisposition = ContentDisposition_enum.Attachment;
			entity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
			entity.ContentDisposition_FileName = Path.GetFileName(file);
			entity.DataFromFile(file);
		}

		private void ProcessLog(int? smtpId, string messageId, string email, ReportExecuteLog log)
		{
			var adapter = new MySqlDataAdapter("", Connection);
			adapter.SelectCommand.CommandText = @"insert into logs.reportslogs
(LogTime, GeneralReportCode, SMTPID, MessageID, EMail, ResultId)
values (NOW(), ?GeneralReportCode, ?SMTPID, ?MessageID, ?EMail, ?ResultId)";
			var parameters = adapter.SelectCommand.Parameters;
			parameters.AddWithValue("?GeneralReportCode", Id);
			parameters.AddWithValue("?SMTPID", smtpId);
			parameters.AddWithValue("?MessageID", messageId);
			parameters.AddWithValue("?EMail", email);
			parameters.AddWithValue("?ResultId", log.Id);
			adapter.SelectCommand.ExecuteNonQuery();
		}

		private void SafeCopyFileToFtp(string[] files)
		{
			try {
				CopyFileToFtp(files);
			}
			catch (Exception ex) {
				Logger.Error("Ошибка при копировании архива с отчетом", ex);
			}
		}

		public void CopyFileToFtp(string[] files)
		{
			if (SupplierId == null)
				return;

			var dir = Path.Combine(Settings.Default.FTPOptBoxPath, SupplierId.Value.ToString("000"), "Reports");
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			foreach (var file in files) {
				File.Copy(file, Path.Combine(dir, Path.GetFileName(file)), true);
			}
		}

		public string[] ArchFile(string[] files)
		{
			if (NoArchive)
				return files;

			if (MailPerFile) {
				var result = new List<string>();
				foreach (var file in files) {
					var zipName = Path.ChangeExtension(file, ".zip");
					result.Add(zipName);
					using (var zip = ZipFile.Create(zipName)) {
						zip.BeginUpdate();
						zip.Add(file, Path.GetFileName(file));
						zip.CommitUpdate();
					}
				}
				return result.ToArray();
			} else {
				var archive = (String.IsNullOrEmpty(ReportArchName))
					? Path.ChangeExtension(Path.GetFileName(_mainFileName), ".zip")
					: ReportArchName;
				archive = Path.Combine(WorkDir, archive);
				WithTempArchive(WorkDir, f => File.Move(f, archive));
				return new[] { archive };
			}
		}

		public static void WithTempArchive(string dir, Action<string> action)
		{
			var zip = new FastZip();
			var tempArchive = Path.GetTempFileName();
			try {
				zip.CreateZip(tempArchive, dir, false, null, null);
				action(tempArchive);
			}
			finally {
				File.Delete(tempArchive);
			}
		}

		//Выбираем отчеты из базы
		public DataTable GetReports()
		{
			return Connection.Fill(@"
select
  *
from
  reports.Reports r,
  reports.reporttypes rt
where
	r.GeneralReportCode = ?GeneralReportCode
and rt.ReportTypeCode = r.ReportTypeCode", new { GeneralReportCode = Id });
		}

		public IDictionary<string, string> GetFilesForReports()
		{
			var result = new Dictionary<string, string>();
			var res = Connection.Fill(@"
SELECT * FROM reports.filessendwithreport f
where f.Report = ?ReportCode
and f.FileName is not null", new { ReportCode = Id });
			foreach (DataRow row in _reports.Rows) {
				if (SendDescriptionFile && Convert.ToBoolean(row["Enabled"])) {
					var reportCode = row[BaseReportColumns.colReportTypeCode];
					var dtFiles = Connection.Fill(@"SELECT * FROM reports.fileforreporttypes f
where ReportType = ?ReportTypeCode;", new { ReportTypeCode = reportCode });
					if (dtFiles.Rows.Count > 0) {
						var filePath = Path.Combine(Settings.Default.SavedFilesReportTypePath, dtFiles.Rows[0]["Id"].ToString());
						if (File.Exists(filePath)) {
							var key = dtFiles.Rows[0]["File"].ToString();
							if (!result.Keys.Contains(key))
								result.Add(key, filePath);
							else
								Logger.Error(string.Format("При формировании отчета {0} не был добавлен файл {1} с описанием, так как файл с таким именем уже существует", Id, key));
						}
					}
				}
			}

			foreach (DataRow row in res.Rows) {
				var file = Path.Combine(Settings.Default.SavedFilesPath, row["Id"].ToString());
				var fileName = row["FileName"].ToString();
				result.Add(fileName, file);
			}
			return result;
		}

		private Type GetReportTypeByName(string ReportTypeClassName)
		{
			var t = Type.GetType(ReportTypeClassName);
			if (t == null)
				throw new ReportException(String.Format("Неизвестный тип отчета : {0}", ReportTypeClassName));
			return t;
		}

		public override string ToString()
		{
			return Id.ToString();
		}
	}
}
