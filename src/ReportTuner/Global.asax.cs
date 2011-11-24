using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Text;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Container;
using Castle.MonoRail.Framework.Services;
using log4net;
using log4net.Config;
using ReportTuner.Models;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;

namespace ReportTuner
{
	public class Global : HttpApplication, IMonoRailContainerEvents
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

		void Application_Start(object sender, EventArgs e)
		{			
			XmlConfigurator.Configure();
			ActiveRecordStarter.Initialize(new[] {
					Assembly.Load("ReportTuner"),
					Assembly.Load("Common.Web.Ui")
				},
				ActiveRecordSectionHandler.Instance);

			if (!Path.IsPathRooted(ScheduleHelper.ScheduleAppPath))
				ScheduleHelper.ScheduleAppPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScheduleHelper.ScheduleAppPath));

			if (!Path.IsPathRooted(ScheduleHelper.ScheduleWorkDir))
				ScheduleHelper.ScheduleWorkDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScheduleHelper.ScheduleWorkDir));


#if DEBUG
			var taskService = ScheduleHelper.GetService();
			var root = taskService.RootFolder;
			var folder = root.SubFolders
				.FirstOrDefault(f => String.Equals(f.Name, ScheduleHelper.ReportsFolderName, StringComparison.CurrentCultureIgnoreCase));
			if (folder == null)
				root.CreateFolder(ScheduleHelper.ReportsFolderName, null);
#endif

			//Проверяем существование шаблонного отчета в базе, если нет, то приложение не запускаем
			ulong _TemplateReportId;
			if (ulong.TryParse(System.Configuration.ConfigurationManager.AppSettings["TemplateReportId"], out _TemplateReportId))
			{
				try
				{
					GeneralReport.Find(_TemplateReportId);
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

		public void Created(IMonoRailContainer container)
		{}

		public void Initialized(IMonoRailContainer container)
		{
			var defaultViewComponentFactory = ((DefaultViewComponentFactory)container.GetService<IViewComponentFactory>());
			defaultViewComponentFactory.Inspect(Assembly.Load("ReportTuner"));
			defaultViewComponentFactory.Inspect(Assembly.Load("Common.Web.Ui"));
		}
	}
}