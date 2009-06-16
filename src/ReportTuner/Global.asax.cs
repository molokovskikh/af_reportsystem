using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using ReportTuner.Models;
using NHibernate.Criterion;
using Microsoft.Win32.TaskScheduler;

/// <summary>
/// Summary description for Global
/// </summary>
namespace Inforoom.ReportTuner
{
	public class Global : HttpApplication
	{
		private System.ComponentModel.IContainer components;

		public Global()
		{
			InitializeComponent();
		}

		[System.Diagnostics.DebuggerStepThrough()]
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}

		void Application_Start(object sender, EventArgs e)
		{
			ActiveRecordStarter.Initialize(new[]
				                               	{
				                               		Assembly.Load("ReportTuner"),
				                               		Assembly.Load("Common.Web.Ui")
				                               	},
										   ActiveRecordSectionHandler.Instance);

			//��������� ������������� ���������� ������ � ����, ���� ���, �� ���������� �� ���������
			ulong _TemplateReportId;
			if (ulong.TryParse(System.Configuration.ConfigurationManager.AppSettings["TemplateReportId"], out _TemplateReportId))
			{
				try
				{
					GeneralReport _templateReport = GeneralReport.Find(_TemplateReportId);
				}
				catch (NotFoundException exp)
				{
					throw new Exception("� ����� Web.Config �������� TemplateReportId ��������� �� �������������� ������.", exp);
				}
			}
			else
				throw new Exception("� ����� Web.Config �������� TemplateReportId �� ���������� ��� �������� �����������.");

		}

		void Session_Start(object sender, EventArgs e)
		{
			//��� ��� ������������ ��������� ��� ����, ����� ��������� ��������������� ��������
			string UserName = HttpContext.Current.User.Identity.Name;
			if (UserName.StartsWith("ANALIT\\", StringComparison.OrdinalIgnoreCase))
				UserName = UserName.Substring(7);
			Session["UserName"] = UserName;

			//������� ��������� ������, ������� ������ 1 ���
			GeneralReport[] _temporaryReportsForDelete = GeneralReport.FindAll(
				Expression.Eq("Temporary", true),
				Expression.Le("TemporaryCreationDate", DateTime.Now.AddDays(-1)));
			if (_temporaryReportsForDelete.Length > 0)
				using (new TransactionScope())
				{
					foreach (GeneralReport _report in _temporaryReportsForDelete)
					{
						DeleteTask(_report.Id);
						_report.Delete();
					}
				}
		}

		private void DeleteTask(ulong generalReportId)
		{
			using (TaskService taskService = new TaskService(
				ConfigurationManager.AppSettings["asComp"],
				ConfigurationManager.AppSettings["asScheduleUserName"],
				ConfigurationManager.AppSettings["asScheduleDomainName"],
				ConfigurationManager.AppSettings["asSchedulePassword"]))
				using (TaskFolder reportsFolder = taskService.GetFolder(ConfigurationManager.AppSettings["asReportsFolderName"]))
					try
					{
						reportsFolder.DeleteTask("GR" + generalReportId + ".job");
					}
					catch (System.IO.FileNotFoundException)
					{
						//"�����" ��� ���������� ��� ������� ������� �������, �������� �� ����������
					}
		}

		void Application_BeginRequest(object sender, EventArgs e)
		{
		}

		void Application_AuthenticateRequest(object sender, EventArgs e)
		{
		}

		void Application_Error(object sender, EventArgs e)
		{
			// Code that runs when an unhandled error occurs
			bool sessionExists = false;
			try
			{
				sessionExists = this.Session != null;
			}
			catch
			{
				sessionExists = false;
			}

			//��� �������� ������ ���������� �������� �� logon.aspx � � ���� ������ ��������� ������
			//���� ������ �� ����������, �� ��� ������ �� ������ � �� ��� ��������
			if (sessionExists)
			{
				StringBuilder builder = new StringBuilder();
				builder.AppendLine("----Url-------");
				builder.AppendLine(Request.Url.ToString());
				builder.AppendLine("--------------");
				builder.AppendLine("----Params----");
				foreach (string name in Request.QueryString)
					builder.AppendLine(String.Format("{0}: {1}", name, Request.QueryString[name]));
				builder.AppendLine("--------------");

				builder.AppendLine("----Error-----");
				Exception exception = Server.GetLastError();
				do
				{
					builder.AppendLine("Message:");
					builder.AppendLine(exception.Message);
					builder.AppendLine("Stack Trace:");
					builder.AppendLine(exception.StackTrace);
					builder.AppendLine("--------------");
					exception = exception.InnerException;
				} while (exception != null);
				builder.AppendLine("--------------");

				builder.AppendLine("----Session---");
				foreach (string key in Session.Keys)
				{
					if (Session[key] == null)
						builder.AppendLine(String.Format("{0} - null", key));
					else
						builder.AppendLine(String.Format("{0} - {1}", key, Session[key].GetType().ToString()));
				}
				builder.AppendLine("--------------");

				builder.AppendLine(String.Format("Version : {0}", Assembly.GetExecutingAssembly().GetName().Version));

				System.Net.Mail.MailMessage m = new System.Net.Mail.MailMessage("service@analit.net", "service@analit.net", "������ � ���������� ��������� ������", builder.ToString());
				m.BodyEncoding = Encoding.UTF8;

#if RELEASE
				System.Net.Mail.SmtpClient c = new System.Net.Mail.SmtpClient(System.Configuration.ConfigurationManager.AppSettings["SMTPHost"]);
				c.Send(m);
#endif
			}
		}

		void Session_End(object sender, EventArgs e)
		{
			//Code that runs when a session ends. 
			//Note: The Session_End event is raised only when the sessionstate mode
			//is set to InProc in the Web.config file. If session mode is set to StateServer 
			//or SQLServer, the event is not raised.

			//�������� �� ���� �������� � ������ � ���� ������ ������������ �������� IDisposable, �� �������� Dispose()
			for (int i = 0; i < Session.Count; i++)
				if (Session[i] is IDisposable)
					((IDisposable)Session[i]).Dispose();
			//������� ���������
			Session.Clear();
			//���������� ������ ������
			GC.Collect();
		}

		void Application_End(object sender, EventArgs e)
		{
			//Code that runs on application shutdown
		}
	}
}