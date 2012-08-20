using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models;
using Common.Web.Ui.NHibernateExtentions;
using Microsoft.Win32.TaskScheduler;
using MySql.Data.MySqlClient;
using ReportTuner.Models;

namespace MigrationTasks
{
	internal class FileProp
	{
		public uint PropId { get; set; }
		public uint FileId { get; set; }
	}

	internal class Program
	{
		private static void Main(string[] args)
		{
			//Мигрируем настройки отчетов с offdc на fms
			var connectionString = "Database=usersettings;Data Source=sql2.analit.net;Port=3306;User Id=ReportsSystem;Password=samepass;pooling=false;default command timeout=0; Allow user variables=true;convert zero datetime=yes;";
			ActiveRecordInitialize.Init("release", typeof(Report).Assembly, typeof(ContactGroup).Assembly);
		}

		private static void MoveAdditionFiles()
		{
			var dirPath = @"\\acdcserv\WebApps\Data\Reports";
			//var dirPath = string.Empty;
			var files = ArHelper.WithSession(s => s.CreateSQLQuery(@"SELECT r.Id as PropId, f.Id FileId FROM reports.report_properties r
join reports.reports rp on  rp.ReportCode = r.ReportCode
join reports.filessendwithreport f on f.Report = rp.GeneralReportCode
where PropertyId = 438;").ToList<FileProp>());
			foreach (var fileProp in files) {
				var from = Path.Combine(dirPath, fileProp.PropId.ToString());
				var to = Path.Combine(dirPath, fileProp.FileId.ToString());
				File.Copy(from, to);
			}
		}

		private void MigrationTask(string connectionString)
		{
			//Если хотим, чтобы что-то сделалось, то надо убрать return

			using (TaskService fromTaskService = new TaskService("offdc", "runer", "analit", "zcxvcb")) {
				TaskFolder _reportsFolder = fromTaskService.GetFolder("Отчеты");

				using (TaskService toTaskService = new TaskService("fms", "runer", "analit", "zcxvcb")) {
					TaskFolder newFolder = toTaskService.GetFolder("Отчеты");

					int allCount = 0, emptyTriggerCount = 0, standartTriggerCount = 0, anotherTriggerCount = 0, existsCount = 0;
					foreach (Task task in _reportsFolder.Tasks) {
						int generalReportCode = 0;
						if (task.Name.StartsWith("GR") && (task.Name.Length > 2)
							&& int.TryParse(task.Name.Substring(2), out generalReportCode)) {
							allCount++;

							if (MySqlHelper.ExecuteScalar(
								connectionString,
								"select GeneralReportCode from reports.general_reports g WHERE g.GeneralReportCode = ?GeneralReportCode",
								new MySqlParameter("?GeneralReportCode", generalReportCode)) != null) {
								existsCount++;

								//Console.WriteLine("taskName: {0}\r\n{1}\r\n{2}", task.Name, task.Xml, task.Definition.XmlText);

								// Create a new task definition and assign properties
								TaskDefinition newTaskDefinition = toTaskService.NewTask();
								newTaskDefinition.XmlText = task.Definition.XmlText;

								Task newTask =
									newFolder.RegisterTaskDefinition(
										task.Name,
										newTaskDefinition,
										TaskCreation.Create,
										"analit\\runer",
										"zcxvcb",
										TaskLogonType.Password,
										null);
								newTask.Enabled = task.Enabled;
							}
							else {
								Console.WriteLine("not exists : {0}", task.Name);
							}
						}
					}

					Console.WriteLine("statistic allCount = {0}, emptyTriggerCount = {1}, standartTriggerCount = {2}, anotherTriggerCount = {3}, existsCount = {4}", allCount, emptyTriggerCount, standartTriggerCount, anotherTriggerCount, existsCount);
				}
			}
		}
	}
}