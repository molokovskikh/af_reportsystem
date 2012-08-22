﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Microsoft.Win32.TaskScheduler;
using MySql.Data.MySqlClient;
using ReportTuner.Helpers;

namespace ReportTuner.Test
{
	[TestFixture]
	public class TaskSchedulerFixture
	{
		[Test]
		public void RunTask()
		{
			var taskService = ScheduleHelper.GetService();
			var reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
			var currentTask = ScheduleHelper.GetTask(taskService, reportsFolder, 1, "Это тестовый отчет Морозова (Рейтинг)", "GR");
			currentTask.Run();
			Thread.Sleep(500);
			if (currentTask != null) {
				currentTask.Dispose();
				currentTask = null;
			}
			if (taskService != null) {
				taskService.Dispose();
				taskService = null;
			}
		}

		[Test(Description = "Включаем все задачи c отчетами на offdc")]
		public void EnabledAllTasks()
		{
			return;

			//using (TaskService toTaskService = new TaskService("offdc", "runer", "analit", "zcxvcb"))
			//{
			//    foreach (Task task in toTaskService.RootFolder.Tasks)
			//    {
			//        int generalReportCode = 0;
			//        if (task.Name.StartsWith("GR") && (task.Name.Length > 2)
			//            && int.TryParse(task.Name.Substring(2), out generalReportCode))
			//        {
			//            task.Enabled = true;
			//        }
			//    }
			//}
		}

		[Test(Description = "Мигрирование задач со старого offdc на новый")]
		public void MigrationTasks()
		{
			string connectionString = "Database=usersettings;Data Source=sql.analit.net;User Id=Morozov;Password=Srt38123;pooling=false";

			return;

			using (TaskService fromTaskService = new TaskService("offdcold", "morozov_sam", "analit", "LtAtylth7", true)) {
				using (TaskService toTaskService = new TaskService("offdc", "runer", "analit", "zcxvcb")) {
					int allCount = 0, emptyTriggerCount = 0, standartTriggerCount = 0, anotherTriggerCount = 0, existsCount = 0;
					foreach (Task task in fromTaskService.RootFolder.Tasks) {
						int generalReportCode = 0;
						if (task.Name.StartsWith("GR") && (task.Name.Length > 2)
							&& int.TryParse(task.Name.Substring(2), out generalReportCode)) {
							allCount++;

							if (MySqlHelper.ExecuteScalar(
								connectionString,
								"select GeneralReportCode from reports.general_reports g WHERE g.GeneralReportCode = ?GeneralReportCode",
								new MySqlParameter("?GeneralReportCode", generalReportCode)) != null) {
								existsCount++;

								// Create a new task definition and assign properties
								TaskDefinition newTaskDefinition = toTaskService.NewTask();

								if (!String.IsNullOrEmpty(task.Definition.RegistrationInfo.Description))
									newTaskDefinition.RegistrationInfo.Description = task.Definition.RegistrationInfo.Description;


								// Create an action that will launch Notepad whenever the trigger fires
								newTaskDefinition.Actions.Add(new ExecAction(
									"C:\\Services\\Reports\\ReportSystem.exe",
									((ExecAction)task.Definition.Actions[0]).Arguments,
									"C:\\Services\\Reports"));

								if (task.Definition.Triggers.Count == 0) {
									emptyTriggerCount++;
								}
								else if ((task.Definition.Triggers.Count == 1) && (task.Definition.Triggers[0] is WeeklyTrigger)) {
									newTaskDefinition.Triggers.Add(new WeeklyTrigger() {
										DaysOfWeek = ((WeeklyTrigger)task.Definition.Triggers[0]).DaysOfWeek,
										StartBoundary = ((WeeklyTrigger)task.Definition.Triggers[0]).StartBoundary,
										WeeksInterval = ((WeeklyTrigger)task.Definition.Triggers[0]).WeeksInterval
									});
									standartTriggerCount++;
								}
								else {
									anotherTriggerCount++;
									string triggers = "   triggers : ";
									foreach (Trigger trigger in task.Definition.Triggers) {
										triggers += String.Format("{0} {{{1}}}", trigger.GetType().Name, trigger.ToString());
										if (trigger is WeeklyTrigger) {
											throw new Exception("Этого не должно было быть");
											newTaskDefinition.Triggers.Add(new WeeklyTrigger() {
												DaysOfWeek = ((WeeklyTrigger)trigger).DaysOfWeek,
												StartBoundary = ((WeeklyTrigger)trigger).StartBoundary,
												WeeksInterval = ((WeeklyTrigger)trigger).WeeksInterval
											});
										}
										else if (trigger is MonthlyTrigger) {
											newTaskDefinition.Triggers.Add(new MonthlyTrigger() {
												DaysOfMonth = ((MonthlyTrigger)trigger).DaysOfMonth,
												MonthsOfYear = ((MonthlyTrigger)trigger).MonthsOfYear,
												StartBoundary = ((MonthlyTrigger)trigger).StartBoundary
											});
										}
										else
											throw new Exception("Этого не должно было быть");
									}
								}

								// Register the task in the root folder
								Task newTask = toTaskService.RootFolder.RegisterTaskDefinition(task.Name, newTaskDefinition);
								newTask.Enabled = false;
							}
						}
					}
				}
			}
		}

		[Test(Description = "Выставили корректные параметры для задач")]
		public void SetCorrectParamsToTasks()
		{
			return;

			//using (TaskService toTaskService = new TaskService("offdc", "runer", "analit", "zcxvcb"))
			//{
			//    foreach (Task task in toTaskService.RootFolder.Tasks)
			//    {
			//        int generalReportCode = 0;
			//        if (task.Name.StartsWith("GR") && (task.Name.Length > 2)
			//            && (!task.Name.Equals("GR1", StringComparison.OrdinalIgnoreCase))
			//            && int.TryParse(task.Name.Substring(2), out generalReportCode))
			//        {
			//            TaskDefinition updateTaskDefinition = task.Definition;

			//            updateTaskDefinition.Settings.AllowDemandStart = true;

			//            updateTaskDefinition.Settings.AllowHardTerminate = true;

			//            updateTaskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(1);

			//            toTaskService.RootFolder.RegisterTaskDefinition(
			//                task.Path,
			//                updateTaskDefinition,
			//                TaskCreation.Update,
			//                "analit\\runer",
			//                "zcxvcb",
			//                TaskLogonType.Password,
			//                null);
			//        }
			//    }
			//}
		}

		[Test]
		public void FolderTest()
		{
			using (TaskService toTaskService = new TaskService()) {
				try {
					TaskFolder _notExists = toTaskService.GetFolder("fjdkfjdlfjdlj");
					Assert.Fail("Метод GetFolder должен был вернуть исключение, а он вернул объект {0}", _notExists);
				}
				catch (System.IO.FileNotFoundException) {
				}
				catch (Exception exception) {
					Assert.Fail("Метод GetFolder вернул неожидаемое исключение {0}", exception);
				}

				TaskFolder _reportsFolder = toTaskService.GetFolder("Отчеты");
				Assert.That(_reportsFolder.Name, Is.EqualTo("Отчеты"), "Имя папки не совпадает");
			}
		}

		[Test(Description = "Перенос задач из корневой папки в папку Отчеты и установка автора у задачи")]
		public void MoveTasks()
		{
			return;

			//using (TaskService toTaskService = new TaskService("offdc", "runer", "analit", "zcxvcb"))
			//{
			//    TaskFolder _reportsFolder = toTaskService.GetFolder("Отчеты");

			//    foreach (Task task in toTaskService.RootFolder.Tasks)
			//    {
			//        int generalReportCode = 0;
			//        if (task.Name.StartsWith("GR") && (task.Name.Length > 2)
			//            && int.TryParse(task.Name.Substring(2), out generalReportCode))
			//        {
			//            TaskDefinition moveTaskDefinition = task.Definition;
			//            moveTaskDefinition.RegistrationInfo.Author = "analit\\runer";
			//            moveTaskDefinition.RegistrationInfo.Date = DateTime.Now;

			//            _reportsFolder.RegisterTaskDefinition(
			//                task.Name,
			//                moveTaskDefinition,
			//                TaskCreation.Create,
			//                "analit\\runer",
			//                "zcxvcb",
			//                TaskLogonType.Password,
			//                null);

			//            toTaskService.RootFolder.DeleteTask(task.Name);
			//        }
			//    }
			//}
		}

		[Test(Description = "Проверка работы метода First")]
		public void TestFirstInFolders()
		{
			using (TaskService toTaskService = new TaskService()) {
				using (TaskFolder reportsFolder = toTaskService.GetFolder("Отчеты")) {
					try {
						Task updateTask = reportsFolder.Tasks.First(
							task => task.Name.Equals("GR dsdshdhskhd", StringComparison.OrdinalIgnoreCase));
						Assert.Fail("Нашли задачу, которую не должны были найти");
					}
					catch (InvalidOperationException) {
						//Задачу не нашли и получили исключение
					}
				}
			}
		}

		[Test(Description = "Изменяем имя запускаемого файла")]
		public void ReplaceExecuteAction()
		{
			return;

			//using (TaskService toTaskService = new TaskService("offdc", "runer", "analit", "zcxvcb"))
			//{
			//    TaskFolder _reportsFolder = toTaskService.GetFolder("Отчеты");

			//    foreach (Task task in _reportsFolder.Tasks)
			//    {
			//        int generalReportCode = 0;
			//        if (task.Name.StartsWith("GR") && (task.Name.Length > 2)
			//            && int.TryParse(task.Name.Substring(2), out generalReportCode))
			//        {
			//            TaskDefinition moveTaskDefinition = task.Definition;

			//            if ((moveTaskDefinition.Actions.Count > 0) && (moveTaskDefinition.Actions[0] is ExecAction))
			//                ((ExecAction)moveTaskDefinition.Actions[0]).Path = ((ExecAction)moveTaskDefinition.Actions[0]).Path.Replace("ReportSystem", "ReportSystemBoot");

			//            _reportsFolder.RegisterTaskDefinition(
			//                task.Name,
			//                moveTaskDefinition,
			//                TaskCreation.Update,
			//                "analit\\runer",
			//                "zcxvcb",
			//                TaskLogonType.Password,
			//                null);
			//        }
			//    }
			//}
		}
	}
}