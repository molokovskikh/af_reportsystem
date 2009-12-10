using System;
using System.Data;
using MySql.Data.MySqlClient;
using Inforoom.ReportSystem.Filters;
using Inforoom.ReportSystem;
using System.Collections.Generic;
using ExecuteTemplate;
using System.Runtime.Remoting;
using System.IO;
using ICSharpCode.SharpZipLib;
using LumiSoft.Net.Mime;
using Zip = ICSharpCode.SharpZipLib.Zip;
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

		//таблица отчетов, которая существует в общем отчете
		DataTable _dtReports;

		//таблица контактов, по которым надо отправить отчет
		DataTable _dtContacts;

		List<BaseReport> _reports;

		public GeneralReport(ulong GeneralReportID, int FirmCode, uint? ContactGroupId, 
			string EMailSubject, MySqlConnection Conn, string ReportFileName, 
			string ReportArchName, bool Temporary, ReportFormats format,
			IReportPropertiesLoader propertiesLoader)
		{
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

			if ((_dtReports != null) && (_dtReports.Rows.Count > 0))
			{
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
				throw new Exception("У комбинированного отчета нет дочерних отчетов.");

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

			if (Directory.Exists(_directoryName))
				Directory.Delete(_directoryName, true);
		}

		private void MailWithAttach(string archFileName, string EMailAddress)
		{ 
			Mime message = new Mime(); 
			MimeEntity mainEntry = message.MainEntity; 

			mainEntry.From = new AddressList(); 
			mainEntry.From.Add(new MailboxAddress("АК Инфорум", "report@analit.net")); 

			mainEntry.To = new LumiSoft.Net.Mime.AddressList();
			mainEntry.To.Parse(EMailAddress); 

			mainEntry.Subject = _eMailSubject; 

			mainEntry.ContentType = MediaType_enum.Multipart_mixed;

			MimeEntity textEntity = mainEntry.ChildEntities.Add();
			textEntity.ContentType = MediaType_enum.Text_plain;
			textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
			textEntity.DataText = String.Empty; 

			MimeEntity attachmentEntity = mainEntry.ChildEntities.Add(); 
			attachmentEntity.ContentType = MediaType_enum.Application_octet_stream; 
			attachmentEntity.ContentDisposition = ContentDisposition_enum.Attachment; 
			attachmentEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64; 
			attachmentEntity.ContentDisposition_FileName = System.IO.Path.GetFileName(archFileName); 
			attachmentEntity.DataFromFile(archFileName);

			int? SMTPID = LumiSoft.Net.SMTP.Client.SmtpClientEx.QuickSendSmartHostSMTPID(Settings.Default.SMTPHost, null, null, message);

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
			MemoryStream ZipOutputStream = new MemoryStream();			
			Zip.ZipOutputStream ZipInputStream = new Zip.ZipOutputStream(ZipOutputStream);
			string ZipEntryName;

			string[] files = Directory.GetFiles(_directoryName);

			foreach (string fileName in files)
			{
				ZipEntryName = Path.GetFileName(fileName);
				Zip.ZipEntry ZipObject = new Zip.ZipEntry(ZipEntryName);
				FileStream MySqlFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 10240);
				byte[] MySqlFileByteArray = new byte[MySqlFileStream.Length];
				MySqlFileStream.Read(MySqlFileByteArray, 0, Convert.ToInt32(MySqlFileStream.Length));
				ZipInputStream.SetLevel(9);
				ZipObject.DateTime = DateTime.Now;
				ZipInputStream.PutNextEntry(ZipObject);
				ZipInputStream.Write(MySqlFileByteArray, 0, Convert.ToInt32(MySqlFileStream.Length));
				MySqlFileStream.Close();
			}
			ZipInputStream.Finish();

#if (TESTING)
			string ResDirPath = "C:\\Temp\\Reports\\";
#else
			string ResDirPath = Properties.Settings.Default.FTPOptBoxPath;
#endif

			string resArchFileName = (String.IsNullOrEmpty(_reportArchName)) ? Path.ChangeExtension(Path.GetFileName(_mainFileName), ".zip") : _reportArchName;

			ResDirPath += _firmCode.ToString("000") + "\\Reports\\";

			if (!(Directory.Exists(ResDirPath)))
			{
				Directory.CreateDirectory(ResDirPath);
			}

			if (File.Exists(ResDirPath + resArchFileName))
			{
				File.Delete(ResDirPath + resArchFileName);
			}

			FileStream ResultFile = new FileStream(_directoryName + "\\" + resArchFileName, FileMode.CreateNew);
			ResultFile.Write(ZipOutputStream.ToArray(), 0, Convert.ToInt32(ZipOutputStream.Length));
			ZipInputStream.Close();
			ZipOutputStream.Close();
			ResultFile.Close();

			File.Copy(_directoryName + "\\" + resArchFileName, ResDirPath + resArchFileName);

			return _directoryName + "\\" + resArchFileName;
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
				throw new Exception(String.Format("Неизвестный тип отчета : {0}", ReportTypeClassName));
			return t;
		}

		private Type GetReportTypeByCode(ulong ReportTypeCode)
		{
			switch(ReportTypeCode)
			{
				case 1:
					return typeof(CombReport);
				case 2:
					return typeof(SpecReport);
				case 5:
					return typeof(DefReport);
				case 6:
					return typeof(CombShortReport);
				case 7:
					return typeof(RatingReport);
				case 8:
					return typeof(SpecShortReport);
				default:
					throw new Exception(String.Format("Неизвестный тип отчета : {0}", ReportTypeCode));
			}
		}
	}
}
