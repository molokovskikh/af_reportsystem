using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using Microsoft.Win32.TaskScheduler;
using System.Configuration;
using ReportTuner.Models;
using Action = Microsoft.Win32.TaskScheduler.Action;

namespace ReportTuner.Helpers
{
	public static class ScheduleHelper
	{
		public static string ScheduleServer = ConfigurationManager.AppSettings["ScheduleServer"];
		public static string ScheduleDomainName = ConfigurationManager.AppSettings["ScheduleDomainName"];
		public static string ScheduleUserName = ConfigurationManager.AppSettings["ScheduleUserName"];
		public static string SchedulePassword = ConfigurationManager.AppSettings["SchedulePassword"];

		public static string ScheduleWorkDir = ConfigurationManager.AppSettings["ScheduleWorkDir"];
		public static string ScheduleAppPath = ConfigurationManager.AppSettings["ScheduleAppPath"];
		public static string ReportsFolderName = ConfigurationManager.AppSettings["ReportsFolderName"];
		

		public static TaskService GetService()
		{
			return new TaskService(ScheduleServer, ScheduleUserName, ScheduleDomainName, SchedulePassword);
		}

		public static TaskFolder GetReportsFolder(TaskService taskService)
		{
			try
			{
				return taskService.GetFolder(ReportsFolderName);
			}
			catch (System.IO.FileNotFoundException ex)
			{
				throw new ReportTunerException(String.Format("На сервере {0} не существует папка '{1}' в планировщике задач",
					ScheduleServer, ReportsFolderName), ex);
			}
		}

		public static void DeleteTask(TaskFolder reportsFolder, ulong generalReportId, string prefix)
		{
			try
			{
				reportsFolder.DeleteTask(prefix + generalReportId);
			}
			catch (System.IO.FileNotFoundException)
			{
				//"Гасим" это исключение при попытке удалить задание, которого не существует
			}
		}

		public static Task CreateTask(TaskService taskService, TaskFolder reportsFolder, ulong generalReportId, string comment, string prefix)
		{
			TaskDefinition createTaskDefinition = taskService.NewTask();

			createTaskDefinition.RegistrationInfo.Author = ScheduleDomainName + "\\" + ScheduleUserName;
			createTaskDefinition.RegistrationInfo.Date = DateTime.Now;
			createTaskDefinition.RegistrationInfo.Description = comment;

			createTaskDefinition.Settings.AllowDemandStart = true;
			createTaskDefinition.Settings.AllowHardTerminate = true;
			createTaskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(1);

			createTaskDefinition.Actions.Add(new ExecAction(ScheduleAppPath, "/gr:" + generalReportId, ScheduleWorkDir));

			return reportsFolder.RegisterTaskDefinition(
				prefix + generalReportId,
				createTaskDefinition,
				TaskCreation.Create,
				GetUser(),
				GetPassword(),
				GetLogonType(),
				null);
		}

		public static Task FindTask(TaskService taskService, TaskFolder reportsFolder, ulong generalReportId, string prefix)
		{
			return reportsFolder.Tasks.FirstOrDefault(
				task => task.Name.Equals(prefix + generalReportId, StringComparison.OrdinalIgnoreCase));
		}

		public static Task UpdateTaskDefinition(TaskService taskService, TaskFolder reportsFolder, ulong generalReportId, TaskDefinition updateTaskDefinition, string prefix)
		{
			return reportsFolder.RegisterTaskDefinition(
				prefix + generalReportId,
				updateTaskDefinition,
				TaskCreation.Update,
				GetUser(),
				GetPassword(),
				GetLogonType(),
				null);
		}

		public static TaskLogonType GetLogonType()
		{
			if (!String.IsNullOrEmpty(SchedulePassword))
				return TaskLogonType.Password;
			return TaskLogonType.InteractiveToken;
		}

		public static string GetPassword()
		{
			if (!String.IsNullOrEmpty(SchedulePassword))
				return SchedulePassword;
			return null;
		}

		public static string GetUser()
		{
			if (!String.IsNullOrEmpty(SchedulePassword))
				return ScheduleDomainName + "\\" + ScheduleUserName;
			return null;
		}

		public static IEnumerable<Task> GetAllTempTask(TaskFolder reportsFolder)
		{
			return reportsFolder.Tasks.Where(
				task => task.Name.IndexOf("temp", StringComparison.OrdinalIgnoreCase) != -1);
		}

		/// <summary>
		/// производим поиск задачи и обновление Description, если задача не существует, то она будет создана
		/// </summary>
		/// <param name="taskService"></param>
		/// <param name="reportsFolder"></param>
		/// <param name="generalReportId"></param>
		/// <param name="comment"></param>
		/// <returns></returns>
		public static Task GetTask(TaskService taskService, TaskFolder reportsFolder, ulong generalReportId, string comment, string prefix)
		{
			try
			{
				return FindTask(taskService, reportsFolder, generalReportId,prefix);

				//Нашли задачу, производим обновление
				/*TaskDefinition updateTaskDefinition = updateTask.Definition;
				updateTaskDefinition.RegistrationInfo.Description = comment;

				return UpdateTaskDefinition(taskService, reportsFolder, generalReportId, updateTaskDefinition,prefix);	*/			
			}
			catch(InvalidOperationException)
			{
				//Задачу не нашли, поэтому создаем ее
				return CreateTask(taskService, reportsFolder, generalReportId, comment,prefix);
			}
		}

		// Выставляем состояние задачи (Включено / Выключено)
		public static void SetTaskEnableStatus(ulong reportId, bool isEnable, string prefix)
		{
			TaskService service = GetService();
			TaskFolder folder = GetReportsFolder(service);
			Task task = FindTask(service, folder, reportId, prefix);
			if (task == null)
				return;

			TaskDefinition definition = task.Definition;
			definition.Settings.Enabled = isEnable;
			UpdateTaskDefinition(service, folder, reportId, definition, prefix);
		}
	}
}
