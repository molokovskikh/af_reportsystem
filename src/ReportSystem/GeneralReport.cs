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
		public const string Temporary = "Temporary";
		public const string Format = "Format";
	}

	/// <summary>
	/// Summary description for GeneralReport.
	/// </summary>
	public class GeneralReport
	{
		public ulong _generalReportID;
		public int _firmCode;



		private uint? _contactGroupId;
		private string _eMailSubject;

		private string _reportFileName;
		private string _reportArchName;

		//отчет является разовым?
		private bool _temporary;

		private MySqlConnection _conn;

		private string _directoryName;
		private string _mainFileName;

		private ReportFormats Format;

        private ILog Logger;

		//таблица отчетов, которая существует в общем отчете
		DataTable _dtReports;

		//таблица контактов, по которым надо отправить отчет
		DataTable _dtContacts;

		List<BaseReport> _reports;

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
								_generalReportID, drGReport1[BaseReportColumns.colReportCaption]));
					}
		}

		public GeneralReport(ulong GeneralReportID, int FirmCode, uint? ContactGroupId, 
			string EMailSubject, MySqlConnection Conn, string ReportFileName, 
			string ReportArchName, bool Temporary, ReportFormats format,
			IReportPropertiesLoader propertiesLoader, bool Interval, DateTime dtFrom, DateTime dtTo)
		{
            Logger = LogManager.GetLogger(GetType());
			_reports = new List<BaseReport>();
			_generalReportID = GeneralReportID;
			_firmCode = FirmCode;
			_conn = Conn;
			_contactGroupId = ContactGroupId;
			_eMailSubject = EMailSubject;
			_reportFileName = ReportFileName;
			_reportArchName = ReportArchName;
			_temporary = Temporary;
			Format = format;

			bool addContacts = false;
			ulong contactsCode = 0;

			_dtReports = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(new ExecuteArgs(), GetReports, null, _conn);

			if (!Interval)
				_dtContacts = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(new ExecuteArgs(), delegate(ExecuteArgs args)
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
				_dtContacts = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(new ExecuteArgs(), delegate(ExecuteArgs args)
				{
				args.DataAdapter.SelectCommand.CommandText = @"
select Mail FROM reports.Mailing_Addresses M
where GeneralReport = ?GeneralReport;";
				args.DataAdapter.SelectCommand.Parameters.AddWithValue("?GeneralReport", _generalReportID);
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
						BaseReport bs = (BaseReport)Activator.CreateInstance(
							GetReportTypeByName(drGReport[BaseReportColumns.colReportClassName].ToString()),
							new object[] { (ulong)drGReport[BaseReportColumns.colReportCode], 
								drGReport[BaseReportColumns.colReportCaption].ToString(), _conn, 
								Temporary, Format,
								propertiesLoader.LoadProperties(_conn, (ulong)drGReport[BaseReportColumns.colReportCode])});
						bs._Interval = Interval;
						bs._dtFrom = dtFrom;
						bs._dtTo = dtTo;
						_reports.Add(bs);

						//Если у общего отчета не выставлена тема письма, то берем ее у первого попавшегося отчета
						if (String.IsNullOrEmpty(_eMailSubject) && !String.IsNullOrEmpty(drGReport[BaseReportColumns.colAlternateSubject].ToString()))
							_eMailSubject = drGReport[BaseReportColumns.colAlternateSubject].ToString();

						//Если в отчетах содержится или комбинированый или специальный отчет, то добавляем в отчеты Контакты
						if (!addContacts)
						{
							addContacts = (bs.GetType() == typeof(CombReport)) || (bs.GetType() == typeof(SpecReport));
							if (addContacts)
								contactsCode = (ulong)drGReport[BaseReportColumns.colReportCode];
						}
					}
				}
			}
			else
				throw new ReportException("У комбинированного отчета нет дочерних отчетов.");

			if (addContacts && Format == ReportFormats.Excel)
				_reports.Add(new ContactsReport(contactsCode, "Контакты", _conn, Temporary, Format, propertiesLoader.LoadProperties(_conn, contactsCode)));
		}

		//Производится построение отчетов
		public void ProcessReports()
		{
			_directoryName = Path.GetTempPath() + "Rep" + _generalReportID.ToString();
			if (Directory.Exists(_directoryName))
				Directory.Delete(_directoryName, true);
			Directory.CreateDirectory(_directoryName);

			_mainFileName = _directoryName + "\\" + ((String.IsNullOrEmpty(_reportFileName)) ? ("Rep" + _generalReportID.ToString() + ".xls") : _reportFileName);

			foreach (BaseReport bs in _reports)
			{
				bs.ReadReportParams();
				bs.ProcessReport();
			}

			foreach (BaseReport bs in _reports)
			{
				bs.ReportToFile(_mainFileName);
			}
            

			string ResFileName = ArchFile();
            

#if (TESTING)
			MailWithAttach(ResFileName, Settings.Default.ErrorReportMail);
#else
			if ((_dtContacts != null) && (_dtContacts.Rows.Count > 0))
				foreach (DataRow drContact in _dtContacts.Rows)
					MailWithAttach(ResFileName, drContact[0].ToString());
#endif
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

		private void MailWithAttach(string archFileName, string EMailAddress)
		{ 
			var message = new Mime(); 
			var mainEntry = message.MainEntity; 

			mainEntry.From = new AddressList {new MailboxAddress("АК Инфорум", "report@analit.net")};

			mainEntry.To = new LumiSoft.Net.Mime.AddressList();
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
			attachmentEntity.ContentDisposition_FileName = System.IO.Path.GetFileName(archFileName); 
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
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?GeneralReportCode", _generalReportID);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SMTPID", e.SmtpID);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?MessageID", e.MessageID);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?EMail", e.EMail);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			return 0;
		}

		private string ArchFile()
		{
			var ResDirPath = Properties.Settings.Default.FTPOptBoxPath;
			var resArchFileName = (String.IsNullOrEmpty(_reportArchName)) ? Path.ChangeExtension(Path.GetFileName(_mainFileName), ".zip") : _reportArchName;

			ResDirPath += _firmCode.ToString("000") + "\\Reports\\";

			if (!(Directory.Exists(ResDirPath)))
				Directory.CreateDirectory(ResDirPath);

			if (File.Exists(ResDirPath + resArchFileName))
				File.Delete(ResDirPath + resArchFileName);

			var zip = new FastZip();
			var tempArchive = Path.GetTempFileName();
            Logger.DebugFormat("zip.CreateZip {0}, {1}", tempArchive, _directoryName);
			zip.CreateZip(tempArchive, _directoryName, false, null, null);
			var archive = Path.Combine(_directoryName, resArchFileName);
            Logger.DebugFormat("File.Move {0}, {1}", tempArchive, archive);
			File.Move(tempArchive, archive);
            try
            {
                Logger.DebugFormat("File.Copy {0}, {1}", archive, ResDirPath + resArchFileName);
                File.Copy(archive, ResDirPath + resArchFileName);
            }
            catch(Exception ex)
            {
                Logger.ErrorFormat("Message: {0}, Stack: {1}", ex.Message, ex.StackTrace);
                Logger.Error("Exception:", ex);                
            }
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
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?" + GeneralReportColumns.GeneralReportCode, _generalReportID);
			DataTable res = new DataTable();
			e.DataAdapter.Fill(res);
			return res;
		}

		private Type GetReportTypeByName(string ReportTypeClassName)
		{
			Type t = Type.GetType(ReportTypeClassName);
			if (t == null)
				throw new ReportException(String.Format("Неизвестный тип отчета : {0}", ReportTypeClassName));
			return t;
		}
	}
}
