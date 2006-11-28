using System;
using System.Data;
using MySql.Data.MySqlClient;
using Inforoom.ReportSystem.RatingReports;
using Inforoom.ReportSystem;
using System.Collections.Generic;
using ExecuteTemplate;
using System.Runtime.Remoting;
using System.IO;
using ICSharpCode.SharpZipLib;
using LumiSoft.Net.Mime;
using Zip = ICSharpCode.SharpZipLib.Zip;

namespace Inforoom.ReportSystem
{

	//Содержит названия полей, используемых при создании общего очета
	public sealed class GeneralReportColumns
	{
		public const string GeneralReportCode = "GeneralReportCode";
		public const string FirmCode = "FirmCode";
		public const string Allow = "Allow";
		public const string EMailAddress = "EMailAddress";
		public const string EMailSubject = "EMailSubject";
		public const string ShortName = "ShortName";
	}

	/// <summary>
	/// Summary description for GeneralReport.
	/// </summary>
	public class GeneralReport
	{
		public ulong _generalReportID;
		public int _firmCode;

		private string _eMailAddress;
		private string _eMailSubject;

		private MySqlConnection _conn;

		private string _directoryName;
		private string _mainFileName;

		//таблица отчетов, которая существует в общем отчете
		DataTable _dtReports;

		List<BaseReport> _reports;

		public GeneralReport(ulong GeneralReportID, int FirmCode, string EMailAddress, string EMailSubject, MySqlConnection Conn)
		{
			_reports = new List<BaseReport>();
			_generalReportID = GeneralReportID;
			_firmCode = FirmCode;
			_conn = Conn;
			_eMailAddress = EMailAddress;
			_eMailSubject = EMailSubject;

			bool addContacts = false;
			ulong contactsCode = 0;

			_dtReports = MethodTemplate.ExecuteMethod<ExecuteArgs, DataTable>(new ExecuteArgs(), GetReports, null, _conn, true, false);

			if ((_dtReports != null) && (_dtReports.Rows.Count > 0))
			{
				foreach (DataRow drGReport in _dtReports.Rows)
				{
					//Создаем отчеты и добавляем их в список отчетов
					BaseReport bs = (BaseReport)Activator.CreateInstance(
						GetReportTypeByCode((ulong)drGReport[BaseReportColumns.colReportTypeCode]),
						new object[] { (ulong)drGReport[BaseReportColumns.colReportCode], drGReport[BaseReportColumns.colReportCaption].ToString(), _conn });
					_reports.Add(bs);

					//Если в отчетах содержится или комбинированый или специальный отчет, то добавляем в отчеты Контакты
					if (!addContacts)
					{
						addContacts = (bs.GetType() == typeof(CombReport)) || (bs.GetType() == typeof(SpecReport));
						if (addContacts)
							contactsCode = (ulong)drGReport[BaseReportColumns.colReportCode];
					}
				}
			}
			else
				throw new Exception("У комбинированного отчета нет дочерних отчетов.");

			if (addContacts)
			    _reports.Add(new ContactsReport(contactsCode, "Контакты", _conn));
		}

		//Производится построение отчетов
		public void ProcessReports()
		{
			_directoryName = Path.GetTempPath() + "Rep" + _generalReportID.ToString();
			if (Directory.Exists(_directoryName))
				Directory.Delete(_directoryName, true);
			Directory.CreateDirectory(_directoryName);

			_mainFileName = _directoryName + "\\" + "Rep" + _generalReportID.ToString() + ".xls";

			foreach (BaseReport bs in _reports)
			{
				bs.ProcessReport();
			}

			foreach (BaseReport bs in _reports)
			{
				bs.ReportToFile(_mainFileName);
			}

			string ResFileName = ArchFile();

			if (!String.IsNullOrEmpty(_eMailAddress))
			    MailWithAttach(ResFileName);
		}

		private void MailWithAttach(string archFileName)
		{ 
			Mime message = new Mime(); 
			MimeEntity mainEntry = message.MainEntity; 

			mainEntry.From = new AddressList(); 
			mainEntry.From.Add(new MailboxAddress("АК Инфорум", "report@analit.net")); 

			mainEntry.To = new LumiSoft.Net.Mime.AddressList();
#if (TESTING)
			mainEntry.To.Parse("s.morozov@analit.net");
			//mainEntry.To.Parse("msv-sergey@yandex.ru");
#else
			mainEntry.To.Parse(_eMailAddress); 
#endif

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

			int SMTPID = LumiSoft.Net.SMTP.Client.SmtpClientEx.QuickSendSmartHostSMTPID("box.analit.net", null, null, message);

			MethodTemplate.ExecuteMethod<ProcessLogArgs, int>(new ProcessLogArgs(SMTPID, message.MainEntity.MessageID), ProcessLog, 0, _conn, true, false);
		}

		class ProcessLogArgs : ExecuteArgs
		{
			public int _smtpID;
			public string _MessageID;

			public ProcessLogArgs(int smtpID, string MessageID)
			{
				_smtpID = smtpID;
				_MessageID = MessageID;
			}
		}

		private int ProcessLog(ProcessLogArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = @"insert into logs.reportslogs (LogTime, GeneralReportCode, SMTPID, MessageID) 
values (NOW(), ?GeneralReportCode, ?SMTPID, ?MessageID)";
			e.DataAdapter.SelectCommand.Parameters.Add("GeneralReportCode", _generalReportID);
			e.DataAdapter.SelectCommand.Parameters.Add("SMTPID", e._smtpID);
			e.DataAdapter.SelectCommand.Parameters.Add("MessageID", e._MessageID);
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
				ZipInputStream.Finish();
			}

#if (TESTING)
			string ResDirPath = "C:\\Temp\\Reports\\";
#else
			string ResDirPath = "\\\\isrv\\FTP\\OptBox\\";
#endif

			string resArchFileName = Path.ChangeExtension(Path.GetFileName(_mainFileName), ".zip");

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
			e.DataAdapter.SelectCommand.CommandText = String.Format("select * from reports.Reports where {0} = ?{0}", GeneralReportColumns.GeneralReportCode);
			e.DataAdapter.SelectCommand.Parameters.Add(GeneralReportColumns.GeneralReportCode, _generalReportID);
			DataTable res = new DataTable();
			e.DataAdapter.Fill(res);
			return res;
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
