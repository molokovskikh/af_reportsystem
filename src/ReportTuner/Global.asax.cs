using System;
using System.Data;
using System.Configuration;
using System.IO;
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
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Views.Aspx;
using Castle.MonoRail.Views.Brail;
using log4net;
using ReportTuner.Models;
using NHibernate.Criterion;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;

/// <summary>
/// Summary description for Global
/// </summary>
namespace Inforoom.ReportTuner
{
	public class Global : HttpApplication
	{
		private System.ComponentModel.IContainer components;
		private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

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

			//Проверяем существование шаблонного отчета в базе, если нет, то приложение не запускаем
			ulong _TemplateReportId;
			if (ulong.TryParse(System.Configuration.ConfigurationManager.AppSettings["TemplateReportId"], out _TemplateReportId))
			{
				try
				{
					GeneralReport _templateReport = GeneralReport.Find(_TemplateReportId);
				}
				catch (NotFoundException exp)
				{
					throw new ReportTunerException("В файле Web.Config параметр TemplateReportId указывает на несуществующую запись.", exp);
				}
			}
			else
				throw new ReportTunerException("В файле Web.Config параметр TemplateReportId не существует или настроен некорректно.");

		}

		void Session_Start(object sender, EventArgs e)
		{
			//Это имя пользователя добавляем для того, чтобы корректно редактировались контакты
			string UserName = HttpContext.Current.User.Identity.Name;
			if (UserName.StartsWith("ANALIT\\", StringComparison.OrdinalIgnoreCase))
				UserName = UserName.Substring(7);
			Session["UserName"] = UserName;

			//Удаляем временные отчеты, которые старше 1 дня
			GeneralReport[] _temporaryReportsForDelete = GeneralReport.FindAll(
				Expression.Eq("Temporary", true),
				Expression.Le("TemporaryCreationDate", DateTime.Now.AddDays(-1)));
			using (TaskService taskService = ScheduleHelper.GetService())
			using (TaskFolder reportsFolder = ScheduleHelper.GetReportsFolder(taskService))
			{
				if (_temporaryReportsForDelete.Length > 0)
					using (new TransactionScope())
					{

						foreach (GeneralReport _report in _temporaryReportsForDelete)
						{
							ScheduleHelper.DeleteTask(reportsFolder, _report.Id, "GR");
							_report.Delete();
						}
					}
				/*foreach (var delTask in ScheduleHelper.GetAllTempTask(reportsFolder))
				{
					if (delTask.State != TaskState.Running)
					try
					{
						reportsFolder.DeleteTask(delTask.Name);
					}
					catch (System.IO.FileNotFoundException)
					{
					}
				}*/
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
			var exception = Server.GetLastError();

			var builder = new StringBuilder();
			builder.AppendLine("----UrlReferer-------");
			builder.AppendLine(Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : String.Empty);
			builder.AppendLine("----Url-------");
			builder.AppendLine(Request.Url.ToString());
			builder.AppendLine("--------------");
			builder.AppendLine("----Params----");
			foreach (string name in Request.QueryString)
				builder.AppendLine(String.Format("{0}: {1}", name, Request.QueryString[name]));
			builder.AppendLine("--------------");

			builder.AppendLine("----Error-----");
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
			try
			{
				foreach (string key in Session.Keys)
				{
					if (Session[key] == null)
						builder.AppendLine(String.Format("{0} - null", key));
					else
						builder.AppendLine(String.Format("{0} - {1}", key, Session[key]));
				}
			}
			catch (Exception ex)
			{ }
			builder.AppendLine("--------------");

			_log.Error(builder.ToString());
		}

		/*public void Configure(IMonoRailConfiguration configuration)
		{
			configuration.ControllersConfig.AddAssembly("ReportTuner");
			configuration.ControllersConfig.AddAssembly("Common.Web.Ui");
			configuration.ViewComponentsConfig.Assemblies = new[] {
				"ReportTuner",
				"Common.Web.Ui"
			};
			configuration.ViewEngineConfig.ViewPathRoot = "Views";
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(WebFormsViewEngine), false));
			configuration.ViewEngineConfig.AssemblySources.Add(new AssemblySourceInfo("Common.Web.Ui", "Common.Web.Ui.Views"));
			configuration.ViewEngineConfig.VirtualPathRoot = configuration.ViewEngineConfig.ViewPathRoot;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration.ViewEngineConfig.ViewPathRoot);
		}*/

		void Session_End(object sender, EventArgs e)
		{
			//Code that runs when a session ends. 
			//Note: The Session_End event is raised only when the sessionstate mode
			//is set to InProc in the Web.config file. If session mode is set to StateServer 
			//or SQLServer, the event is not raised.

			//Проходим по всем объектам в сессии и если объект поддерживает интефейс IDisposable, то вызываем Dispose()
			for (int i = 0; i < Session.Count; i++)
				if (Session[i] is IDisposable)
					((IDisposable)Session[i]).Dispose();
			//Очищаем коллекцию
			Session.Clear();
			//Производим сборку мусора
			GC.Collect();
		}

		void Application_End(object sender, EventArgs e)
		{
			//Code that runs on application shutdown
		}
	}
}