using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Schedule;
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
			var currentTask = ScheduleHelper.GetTaskOrCreate(taskService, reportsFolder, 1, "Это тестовый отчет Морозова (Рейтинг)", "GR");
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

		[Test]
		public void FolderTest()
		{
			using (var toTaskService = new TaskService()) {
				try {
					var notExists = toTaskService.GetFolder("fjdkfjdlfjdlj");
					Assert.Fail("Метод GetFolder должен был вернуть исключение, а он вернул объект {0}", notExists);
				}
				catch (System.IO.FileNotFoundException) {
				}
				catch (Exception exception) {
					Assert.Fail("Метод GetFolder вернул неожидаемое исключение {0}", exception);
				}

				var reportsFolder = toTaskService.GetFolder("Отчеты");
				Assert.That(reportsFolder.Name, Is.EqualTo("Отчеты"), "Имя папки не совпадает");
			}
		}

		[Test(Description = "Проверка работы метода First")]
		public void TestFirstInFolders()
		{
			using (var toTaskService = new TaskService()) {
				using (var reportsFolder = toTaskService.GetFolder("Отчеты")) {
					try {
						var updateTask = reportsFolder.Tasks.First(
							task => task.Name.Equals("GR dsdshdhskhd", StringComparison.OrdinalIgnoreCase));
						Assert.Fail("Нашли задачу, которую не должны были найти");
					}
					catch (InvalidOperationException) {
						//Задачу не нашли и получили исключение
					}
				}
			}
		}
	}
}