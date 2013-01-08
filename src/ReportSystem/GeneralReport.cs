﻿using System;
using System.Data;
using System.Linq;
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
	//Содержит названия полей, используемых при создании общего очета
	public sealed class GeneralReportColumns
	{
		public const string GeneralReportCode = "GeneralReportCode";
		public const string FirmCode = "FirmCode";
		public const string Allow = "Allow";
		public const string ContactGroupId = "ContactGroupId";
		public const string EMailSubject = "EMailSubject";
		public const string ShortName = "ShortName";
		public const string ReportFileName = "ReportFileName";
		public const string ReportArchName = "ReportArchName";
		public const string NoArchive = "NoArchive";
		public const string SendDescriptionFile = "SendDescriptionFile";
		public const string Temporary = "Temporary";
		public const string Format = "Format";
	}

	/// <summary>
	/// Summary description for GeneralReport.
	/// </summary>
	public class GeneralReport
	{
		public bool Testing;
		public List<Mime> Messages = new List<Mime>();

		public ulong GeneralReportID;
		public uint? SupplierId;

		private uint? _contactGroupId;
		public string EMailSubject;

		private string _reportFileName;
		private string _reportArchName;

		private string _directoryName;
		private string _mainFileName;

		private ReportFormats Format;

		public string _payer;

		public ILog Logger;

		public MySqlConnection Connection;
		public bool NoArchive;
		public bool SendDescriptionFile;

		public IDictionary<string, string> FilesForReport;

		//таблица отчетов, которая существует в общем отчете

		protected DataTable _reports;

		//таблица контактов, по которым надо отправить отчет
		public DataTable Contacts;

		public List<BaseReport> Reports = new List<BaseReport>();

		// Проверка спика отчетов
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
								GeneralReportID, drGReport1[BaseReportColumns.colReportCaption]));
					}
		}

		public GeneralReport() // конструктор для возможности тестирования
		{
			FilesForReport = new Dictionary<string, string>();
		}

		public GeneralReport(bool noArchive) // конструктор для возможности тестирования
		{
			FilesForReport = new Dictionary<string, string>();
			NoArchive = noArchive;
		}

		public GeneralReport(ulong id, uint? supplierId, uint? contactGroupId,
			string emailSubject,
			MySqlConnection connection,
			string reportFileName,
			string reportArchName,
			ReportFormats format,
			IReportPropertiesLoader propertiesLoader,
			bool interval,
			DateTime dtFrom,
			DateTime dtTo,
			string payer,
			bool noArchive,
			bool sendDescriptionFile)
		{
			Logger = LogManager.GetLogger(GetType());
			GeneralReportID = id;
			SupplierId = supplierId;
			Connection = connection;
			_contactGroupId = contactGroupId;
			EMailSubject = emailSubject;
			_reportFileName = reportFileName;
			_reportArchName = reportArchName;
			_payer = payer;
			NoArchive = noArchive;
			SendDescriptionFile = sendDescriptionFile;
			Format = format;

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
					args.DataAdapter.SelectCommand.Parameters.AddWithValue("?ContactGroupId", _contactGroupId);
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
					args.DataAdapter.SelectCommand.Parameters.AddWithValue("?GeneralReport", this.GeneralReportID);
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
								propertiesLoader.LoadProperties(Connection, (ulong)drGReport[BaseReportColumns.colReportCode])
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
		public void ProcessReports(ReportExecuteLog log)
		{
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
				new MySqlParameter("id", GeneralReportID));
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
			_directoryName = Path.GetTempPath() + "Rep" + GeneralReportID.ToString();
			if (Directory.Exists(_directoryName))
				Directory.Delete(_directoryName, true);
			Directory.CreateDirectory(_directoryName);

			_mainFileName = _directoryName + "\\" +
				((String.IsNullOrEmpty(_reportFileName)) ? ("Rep" + GeneralReportID.ToString() + ".xls") : _reportFileName);

			bool emptyReport = true;
			while (Reports.Count > 0) {
				var bs = Reports.First();
				try {
					Reports.Remove(bs);
					bs.ReadReportParams();
					bs.ProcessReport();
					bs.ReportToFile(_mainFileName);
					bs.ToLog(GeneralReportID); // логируем успешное выполнение отчета
					emptyReport = false;
				}
				catch (Exception ex) {
					bs.ToLog(GeneralReportID, ex.ToString()); // логируем ошибку при выполнении отчета
					if (ex is ReportException) {
						// уведомление об ошибке при формировании одного из подотчетов
						Mailer.MailReportErr(ex.ToString(), _payer, GeneralReportID, bs.ReportCode, bs.ReportCaption);
						continue; // выполняем следующий отчет
					}
					throw new ReportException(ex.Message, ex, bs.ReportCode, bs.ReportCaption, _payer); // передаем наверх
				}
			}

			foreach (var file in FilesForReport.Keys) {
				var source = FilesForReport[file];
				if (File.Exists(source))
					File.Copy(source, Path.Combine(_directoryName, file), true);
			}

			if (emptyReport) throw new ReportException("Отчет пуст.");

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
			parameters.AddWithValue("?GeneralReportCode", GeneralReportID);
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
			var resArchFileName = (String.IsNullOrEmpty(_reportArchName)) ? Path.ChangeExtension(Path.GetFileName(_mainFileName), ".zip") : _reportArchName;

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
			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select
  *
from
  reports.Reports r,
  reports.reporttypes rt
where
	r.{0} = ?{0}
and rt.ReportTypeCode = r.ReportTypeCode",
				GeneralReportColumns.GeneralReportCode);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?" + GeneralReportColumns.GeneralReportCode, GeneralReportID);
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
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ReportCode", GeneralReportID);
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
								Logger.Error(string.Format("При формаровании отчета {0} не был добавлен файл {1} с описанием, так как файл с таким именем уже существует", GeneralReportID, key));
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
	}
}
