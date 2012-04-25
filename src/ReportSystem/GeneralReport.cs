using System;
using System.Data;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
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
		public const string Temporary = "Temporary";
		public const string Format = "Format";
	}

	/// <summary>
	/// Summary description for GeneralReport.
	/// </summary>
	public class GeneralReport
	{
		public ulong GeneralReportID;
		public uint SupplierId;

		private uint? _contactGroupId;
		private string _eMailSubject;

		private string _reportFileName;
		private string _reportArchName;

		private bool _noArchive;

		//отчет является разовым?
		private bool _temporary;

		private MySqlConnection _conn;

		private string _directoryName;
		private string _mainFileName;

		private ReportFormats Format;

		public string _payer;

		private ILog Logger;

		//таблица отчетов, которая существует в общем отчете
		DataTable _dtReports;

		//таблица контактов, по которым надо отправить отчет
		DataTable _dtContacts;

		public List<BaseReport> Reports = new List<BaseReport>();

		// Проверка спика отчетов
		private void CheckReports()
		{
			foreach (DataRow drGReport1 in _dtReports.Rows) // Проверяем чтобы не было
				foreach (DataRow drGReport2 in _dtReports.Rows)  // двух листов с одинаковыми названиями
					if(Convert.ToBoolean(drGReport1[BaseReportColumns.colEnabled]) &&
						Convert.ToBoolean(drGReport2[BaseReportColumns.colEnabled]) &&
						Convert.ToUInt32(drGReport1[BaseReportColumns.colReportCode]) != 
							Convert.ToUInt32(drGReport2[BaseReportColumns.colReportCode]) &&
						Convert.ToString(drGReport1[BaseReportColumns.colReportCaption]) ==
							Convert.ToString(drGReport2[BaseReportColumns.colReportCaption]))
					{
						throw new ReportException(
							String.Format("В отчете {0} содержатся листы с одинаковым названием {1}.",
								GeneralReportID, drGReport1[BaseReportColumns.colReportCaption]));
					}
		}

		public GeneralReport() // конструктор для возможности тестирования
		{}

		public GeneralReport(bool noArchive) // конструктор для возможности тестирования
		{
			_noArchive = noArchive;
		}

		public GeneralReport(ulong id, uint supplierId, uint? ContactGroupId, 
			string EMailSubject, MySqlConnection Conn, string ReportFileName, 
			string ReportArchName, bool Temporary, ReportFormats format,
			IReportPropertiesLoader propertiesLoader, bool Interval, DateTime dtFrom, DateTime dtTo, string payer, bool noArchive)
		{
			Logger = LogManager.GetLogger(GetType());
			GeneralReportID = id;
			SupplierId = supplierId;
			_conn = Conn;
			_contactGroupId = ContactGroupId;
			_eMailSubject = EMailSubject;
			_reportFileName = ReportFileName;
			_reportArchName = ReportArchName;
			_temporary = Temporary;
			_payer = payer;
			_noArchive = noArchive;
			Format = format;

			ulong contactsCode = 0;

			_dtReports = MethodTemplate.ExecuteMethod(new ExecuteArgs(), GetReports, null, _conn);

			if (!Interval)
				_dtContacts = MethodTemplate.ExecuteMethod(new ExecuteArgs(), delegate(ExecuteArgs args)
				{
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
					null, _conn);
			else
			{
				_dtContacts = MethodTemplate.ExecuteMethod(new ExecuteArgs(), delegate(ExecuteArgs args)
				{
				args.DataAdapter.SelectCommand.CommandText = @"
select Mail FROM reports.Mailing_Addresses M
where GeneralReport = ?GeneralReport;";
				args.DataAdapter.SelectCommand.Parameters.AddWithValue("?GeneralReport", this.GeneralReportID);
				var res = new DataTable();
				args.DataAdapter.Fill(res);
				return res;
				}, null, _conn);
			}
			if ((_dtReports != null) && (_dtReports.Rows.Count > 0))
			{
				CheckReports(); // Проверяем отчеты, если что-то не нравится выдаем исключение
				foreach (DataRow drGReport in _dtReports.Rows)
				{
					if (Convert.ToBoolean(drGReport[BaseReportColumns.colEnabled]))
					{
						//Создаем отчеты и добавляем их в список отчетов
						var bs = (BaseReport)Activator.CreateInstance(
							GetReportTypeByName(drGReport[BaseReportColumns.colReportClassName].ToString()),
							new object[] { (ulong)drGReport[BaseReportColumns.colReportCode], 
								drGReport[BaseReportColumns.colReportCaption].ToString(), _conn, 
								Temporary, Format,
								propertiesLoader.LoadProperties(_conn, (ulong)drGReport[BaseReportColumns.colReportCode])});
						bs._Interval = Interval;
						bs._dtFrom = dtFrom;
						bs._dtTo = dtTo;
						Reports.Add(bs);

						//Если у общего отчета не выставлена тема письма, то берем ее у первого попавшегося отчета
						if (String.IsNullOrEmpty(_eMailSubject) && !String.IsNullOrEmpty(drGReport[BaseReportColumns.colAlternateSubject].ToString()))
							_eMailSubject = drGReport[BaseReportColumns.colAlternateSubject].ToString();
					}
				}
			}
			else
				throw new ReportException("У комбинированного отчета нет дочерних отчетов.");
		}

		//Производится построение отчетов
		public void ProcessReports()
		{
			var resFileName = BuildResultFile();

			var mails = _dtContacts.AsEnumerable().Select(r => r[0].ToString()).ToArray();
#if TESTING
			mails = new[] {Settings.Default.ErrorReportMail};
#endif
			foreach (var mail in mails)
				MailWithAttach(resFileName, mail);

			//Написать удаление записей из таблицы !!
			MethodTemplate.ExecuteMethod(new ExecuteArgs(), delegate(ExecuteArgs args)
																{
																	//args.DataAdapter.DeleteCommand = new MySqlCommand();
																	args.DataAdapter.SelectCommand.CommandText =
																		"delete FROM reports.Mailing_Addresses";
																	args.DataAdapter.SelectCommand.ExecuteNonQuery();
																	return new DataTable();
																}, null, _conn);

			if (Directory.Exists(_directoryName))
				Directory.Delete(_directoryName, true);
		}

		public string BuildResultFile()
		{
			_directoryName = Path.GetTempPath() + "Rep" + GeneralReportID.ToString();
			if (Directory.Exists(_directoryName))
				Directory.Delete(_directoryName, true);
			Directory.CreateDirectory(_directoryName);

			_mainFileName = _directoryName + "\\" +
				((String.IsNullOrEmpty(_reportFileName)) ? ("Rep" + GeneralReportID.ToString() + ".xls") : _reportFileName);

			bool emptyReport = true;
			while (Reports.Count > 0)
			{
				var bs = Reports.First();
				try
				{
					Reports.Remove(bs);
					bs.ReadReportParams();
					bs.ProcessReport();
					bs.ReportToFile(_mainFileName);
					bs.ToLog(GeneralReportID); // логируем успешное выполнение отчета
					emptyReport = false;
					foreach (var file in bs.AdditionalFiles.Keys)
					{
						var source = bs.AdditionalFiles[file];
						if (File.Exists(source))
							File.Copy(source, Path.Combine(_directoryName, file), true);
					}
				}
				catch (Exception ex)
				{
					bs.ToLog(GeneralReportID, ex.ToString()); // логируем ошибку при выполнении отчета
					if (ex is ReportException)
					{
						// уведомление об ошибке при формировании одного из подотчетов
						Mailer.MailReportErr(ex.ToString(), _payer, GeneralReportID, bs.ReportCode);
						continue; // выполняем следующий отчет
					}
					throw; // передаем наверх
				}
			}

			if (emptyReport) throw new ReportException("Отчет пуст.");

			if (_noArchive)
			{
				PrepareFtpDirectory();
				CopyFileToFtp(_mainFileName, Path.GetFileName(_mainFileName));
				return _mainFileName;
			}

			var resFileName = ArchFile();
			return resFileName;
		}

		private void MailWithAttach(string archFileName, string EMailAddress)
		{ 
			var message = new Mime(); 
			var mainEntry = message.MainEntity; 

			mainEntry.From = new AddressList {new MailboxAddress("АК Инфорум", "report@analit.net")};

			mainEntry.To = new AddressList();
			mainEntry.To.Parse(EMailAddress); 

			mainEntry.Subject = _eMailSubject; 

			mainEntry.ContentType = MediaType_enum.Multipart_mixed;

			var textEntity = mainEntry.ChildEntities.Add();
			textEntity.ContentType = MediaType_enum.Text_plain;
			textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
			textEntity.DataText = String.Empty; 

			var attachmentEntity = mainEntry.ChildEntities.Add(); 
			attachmentEntity.ContentType = MediaType_enum.Application_octet_stream; 
			attachmentEntity.ContentDisposition = ContentDisposition_enum.Attachment; 
			attachmentEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64; 
			attachmentEntity.ContentDisposition_FileName = Path.GetFileName(archFileName); 
			attachmentEntity.DataFromFile(archFileName);

			int? SMTPID = SmtpClientEx.QuickSendSmartHostSMTPID(Settings.Default.SMTPHost, null, null, message);

#if (!TESTING)
			MethodTemplate.ExecuteMethod<ProcessLogArgs, int>(new ProcessLogArgs(SMTPID, message.MainEntity.MessageID, EMailAddress), ProcessLog, 0, _conn, true, false, null);
#endif
		}

		class ProcessLogArgs : ExecuteArgs
		{
			public int? SmtpID;
			public string MessageID;
			public string EMail;

			public ProcessLogArgs(int? smtpID, string messageID, string eMail)
			{
				SmtpID = smtpID;
				MessageID = messageID;
				EMail = eMail;
			}
		}

		private int ProcessLog(ProcessLogArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = @"insert into logs.reportslogs 
(LogTime, GeneralReportCode, SMTPID, MessageID, EMail) 
values (NOW(), ?GeneralReportCode, ?SMTPID, ?MessageID, ?EMail)";
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?GeneralReportCode", GeneralReportID);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SMTPID", e.SmtpID);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?MessageID", e.MessageID);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?EMail", e.EMail);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			return 0;
		}

		private string GetResDirPath()
		{
			return Settings.Default.FTPOptBoxPath + SupplierId.ToString("000") + "\\Reports\\";
		}

		private void PrepareFtpDirectory()
		{
			var resDirPath = GetResDirPath();

			if (!(Directory.Exists(resDirPath)))
				Directory.CreateDirectory(resDirPath);

			foreach (string file in Directory.GetFiles(resDirPath))
				File.Delete(file);
		}

		private void CopyFileToFtp(string fromfile, string toFile)
		{
			try
			{
				var resDirPath = GetResDirPath();
				File.Copy(fromfile, resDirPath + toFile);
			}
			catch(Exception ex)
			{
				Logger.Error("Ошибка при копировании архива с отчетом", ex);
			}
		}

		private string ArchFile()
		{
			var resArchFileName = (String.IsNullOrEmpty(_reportArchName)) ? Path.ChangeExtension(Path.GetFileName(_mainFileName), ".zip") : _reportArchName;

			PrepareFtpDirectory();

			var zip = new FastZip();
			var tempArchive = Path.GetTempFileName();
			zip.CreateZip(tempArchive, _directoryName, false, null, null);
			var archive = Path.Combine(_directoryName, resArchFileName);
			File.Move(tempArchive, archive);

			CopyFileToFtp(archive, resArchFileName);

			return archive;
		}

		//Выбираем отчеты из базы
		private DataTable GetReports(ExecuteArgs e)
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

		private Type GetReportTypeByName(string ReportTypeClassName)
		{
			var t = Type.GetType(ReportTypeClassName);
			if (t == null)
				throw new ReportException(String.Format("Неизвестный тип отчета : {0}", ReportTypeClassName));
			return t;
		}
	}
}
