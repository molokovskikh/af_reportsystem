using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Win32.TaskScheduler;
using System.Configuration;
using ReportTuner.Models;

namespace ReportTuner.Helpers
{
	public static class ScheduleHelper
	{
		readonly static string ScheduleServer = ConfigurationManager.AppSettings["ScheduleServer"];
		readonly static string ScheduleDomainName = ConfigurationManager.AppSettings["ScheduleDomainName"];
		readonly static string ScheduleUserName = ConfigurationManager.AppSettings["ScheduleUserName"];
		readonly static string SchedulePassword = ConfigurationManager.AppSettings["SchedulePassword"];
		readonly static string ScheduleWorkDir = ConfigurationManager.AppSettings["ScheduleWorkDir"];
		readonly static string ScheduleAppPath = ConfigurationManager.AppSettings["ScheduleAppPath"];
		readonly static string ReportsFolderName = ConfigurationManager.AppSettings["ReportsFolderName"];
		

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

		public static void DeleteTask(TaskFolder reportsFolder, ulong generalReportId)
		{
			try
			{
				reportsFolder.DeleteTask("GR" + generalReportId);
			}
			catch (System.IO.FileNotFoundException)
			{
				//"Гасим" это исключение при попытке удалить задание, которого не существует
			}
		}

		public static Task CreateTask(TaskService taskService, TaskFolder reportsFolder, ulong generalReportId, string comment)
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
				"GR" + generalReportId,
				createTaskDefinition,
				TaskCreation.Create,
				ScheduleDomainName + "\\" + ScheduleUserName,
				SchedulePassword,
				TaskLogonType.Password,
				null);
		}

		public static Task FindTask(TaskService taskService, TaskFolder reportsFolder, ulong generalReportId)
		{
			return reportsFolder.Tasks.First(
				task => task.Name.Equals("GR" + generalReportId, StringComparison.OrdinalIgnoreCase));
		}

		public static Task UpdateTaskDefinition(TaskService taskService, TaskFolder reportsFolder, ulong generalReportId, TaskDefinition updateTaskDefinition)
		{
			return reportsFolder.RegisterTaskDefinition(
				"GR" + generalReportId,
				updateTaskDefinition,
				TaskCreation.Update,
				ScheduleDomainName + "\\" + ScheduleUserName,
				SchedulePassword,
				TaskLogonType.Password,
				null);
		}

		/// <summary>
		/// производим поиск задачи и обновление Description, если задача не существует, то она будет создана
		/// </summary>
		/// <param name="taskService"></param>
		/// <param name="reportsFolder"></param>
		/// <param name="generalReportId"></param>
		/// <param name="comment"></param>
		/// <returns></returns>
		public static Task GetTask(TaskService taskService, TaskFolder reportsFolder, ulong generalReportId, string comment)
		{
			try
			{
				Task updateTask = FindTask(taskService, reportsFolder, generalReportId);

				//Нашли задачу, производим обновление
				TaskDefinition updateTaskDefinition = updateTask.Definition;
				updateTaskDefinition.RegistrationInfo.Description = comment;

				return UpdateTaskDefinition(taskService, reportsFolder, generalReportId, updateTaskDefinition);				
			}
			catch(InvalidOperationException)
			{
				//Задачу не нашли, поэтому создаем ее
				return CreateTask(taskService, reportsFolder, generalReportId, comment);
			}
		}

		// Выставляем состояние задачи (Включено / Выключено)
		public static void SetTaskEnableStatus(ulong reportId, bool isEnable)
		{
			TaskService service = GetService();
			TaskFolder folder = GetReportsFolder(service);
			Task task = FindTask(service, folder, reportId);
			if (task == null)
				return;

			TaskDefinition definition = task.Definition;
			definition.Settings.Enabled = isEnable;
			UpdateTaskDefinition(service, folder, reportId, definition);
		}
	}
}
