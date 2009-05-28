using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Microsoft.Win32.TaskScheduler;
using MySql.Data.MySqlClient;

namespace ReportTuner.Test
{
	[TestFixture]
	public class TaskSchedulerFixture
	{

		[Test]
		public void SimpleConnectTest()
		{
			//using (TaskService taskService = new TaskService("offdcold", "morozov_sam", "analit", "LtAtylth7", true))
			//{
			//    foreach (Task task in taskService.RootFolder.Tasks)
			//    {
			//        Console.WriteLine("task name : {0}", task.Name);
			//    }
			//}

			//using (TaskService taskService = new TaskService("offdc", "morozov_sam", "analit", "LtAtylth7"))
			//{
			//    foreach (Task task in taskService.RootFolder.Tasks)
			//    {
			//        Console.WriteLine("task name : {0}", task.Name);
			//    }
			//}

			using (TaskService taskService = new TaskService("offdc", "runer", "analit", "zcxvcb"))
			{
				foreach (Task task in taskService.RootFolder.Tasks)
				{
					Console.WriteLine("task name : {0}", task.Name);
				}
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

			using (TaskService fromTaskService = new TaskService("offdcold", "morozov_sam", "analit", "LtAtylth7", true))
			{

				using (TaskService toTaskService = new TaskService("offdc", "runer", "analit", "zcxvcb"))
				{
					int allCount = 0, emptyTriggerCount = 0, standartTriggerCount = 0, anotherTriggerCount = 0, existsCount = 0;
					foreach (Task task in fromTaskService.RootFolder.Tasks)
					{
						int generalReportCode = 0;
						if (task.Name.StartsWith("GR") && (task.Name.Length > 2)
							&& int.TryParse(task.Name.Substring(2), out generalReportCode))
						{
							allCount++;

							if (MySqlHelper.ExecuteScalar(
								connectionString,
								"select GeneralReportCode from reports.general_reports g WHERE g.GeneralReportCode = ?GeneralReportCode",
								new MySqlParameter("?GeneralReportCode", generalReportCode)) != null)
							{
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

								if (task.Definition.Triggers.Count == 0)
								{
									emptyTriggerCount++;
								}
								else
									if ((task.Definition.Triggers.Count == 1) && (task.Definition.Triggers[0] is WeeklyTrigger))
									{
										newTaskDefinition.Triggers.Add(new WeeklyTrigger()
										{
											DaysOfWeek = ((WeeklyTrigger)task.Definition.Triggers[0]).DaysOfWeek,
											StartBoundary = ((WeeklyTrigger)task.Definition.Triggers[0]).StartBoundary,
											WeeksInterval = ((WeeklyTrigger)task.Definition.Triggers[0]).WeeksInterval
										});
										standartTriggerCount++;
									}
									else
									{
										Console.WriteLine("task name : {0}", task.Name);
										anotherTriggerCount++;
										string triggers = "   triggers : ";
										foreach (Trigger trigger in task.Definition.Triggers)
										{
											triggers += String.Format("{0} {{{1}}}", trigger.GetType().Name, trigger.ToString());
											if (trigger is WeeklyTrigger)
											{
												throw new Exception("Этого не должно было быть");
												newTaskDefinition.Triggers.Add(new WeeklyTrigger()
												{
													DaysOfWeek = ((WeeklyTrigger)trigger).DaysOfWeek,
													StartBoundary = ((WeeklyTrigger)trigger).StartBoundary,
													WeeksInterval = ((WeeklyTrigger)trigger).WeeksInterval
												});
											}
											else
												if (trigger is MonthlyTrigger)
												{
													newTaskDefinition.Triggers.Add(new MonthlyTrigger()
													{
														DaysOfMonth = ((MonthlyTrigger)trigger).DaysOfMonth,
														MonthsOfYear = ((MonthlyTrigger)trigger).MonthsOfYear,
														StartBoundary = ((MonthlyTrigger)trigger).StartBoundary
													});
												}
												else
													throw new Exception("Этого не должно было быть");

										}
										Console.WriteLine(triggers);
									}

								// Register the task in the root folder
								Task newTask = toTaskService.RootFolder.RegisterTaskDefinition(task.Name, newTaskDefinition);
								newTask.Enabled = false;

							}
							else
							{
								//Console.WriteLine("not exists : {0}", task.Name);
							}
						}
					}

					Console.WriteLine("statistic allCount = {0}, emptyTriggerCount = {1}, standartTriggerCount = {2}, anotherTriggerCount = {3}, existsCount = {4}", allCount, emptyTriggerCount, standartTriggerCount, anotherTriggerCount, existsCount);
				}
			}
		}

		[Test]
		public void FindTaskTest()
		{
			using (TaskService toTaskService = new TaskService("offdc", "runer", "analit", "zcxvcb"))
			{
				Task gr1Task = toTaskService.GetTask("GR1");
				Assert.IsNotNull(gr1Task, "Проблема, не нашли отчет с номером 1");
				try
				{
					Task notFoundTask = toTaskService.GetTask("GR_dsjfhdkhffdj");
					Assert.Fail("Метод GetTask должен был вернуть исключение, а он вернул объект {0}", notFoundTask);
				}
				catch (System.IO.FileNotFoundException)
				{
				}
				catch (Exception exception)
				{
					Assert.Fail("Метод GetTask вернул неожидаемое исключение {0}", exception);
				}
			}
		}

		[Test]
		public void ClearAllTriggers()
		{
			using (TaskService toTaskService = new TaskService())
			{
				Task gr1Task = toTaskService.GetTask("GR1");

				TaskDefinition taskDefinition = gr1Task.Definition;

				//taskDefinition.Settings.AllowDemandStart = true;

				//taskDefinition.Settings.AllowHardTerminate = true;

				//taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(1);


				//WeeklyTrigger trigger = new WeeklyTrigger();
				//trigger.DaysOfWeek = DaysOfTheWeek.Monday;
				//trigger.StartBoundary = DateTime.Now;
				//trigger.WeeksInterval = 1;

				//taskDefinition.Triggers.Add(trigger);

				//toTaskService.RootFolder.RegisterTaskDefinition(gr1Task.Path, taskDefinition, TaskCreation.Update, "analit\\runer", "zcxvcb", TaskLogonType.Password, null);
				//toTaskService.RootFolder.RegisterTaskDefinition(gr1Task.Path, taskDefinition, TaskCreation.Update, "system", null, TaskLogonType.ServiceAccount, null);
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
			using (TaskService toTaskService = new TaskService())
			{
				try
				{
					TaskFolder _notExists = toTaskService.GetFolder("fjdkfjdlfjdlj");
					Assert.Fail("Метод GetFolder должен был вернуть исключение, а он вернул объект {0}", _notExists);
				}
				catch (System.IO.FileNotFoundException)
				{
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception);
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

	}

}
