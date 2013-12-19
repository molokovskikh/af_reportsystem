using System;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Common.Web.Ui.ActiveRecordExtentions;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.ReportSystem.Model;
using log4net;
using LumiSoft.Net.SMTP.Client;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using ExecuteTemplate;
using System.IO;
using LumiSoft.Net.Mime;
using Inforoom.ReportSystem.Properties;

namespace Inforoom.ReportSystem
{
	[ActiveRecord("general_reports", Schema = "reports")]
	public class GeneralReport
	{
		private string _directoryName;
		private string _mainFileName;

		//таблица отчетов, которая существует в общем отчете
		protected DataTable _reports;

		public bool Testing;
		public List<Mime> Messages = new List<Mime>();

		public ILog Logger;

		public MySqlConnection Connection;

		public IDictionary<string, string> FilesForReport;

		//таблица контактов, по которым надо отправить отчет
		public DataTable Contacts;

		public List<BaseReport> Reports = new List<BaseReport>();

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

		[Property(ColumnType = "NHibernate.Type.EnumStringType`1[[Inforoom.ReportSystem.ReportFormats, ReportSystem]], NHibernate")]
		public virtual ReportFormats Format { get; set; }

		[Property]
		public virtual uint? ContactGroupId { get; set; }

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
			var loader = new ReportPropertiesLoader();

			_reports = MethodTemplate.ExecuteMethod(new ExecuteArgs(), GetReports, null, Connection);

			FilesForReport = MethodTemplate.ExecuteMethod(new ExecuteArgs(), GetFilesForReports, null, Connection);

			if (!interval)
				Contacts = MethodTemplate.ExecuteMethod(new ExecuteArgs(), delegate(ExecuteArgs args) {
					args.DataAdapter.SelectCommand.CommandText = @"
select lower(c.contactText)
from
  contacts.contact_groups cg
  join contacts.contacts c on cg.Id = c.ContactOwnerId
where
	cg.Id = ?ContactGroupId
and cg.Type = ?ContactGroupType
and c.Type = ?ContactType
union
select lower(c.contactText)
from
  contacts.contact_groups cg
  join contacts.persons p on cg.id = p.ContactGroupId
  join contacts.contacts c on p.Id = c.ContactOwnerId
where
	cg.Id = ?ContactGroupId
and cg.Type = ?ContactGroupType
and c.Type = ?ContactType";
					args.DataAdapter.SelectCommand.Parameters.AddWithValue("?ContactGroupId", ContactGroupId);
					args.DataAdapter.SelectCommand.Parameters.AddWithValue("?ContactGroupType", 6);
					args.DataAdapter.SelectCommand.Parameters.AddWithValue("?ContactType", 0);
					DataTable res = new DataTable();
					args.DataAdapter.Fill(res);
					return res;
				},
					null, Connection);
			else {
				Contacts = MethodTemplate.ExecuteMethod(new ExecuteArgs(), delegate(ExecuteArgs args) {
					args.DataAdapter.SelectCommand.CommandText = @"
select Mail FROM reports.Mailing_Addresses M
where GeneralReport = ?GeneralReport;";
					args.DataAdapter.SelectCommand.Parameters.AddWithValue("?GeneralReport", Id);
					var res = new DataTable();
					args.DataAdapter.Fill(res);
					return res;
				}, null, Connection);
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
								loader.LoadProperties(Connection, (ulong)drGReport[BaseReportColumns.colReportCode])
							});
						bs.Interval = interval;
						bs.From = dtFrom;
						bs.To = dtTo;
						Reports.Add(bs);

						//Если у общего отчета не выставлена тема письма, то берем ее у первого попавшегося отчета
						if (String.IsNullOrEmpty(EMailSubject) && !String.IsNullOrEmpty(drGReport[BaseReportColumns.colAlternateSubject].ToString()))
							EMailSubject = drGReport[BaseReportColumns.colAlternateSubject].ToString();
					}
				}
			}
			else
				throw new ReportException("У комбинированного отчета нет дочерних отчетов.");
		}

		//Производится построение отчетов
		public void ProcessReports(ReportExecuteLog log, MySqlConnection connection, bool interval, DateTime begin, DateTime end)
		{
			Connection = connection;
			Load(interval, begin, end);
			try {
				var files = BuildResultFile();
				SendReport(files, log);
				Historify(files, log);
			}
			finally {
				Clean();
			}
		}

		public void LogSuccess()
		{
			MySqlHelper.ExecuteScalar(Connection,
				"update Reports.general_reports set LastSuccess = now() where GeneralReportCode = ?id",
				new MySqlParameter("id", Id));
		}

		public void SendReport(string[] files, ReportExecuteLog log)
		{
			var mails = Contacts.AsEnumerable().Select(r => r[0].ToString()).ToArray();
#if TESTING
			mails = new[] { Settings.Default.ErrorReportMail };
#endif
			foreach (var mail in mails)
				MailWithAttach(log, mail, files);

			//Написать удаление записей из таблицы !!
			MethodTemplate.ExecuteMethod(new ExecuteArgs(), delegate(ExecuteArgs args) {
				args.DataAdapter.SelectCommand.CommandText =
					"delete FROM reports.Mailing_Addresses";
				args.DataAdapter.SelectCommand.ExecuteNonQuery();
				return new DataTable();
			}, null, Connection);
		}

		private void Clean()
		{
			if (Directory.Exists(_directoryName))
				Directory.Delete(_directoryName, true);
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
				WithTempArchive(_directoryName, f => File.Copy(f, historyFile));
			}
		}

		public string[] BuildResultFile()
		{
			_directoryName = Path.GetTempPath() + "Rep" + Id;
			if (Directory.Exists(_directoryName))
				Directory.Delete(_directoryName, true);
			Directory.CreateDirectory(_directoryName);

			_mainFileName = _directoryName + "\\" +
				((String.IsNullOrEmpty(ReportFileName)) ? ("Rep" + Id + ".xls") : ReportFileName);

			bool emptyReport = true;
			while (Reports.Count > 0) {
				var bs = Reports.First();
				try {
					Reports.Remove(bs);
					using (new SessionScope()) {
						ArHelper.WithSession(s => {
							bs.Session = s;
							bs.ReadReportParams();
							bs.ProcessReport();
						});
					}
					bs.ReportToFile(_mainFileName);
					bs.ToLog(Id); // протоколируем успешное выполнение отчета
					emptyReport = false;
				}
				catch (Exception ex) {
					bs.ToLog(Id, ex.ToString()); // протоколируем ошибку при выполнении отчета
					if (ex is ReportException) {
						// уведомление об ошибке при формировании одного из подотчетов
						Mailer.MailReportErr(ex.ToString(), Payer.Name, Id, bs.ReportCode, bs.ReportCaption);
						continue; // выполняем следующий отчет
					}
					throw new ReportException(ex.Message, ex, bs.ReportCode, bs.ReportCaption, Payer.Name); // передаем наверх
				}
			}

			foreach (var file in FilesForReport.Keys) {
				var source = FilesForReport[file];
				if (File.Exists(source))
					File.Copy(source, Path.Combine(_directoryName, file), true);
			}

			if (emptyReport)
				throw new ReportException("Отчет пуст.");

			if (NoArchive) {
				SafeCopyFileToFtp(_mainFileName, Path.GetFileName(_mainFileName));
				return Directory.GetFiles(Path.GetDirectoryName(_mainFileName));
			}

			return new[] { ArchFile() };
		}

		private void MailWithAttach(ReportExecuteLog log, string address, string[] files)
		{
			var message = new Mime();
			var mainEntry = message.MainEntity;

			mainEntry.From = new AddressList { new MailboxAddress("АК Инфорум", "report@analit.net") };

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
				MethodTemplate.ExecuteMethod(new ExecuteArgs(),
					e => ProcessLog(e, smtpId, message.MainEntity.MessageID, address, log),
					0, Connection, true, false, null);
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

		private int ProcessLog(ExecuteArgs e, int? smtpId, string messageId, string email, ReportExecuteLog log)
		{
			e.DataAdapter.SelectCommand.CommandText = @"insert into logs.reportslogs
(LogTime, GeneralReportCode, SMTPID, MessageID, EMail, ResultId)
values (NOW(), ?GeneralReportCode, ?SMTPID, ?MessageID, ?EMail, ?ResultId)";
			var parameters = e.DataAdapter.SelectCommand.Parameters;
			parameters.AddWithValue("?GeneralReportCode", Id);
			parameters.AddWithValue("?SMTPID", smtpId);
			parameters.AddWithValue("?MessageID", messageId);
			parameters.AddWithValue("?EMail", email);
			parameters.AddWithValue("?ResultId", log.Id);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			return 0;
		}

		private string GetResDirPath()
		{
			if (SupplierId == null)
				return null;
			return Settings.Default.FTPOptBoxPath + SupplierId.Value.ToString("000") + "\\Reports\\";
		}

		private string PrepareFtpDirectory()
		{
			var resDirPath = GetResDirPath();

			if (!String.IsNullOrEmpty(resDirPath)) {
				if (!(Directory.Exists(resDirPath)))
					Directory.CreateDirectory(resDirPath);
			}

			return resDirPath;
		}

		private void SafeCopyFileToFtp(string fromfile, string toFile)
		{
			try {
				CopyFileToFtp(fromfile, toFile);
			}
			catch (Exception ex) {
				Logger.Error("Ошибка при копировании архива с отчетом", ex);
			}
		}

		public void CopyFileToFtp(string fromfile, string toFile)
		{
			var resDirPath = PrepareFtpDirectory();
			if (!String.IsNullOrEmpty(resDirPath))
				File.Copy(fromfile, Path.Combine(resDirPath, toFile), true);
		}

		private string ArchFile()
		{
			var resArchFileName = (String.IsNullOrEmpty(ReportArchName)) ? Path.ChangeExtension(Path.GetFileName(_mainFileName), ".zip") : ReportArchName;

			var archive = Path.Combine(_directoryName, resArchFileName);
			WithTempArchive(_directoryName, f => File.Move(f, archive));

			SafeCopyFileToFtp(archive, resArchFileName);

			return archive;
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
		public DataTable GetReports(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = @"
select
  *
from
  reports.Reports r,
  reports.reporttypes rt
where
	r.GeneralReportCode = ?GeneralReportCode
and rt.ReportTypeCode = r.ReportTypeCode";
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?GeneralReportCode", Id);
			var res = new DataTable();
			e.DataAdapter.Fill(res);
			return res;
		}

		public IDictionary<string, string> GetFilesForReports(ExecuteArgs e)
		{
			var result = new Dictionary<string, string>();
			e.DataAdapter.SelectCommand.CommandText = @"
SELECT * FROM reports.filessendwithreport f
where f.Report = ?ReportCode
and f.FileName is not null";
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ReportCode", Id);
			var res = new DataTable();
			e.DataAdapter.Fill(res);
			foreach (DataRow row in _reports.Rows) {
				if (SendDescriptionFile && Convert.ToBoolean(row["Enabled"])) {
					var reportCode = row[BaseReportColumns.colReportTypeCode];
					e.DataAdapter.SelectCommand.Parameters.Clear();
					e.DataAdapter.SelectCommand.CommandText = @"SELECT * FROM reports.fileforreporttypes f
where ReportType = ?ReportTypeCode;";
					e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ReportTypeCode", reportCode);
					var dtFiles = new DataTable();
					e.DataAdapter.Fill(dtFiles);
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
