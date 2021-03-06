using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Common.MySql;
using Common.Schedule;
using Common.Tools;
using Common.Web.Ui.Helpers;
using MySql.Data.MySqlClient;
using Microsoft.Win32.TaskScheduler;
using System.Threading;
using NHibernate;
using NHibernate.Linq;
using ReportTuner.Models;
using Task = Microsoft.Win32.TaskScheduler.Task;

public partial class Reports_schedule : BasePage
{
	private MySqlConnection MyCn = new MySqlConnection(ConnectionHelper.GetConnectionString());
	private MySqlCommand MyCmd = new MySqlCommand();
	private MySqlDataAdapter MyDA = new MySqlDataAdapter();

	private GeneralReport _generalReport;

	private TaskService taskService;
	private TaskFolder reportsFolder;

	private DataSet DS;
	private DataTable dtSchedule;
	private DataTable dtScheduleMonth;
	private DataColumn SWeek;
	private DataColumn SMonday;
	private DataColumn STuesday;
	private DataColumn SWednesday;
	private DataColumn SThursday;
	private DataColumn SFriday;
	private DataColumn SSaturday;
	private DataColumn SSunday;
	private Task currentTask;
	private Task tempTask;
	private TaskDefinition currentTaskDefinition;
	private DaysOfTheWeek triggerDays = 0;
	private DataColumn SStartHour;
	private DataColumn SStartMinute;

	private DataColumn MSStartHour;
	private DataColumn MSStartMinute;

	private const string DSSchedule = "Inforoom.Reports.Schedule.DSSchedule";

	private const string StatusRunning = "Выполнить задание";
	private const string StatusNotRunning = "Выполняется...";

	protected void Page_Init(object sender, EventArgs e)
	{
		InitializeComponent();
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		if ((Request.UrlReferrer == null || !Request.UrlReferrer.LocalPath.Contains("Schedule.aspx")) && Session["StartTaskTime"] != null)
			Session.Remove("StartTaskTime");

		if (Request["r"] == null)
			Response.Redirect("GeneralReports.aspx");

		_generalReport = GeneralReport.Find(Convert.ToUInt64(Request["r"]));

		taskService = ScheduleHelper.GetService();
		reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
		currentTask = ScheduleHelper.GetTaskOrCreate(taskService, reportsFolder, _generalReport.Id, _generalReport.Comment, "GR");
		currentTaskDefinition = currentTask.Definition;

		tempTask = ScheduleHelper.FindTaskNullable(reportsFolder, _generalReport.Id, "temp_");
		TaskState tempTaskState;
		string tempTaskDescription = string.Empty;
		if (tempTask != null) {
			tempTaskState = tempTask.State;
			tempTaskDescription = tempTask.Definition.RegistrationInfo.Description;
		}
		else
			tempTaskState = TaskState.Unknown;

		btnExecute.Enabled = currentTask.State != TaskState.Running && tempTaskState != TaskState.Running;
		btnExecute.Text = (currentTask.State == TaskState.Running) ? StatusNotRunning : StatusRunning;

		var userName = HttpContext.Current.User.Identity.Name.Replace(@"ANALIT\", string.Empty);

		ErrorMassage.Text = string.Empty;
		ErrorMassage.CssClass = "error";

		var description = tempTaskState == TaskState.Running ? string.Format("(запустил: {0})", tempTaskDescription) : string.Empty;

		if (tempTaskState == TaskState.Running || currentTask.State == TaskState.Running) {
			ExecAction action = null;
			var currentReportNumber = "";
			ulong runningNumber = 0;
			if(tempTaskState == TaskState.Running) {
				if (tempTask != null)
					action = (ExecAction)tempTask.Definition.Actions.FirstOrDefault();
			}
			else {
				action = (ExecAction)currentTask.Definition.Actions.FirstOrDefault();
			}
			if (action != null) {
				var arguments = (action).Arguments;
				if(!String.IsNullOrEmpty(arguments)) {
					if(arguments.IndexOf("/gr:") >= 0) {
						var substring = arguments.Substring(arguments.IndexOf("/gr:") + 4);
						var numberLength = substring.IndexOf(@" /");
						var reportNumber = substring.Substring(0, numberLength != -1 ? numberLength : substring.Length);
						if (!String.IsNullOrEmpty(reportNumber)) {
							currentReportNumber += " № ";
							currentReportNumber += reportNumber;
							ulong.TryParse(reportNumber, out runningNumber);
						}
					}
				}
			}
			var startTime = GetStartTime(DbSession, runningNumber != 0 ? runningNumber : _generalReport.Id);

			var prefix = tempTaskState == TaskState.Running ? String.Format("Успешно запущен разовый отчет{0}", currentReportNumber)
				: String.Format("Отчет запущен ({0})", currentReportNumber);
			if (tempTaskDescription == userName || currentTask.State == TaskState.Running) {
				ErrorMassage.Text = string.Format("{0}, ожидайте окончания выполнения операции. {1}", prefix, startTime);
				ErrorMassage.BackColor = Color.LightGreen;
			}
			else {
				ErrorMassage.Text = String.Format("{1}, выполнение данного отчета отложено {0}. {2}", description, prefix, startTime);
				ErrorMassage.BackColor = Color.Red;
			}
			btn_Mailing.Enabled = false;
			RadioSelf.Enabled = false;
			RadioMails.Enabled = false;
		}
		if (tempTaskState == TaskState.Queued || currentTask.State == TaskState.Queued) {
			var prefix = tempTaskState == TaskState.Running ? "Запускается разовый отчет" : "Отчет запускается";
			if (tempTaskDescription == userName || currentTask.State == TaskState.Queued) {
				ErrorMassage.Text = string.Format("{0}, ожидайте окончания выполнения операции", prefix);
				ErrorMassage.BackColor = Color.LightGreen;
			}
			else {
				ErrorMassage.Text = string.Format("{1} {0}, выполнение данного отчета отложено)", description, prefix);
				ErrorMassage.BackColor = Color.Red;
			}
			btn_Mailing.Enabled = false;
			RadioSelf.Enabled = false;
			RadioMails.Enabled = false;
		}
		if ((tempTaskState == TaskState.Ready && currentTask.State != TaskState.Running && currentTask.State != TaskState.Queued) ||
			(currentTask.State == TaskState.Ready && tempTaskState != TaskState.Running && tempTaskState != TaskState.Queued)) {
			if (tempTaskDescription == userName || currentTask.State == TaskState.Ready) {
				// отчет выполнен
				if (Session["StartTaskTime"] != null) {
					Session.Remove("StartTaskTime");
					ErrorMassage.Text = "Операция выполнена";
					ErrorMassage.BackColor = Color.LightGreen;
				}
				else
					ErrorMassage.Text = "";
			}
		}

		if ((tempTaskState == TaskState.Disabled && currentTask.State != TaskState.Running && currentTask.State != TaskState.Queued) ||
			(currentTask.State == TaskState.Disabled && tempTaskState != TaskState.Running && tempTaskState != TaskState.Queued)) {
			if (Session["StartTaskTime"] != null) {
				Session.Remove("StartTaskTime");
				ErrorMassage.Text = "Операция отменена";
				ErrorMassage.BackColor = Color.Red;
			}
			else
				ErrorMassage.Text = "";
		}

		var otherTriggers = new List<Trigger>();
		if (!IsPostBack) {
			var selfMail = GetSelfEmails();
			if ((selfMail.Count != 0) && (selfMail[0].Length != 0)) {
				RadioSelf.Text = "Выполнить и отослать на: " + selfMail[0][0];
			}

			dtFrom.Value = DateTime.Now.AddDays(-7).ToShortDateString();
			dtTo.Value = DateTime.Now.ToShortDateString();
			mail_Text.Text = GetMailingAdresses();

			try {
				lblClient.Text = _generalReport.Payer.Id + " - " + _generalReport.Payer.ShortName;
				lblReportComment.Text = _generalReport.Comment;
				var lastLogTimes = ObjectFromQuery(new[] { new MySqlParameter("?GeneralReportCode", _generalReport.Id) },
					@"
SELECT
  Max(LogTime) as MaxLogTime
FROM
  logs.reportslogs
WHERE
  reportslogs.GeneralReportCode = ?GeneralReportCode
");
				if ((lastLogTimes.Count > 0) && (lastLogTimes[0].Length > 0))
					if (lastLogTimes[0][0] is DateTime) {
						MyCn.Open();
						MyCmd.CommandText = @"
SELECT
  LogTime,
  EMail,
  SMTPID
FROM
  logs.reportslogs
WHERE
	reportslogs.GeneralReportCode = ?GeneralReportCode
and reportslogs.LogTime > ?LastLogTime
order by LogTime desc
";
						MyCmd.Parameters.AddWithValue("?LastLogTime", ((DateTime)lastLogTimes[0][0]).AddDays(-1).Date);
						var _logs = new DataTable();
						MyDA.Fill(_logs);
						gvLogs.DataSource = _logs;
					}
				gvLogs.DataBind();

				MyCmd.Parameters.Clear();
				MyCmd.CommandText = @"select
rel.StartTime,
if (not EndError, rel.EndTime, 'Ошибка при формировании отчета') as EndTime
from `logs`.reportexecutelogs rel
where rel.GeneralReportCode = ?GeneralReportCode
order by StartTime desc
limit 15;";
				MyCmd.Parameters.AddWithValue("?GeneralReportCode", _generalReport.Id);

				var startlogs = new DataTable();
				MyDA.Fill(startlogs);
				startLogs.DataSource = startlogs;

				startLogs.DataBind();
			}
			finally {
				MyCn.Close();
			}

			chbAllow.Checked = currentTask.Enabled;
			lblWork.Text = ((ExecAction)currentTask.Definition.Actions[0]).Path + " " + ((ExecAction)currentTask.Definition.Actions[0]).Arguments;
			lblFolder.Text = ((ExecAction)currentTask.Definition.Actions[0]).WorkingDirectory;
			if (_generalReport.FirmCode != null) {
				var ftpId = _generalReport.FirmCode.ToString().PadLeft(3, '0');
				FtpPath.Text = $"ftp://ftp.analit.net/OptBox/{ftpId}/Reports/";
			} else {
				FtpPath.Text = "";
			}
			var tl = currentTask.Definition.Triggers;

			for (int i = 0; i < tl.Count; i++) {
				if (tl[i] is WeeklyTrigger) {
					var dr = DS.Tables[dtSchedule.TableName].NewRow();
					var trigger = ((WeeklyTrigger)tl[i]);
					dr[SStartHour.ColumnName] = trigger.StartBoundary.Hour;
					dr[SStartMinute.ColumnName] = trigger.StartBoundary.Minute;
					var days = trigger.DaysOfWeek;

					SetWeekDays(dr, DaysOfTheWeek.Monday, days);
					SetWeekDays(dr, DaysOfTheWeek.Tuesday, days);
					SetWeekDays(dr, DaysOfTheWeek.Wednesday, days);
					SetWeekDays(dr, DaysOfTheWeek.Thursday, days);
					SetWeekDays(dr, DaysOfTheWeek.Friday, days);
					SetWeekDays(dr, DaysOfTheWeek.Saturday, days);
					SetWeekDays(dr, DaysOfTheWeek.Sunday, days);

					DS.Tables[dtSchedule.TableName].Rows.Add(dr);
				}
				else if (tl[i] is MonthlyTrigger) {
					var dr = DS.Tables[dtScheduleMonth.TableName].NewRow();
					//очищаем таблицу от значений по умолчанию
					for (var k = 1; k <= 31; k++)
						dr["d" + k] = 0;
					for (var k = 1; k <= 12; k++)
						dr["m" + k] = 0;

					var trigger = ((MonthlyTrigger)tl[i]);
					dr[MSStartHour.ColumnName] = trigger.StartBoundary.Hour;
					dr[MSStartMinute.ColumnName] = trigger.StartBoundary.Minute;
					var months = trigger.MonthsOfYear;
					MonthsOfTheYear month;
					for (int j = 0; j < 12; j++) {
						MonthsOfTheYear.TryParse((1 << j).ToString(), true, out month);
						if (months.HasFlag(month))
							dr["m" + (j + 1)] = 1;
					}
					foreach (int em in trigger.DaysOfMonth) {
						dr["d" + em] = 1;
					}
					DS.Tables[dtScheduleMonth.TableName].Rows.Add(dr);
				}
				else
					otherTriggers.Add(tl[i]);
			}

			DS.Tables[dtSchedule.TableName].AcceptChanges();
			dgvSchedule.DataSource = DS;
			dgvSchedule.DataMember = dtSchedule.TableName;
			dgvSchedule.DataBind();

			dgvScheduleMonth.DataSource = DS;
			dgvScheduleMonth.DataMember = dtScheduleMonth.TableName;
			dgvScheduleMonth.DataBind();

			gvOtherTriggers.DataSource = otherTriggers;
			gvOtherTriggers.DataBind();

			Session[DSSchedule] = DS;

			CloseTaskService();
		}
		else {
			DS = ((DataSet)Session[DSSchedule]);
			if (DS == null) // вероятно, сессия завершилась и все ее данные утеряны
				Reports_GeneralReports.Redirect(this);
		}

		send_created_report.Visible = _generalReport.IsSuccessfulyProcessed;
	}

	public static string GetStartTime(ISession session, ulong grId)
	{
		var executeLogs = session.Query<ReportExecuteLog>().Where(l => l.GeneralReportCode == grId).OrderByDescending(l => l.StartTime).ToList();
		var normalLanches = executeLogs.Where(l => l.EndTime != null).ToList();
		var avgExTime = normalLanches.Sum(l => (l.EndTime.Value - l.StartTime).TotalMinutes / normalLanches.Count);
		var executeLog = executeLogs.FirstOrDefault(l => l.EndTime == null);
		var startTime = executeLog != null ? executeLog.StartTime.ToString() : string.Empty;
		startTime = string.IsNullOrEmpty(startTime) ? startTime : string.Format("Отчет запущен {0}. ", startTime);
		if (avgExTime > 0)
			startTime += string.Format("Среднее время выполнения: {0} минут", avgExTime.ToString("0.0"));
		return startTime;
	}

	private List<object[]> ObjectFromQuery(MySqlParameter[] parameters, string commandText)
	{
		var result = new List<object[]>();
		if (MyCn.State == ConnectionState.Closed)
			MyCn.Open();
		try {
			MyCmd.Connection = MyCn;
			MyCmd.CommandText = commandText;
			MyDA.SelectCommand = MyCmd;

			MyCmd.Parameters.Clear();
			MyCmd.Parameters.AddRange(parameters);

			var MyReader = MyCmd.ExecuteReader();
			while (MyReader.Read()) {
				var temp = new object[MyReader.FieldCount];
				MyReader.GetValues(temp);
				result.Add(temp);
			}
		}
		finally {
			MyCn.Close();
		}
		return result;
	}

	private void SetWeekDays(DataRow dr, DaysOfTheWeek weekDay, DaysOfTheWeek days)
	{
		string column = "S" + weekDay.ToString();
		if ((weekDay & days) == weekDay)
			dr[column] = 1;
		else
			dr[column] = 0;
	}

	private bool CheckGridTimeValue(GridView grid)
	{
		foreach (GridViewRow drv in grid.Rows) {
			var time = ((TextBox)drv.FindControl("tbStart")).Text;
			var h = int.Parse(time.Substring(0, time.IndexOf(':')));
			var m = int.Parse(time.Substring(time.IndexOf(':') + 1, time.Length - time.IndexOf(':') - 1));
			if((h >= 0 && h < 4) || h == 23 || (h == 4 && m == 0)) {
				ErrorMassage.Text = "Временной промежуток от 23:00 до 4:00 является недопустимым для времени выполнения отчета";
				ErrorMassage.BackColor = Color.Red;
				return false;
			}
		}
		return true;
	}

	private bool CheckTimeValue()
	{
		return CheckGridTimeValue(dgvSchedule) && CheckGridTimeValue(dgvScheduleMonth);
	}

	protected void btnApply_Click(object sender, EventArgs e)
	{
		if (this.IsValid) {
			if(!CheckTimeValue())
				return;
			CopyChangesToTable();
			CopyMonthTriggerValuesInToTable();

			SaveTriggers();

			SaveTaskChanges();
		}

		CloseTaskService();
	}

	private void CopyChangesToTable()
	{
		DS.Tables[dtSchedule.TableName].Rows.Clear();
		foreach (GridViewRow drv in dgvSchedule.Rows) {
			DataRow dr = DS.Tables[dtSchedule.TableName].NewRow();
			string h = ((TextBox)drv.FindControl("tbStart")).Text;
			string m = ((TextBox)drv.FindControl("tbStart")).Text.Substring(h.IndexOf(':') + 1, h.Length - h.IndexOf(':') - 1);
			if (m.StartsWith("0"))
				m = m.Substring(1, 1);

			dr[SStartHour.ColumnName] = Convert.ToInt16(h.Substring(0, h.IndexOf(':')));
			dr[SStartMinute.ColumnName] = Convert.ToInt16(m);
			dr[SMonday.ColumnName] = Convert.ToByte(((CheckBox)drv.FindControl("chbMonday")).Checked);
			dr[STuesday.ColumnName] = Convert.ToByte(((CheckBox)drv.FindControl("chbTuesday")).Checked);
			dr[SWednesday.ColumnName] = Convert.ToByte(((CheckBox)drv.FindControl("chbWednesday")).Checked);
			dr[SThursday.ColumnName] = Convert.ToByte(((CheckBox)drv.FindControl("chbThursday")).Checked);
			dr[SFriday.ColumnName] = Convert.ToByte(((CheckBox)drv.FindControl("chbFriday")).Checked);
			dr[SSaturday.ColumnName] = Convert.ToByte(((CheckBox)drv.FindControl("chbSaturday")).Checked);
			dr[SSunday.ColumnName] = Convert.ToByte(((CheckBox)drv.FindControl("chbSunday")).Checked);
			DS.Tables[dtSchedule.TableName].Rows.Add(dr);
		}
		DS.Tables[dtSchedule.TableName].AcceptChanges();
	}

	private void SaveTaskChanges()
	{
		currentTaskDefinition.Settings.Enabled = chbAllow.Checked;
		_generalReport.Allow = chbAllow.Checked;
		_generalReport.Save();

		btnExecute.Enabled = currentTask.State != TaskState.Running && currentTask.State != TaskState.Running;
		btnExecute.Text = (currentTask.State == TaskState.Running) ? StatusNotRunning : StatusRunning;

		ScheduleHelper.UpdateTaskDefinition(taskService, reportsFolder, _generalReport.Id, currentTaskDefinition, "GR");
	}

	private void SaveTriggers()
	{
		for (int i = currentTaskDefinition.Triggers.Count - 1; i >= 0; i--) {
			if (currentTaskDefinition.Triggers[i] is WeeklyTrigger) {
				currentTaskDefinition.Triggers.RemoveAt(i);
				continue;
			}
			if (currentTaskDefinition.Triggers[i] is MonthlyTrigger)
				currentTaskDefinition.Triggers.RemoveAt(i);
		}

		foreach (DataRow dr in DS.Tables[dtSchedule.TableName].Rows) {
			short h = Convert.ToInt16(dr[SStartHour.ColumnName]);
			short m = Convert.ToInt16(dr[SStartMinute.ColumnName]);

			triggerDays = 0;
			AddDay(dr, DaysOfTheWeek.Monday);
			AddDay(dr, DaysOfTheWeek.Tuesday);
			AddDay(dr, DaysOfTheWeek.Wednesday);
			AddDay(dr, DaysOfTheWeek.Thursday);
			AddDay(dr, DaysOfTheWeek.Friday);
			AddDay(dr, DaysOfTheWeek.Saturday);
			AddDay(dr, DaysOfTheWeek.Sunday);

			var trigger = (WeeklyTrigger)currentTaskDefinition.Triggers.AddNew(TaskTriggerType.Weekly);
			trigger.DaysOfWeek = triggerDays;
			trigger.WeeksInterval = 1;
			trigger.StartBoundary = DateTime.Now.Date.AddHours(h).AddMinutes(m);
		}

		foreach (DataRow dr in DS.Tables[dtScheduleMonth.TableName].Rows) {
			var trigger = (MonthlyTrigger)currentTaskDefinition.Triggers.AddNew(TaskTriggerType.Monthly);
			short h = Convert.ToInt16(dr[MSStartHour.ColumnName]);
			short m = Convert.ToInt16(dr[MSStartMinute.ColumnName]);

			MonthsOfTheYear month;
			MonthsOfTheYear allmonth = 0;
			for (int i = 1; i <= 12; i++) {
				if ((byte)dr["m" + i] > 0) {
					MonthsOfTheYear.TryParse((1 << (i - 1)).ToString(), true, out month);
					allmonth |= month;
				}
			}
			var dayInt = new List<int>();
			for (int i = 1; i <= 31; i++) {
				if ((byte)dr["d" + i] > 0) {
					dayInt.Add(i);
					//monthInt |= i;
				}
			}
			trigger.DaysOfMonth = dayInt.ToArray();

			trigger.MonthsOfYear = allmonth;
			trigger.StartBoundary = DateTime.Now.Date.AddHours(h).AddMinutes(m);
		}
	}

	private void AddDay(DataRow dr, DaysOfTheWeek weekDay)
	{
		string column = "S" + weekDay.ToString();
		if (dr[column].ToString() == "1") {
			if (triggerDays == 0)
				triggerDays = weekDay;
			else
				triggerDays = triggerDays | weekDay;
		}
	}
	#region Component Designer generated code
	private void InitializeComponent()
	{
		this.DS = new System.Data.DataSet();
		this.dtSchedule = new System.Data.DataTable();
		this.dtScheduleMonth = new DataTable();
		this.SWeek = new System.Data.DataColumn();
		this.SMonday = new System.Data.DataColumn();
		this.STuesday = new System.Data.DataColumn();
		this.SWednesday = new System.Data.DataColumn();
		this.SThursday = new System.Data.DataColumn();
		this.SFriday = new System.Data.DataColumn();
		this.SSaturday = new System.Data.DataColumn();
		this.SSunday = new System.Data.DataColumn();
		this.SStartHour = new System.Data.DataColumn();
		this.SStartMinute = new System.Data.DataColumn();
		this.MSStartHour = new System.Data.DataColumn();
		this.MSStartMinute = new System.Data.DataColumn();
		((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtSchedule)).BeginInit();
		//
		// DS
		//
		this.DS.DataSetName = "NewDataSet";
		this.DS.Tables.AddRange(new System.Data.DataTable[] {
			this.dtSchedule, dtScheduleMonth
		});
		//
		// dtSchedule
		//
		this.dtSchedule.Columns.AddRange(new System.Data.DataColumn[] {
			this.SWeek,
			this.SMonday,
			this.STuesday,
			this.SWednesday,
			this.SThursday,
			this.SFriday,
			this.SSaturday,
			this.SSunday,
			this.SStartHour,
			this.SStartMinute
		});

		var columnsForAdd = new List<DataColumn>();
		for (var i = 1; i <= 12; i++) {
			columnsForAdd.Add(new DataColumn("m" + i, typeof(byte)) { DefaultValue = ((byte)1) });
		}
		for (var i = 1; i <= 31; i++) {
			var val = i == 1 ? 1 : 0;
			columnsForAdd.Add(new DataColumn("d" + i, typeof(byte)) { DefaultValue = ((byte)val)});
		}

		dtScheduleMonth.Columns.AddRange(new[] { MSStartHour, MSStartMinute });

		dtScheduleMonth.Columns.AddRange(columnsForAdd.ToArray());

		this.dtSchedule.TableName = "dtSchedule";
		this.dtScheduleMonth.TableName = "dtScheduleMonth";
		//
		// SWeek
		//
		this.SWeek.ColumnName = "SWeek";
		this.SWeek.DataType = typeof(int);
		//
		// SMonday
		//
		this.SMonday.ColumnName = "SMonday";
		this.SMonday.DataType = typeof(byte);
		this.SMonday.DefaultValue = ((byte)(0));
		//
		// STuesday
		//
		this.STuesday.ColumnName = "STuesday";
		this.STuesday.DataType = typeof(byte);
		this.STuesday.DefaultValue = ((byte)(0));
		//
		// SWednesday
		//
		this.SWednesday.ColumnName = "SWednesday";
		this.SWednesday.DataType = typeof(byte);
		this.SWednesday.DefaultValue = ((byte)(0));
		//
		// SThursday
		//
		this.SThursday.ColumnName = "SThursday";
		this.SThursday.DataType = typeof(byte);
		this.SThursday.DefaultValue = ((byte)(0));
		//
		// SFriday
		//
		this.SFriday.ColumnName = "SFriday";
		this.SFriday.DataType = typeof(byte);
		this.SFriday.DefaultValue = ((byte)(0));
		//
		// SSaturday
		//
		this.SSaturday.ColumnName = "SSaturday";
		this.SSaturday.DataType = typeof(byte);
		this.SSaturday.DefaultValue = ((byte)(0));
		//
		// SSunday
		//
		this.SSunday.ColumnName = "SSunday";
		this.SSunday.DataType = typeof(byte);
		this.SSunday.DefaultValue = ((byte)(0));
		//
		// SStartHour
		//
		this.SStartHour.ColumnName = "SStartHour";
		this.SStartHour.DataType = typeof(short);
		this.SStartHour.DefaultValue = ((short)(0));
		//
		// SStartMinute
		//
		this.SStartMinute.ColumnName = "SStartMinute";
		this.SStartMinute.DataType = typeof(short);
		this.SStartMinute.DefaultValue = ((short)(0));

		this.MSStartHour.ColumnName = "MSStartHour";
		this.MSStartHour.DataType = typeof(short);
		this.MSStartHour.DefaultValue = ((short)(0));

		this.MSStartMinute.ColumnName = "MSStartMinute";
		this.MSStartMinute.DataType = typeof(short);
		this.MSStartMinute.DefaultValue = ((short)(0));
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtSchedule)).EndInit();
	}
	#endregion
	protected void dgvSchedule_RowCommand(object sender, GridViewCommandEventArgs e)
	{
		if (e.CommandName == "Add") {
			CopyChangesToTable();

			DataRow dr = DS.Tables[dtSchedule.TableName].NewRow();
			DS.Tables[dtSchedule.TableName].Rows.Add(dr);

			dgvSchedule.DataSource = DS;
			dgvSchedule.DataBind();
		}
	}

	protected void dgvSchedule_RowDeleting(object sender, GridViewDeleteEventArgs e)
	{
		CopyChangesToTable();
		DS.Tables[dtSchedule.TableName].DefaultView[e.RowIndex].Delete();
		dgvSchedule.DataSource = DS;
		dgvSchedule.DataBind();
	}

	protected void dgvScheduleMonth_RowDeleting(object sender, GridViewDeleteEventArgs e)
	{
		CopyMonthTriggerValuesInToTable();
		DS.Tables[dtScheduleMonth.TableName].DefaultView[e.RowIndex].Delete();
		dgvScheduleMonth.DataSource = DS;
		dgvScheduleMonth.DataBind();
	}

	protected void dgvSchedule_RowDataBound(object sender, GridViewRowEventArgs e)
	{
		RowDataBoundAll(e, SStartHour.ColumnName, SStartMinute.ColumnName);
	}

	protected void dgvSchedule_RowDataBoundMonth(object sender, GridViewRowEventArgs e)
	{
		RowDataBoundAll(e, MSStartHour.ColumnName, MSStartMinute.ColumnName);
	}

	protected void RowDataBoundAll(GridViewRowEventArgs e, string startHourColumnName, string startMinuteColumnName)
	{
		if (e.Row.RowType == DataControlRowType.DataRow) {
			TextBox tb = ((TextBox)e.Row.Cells[0].FindControl("tbStart"));
			tb.Text = ((DataRowView)e.Row.DataItem)[startHourColumnName].ToString() + ":" + ((DataRowView)e.Row.DataItem)[startMinuteColumnName].ToString().PadLeft(2, '0');
		}
	}


	protected bool Send_in_Emails()
	{
		if (IsValid) {
			var mails = mail_Text.Text.Split(',');
			for (int i = 0; i < mails.Length; i++) {
				mails[i] = mails[i].Trim(new[] { ' ', '\n', '\r' });
				var recordMail = new MailingAddresses {
					Mail = mails[i],
					GeneralReport = _generalReport
				};
				recordMail.SaveAndFlush();
			}

			return true;
		}
		return false;
	}

	protected void btnExecute_Click(object sender, EventArgs e)
	{
		var runed = false;
		if (IsValid && (currentTask.State != TaskState.Running)) {
			ScheduleHelper.SetTaskEnableStatus(_generalReport.Id, true, "GR");
			ScheduleHelper.SetTaskAction(_generalReport.Id, string.Format("/gr:{0} /manual:true", _generalReport.Id));
			currentTask.Run();
			Thread.Sleep(500);
			btnExecute.Enabled = false;
			btnExecute.Text = StatusNotRunning;
			runed = true;
		}

		CloseTaskService();
		Thread.Sleep(500);
		if (runed)
			Response.Redirect("Schedule.aspx?r=" + _generalReport.Id);
	}

	/// <summary>
	/// Закончили работу с TaskService
	/// </summary>
	private void CloseTaskService()
	{
		if (currentTask != null) {
			currentTask.Dispose();
			currentTask = null;
		}
		if (taskService != null) {
			taskService.Dispose();
			taskService = null;
		}
	}

	private void RunSelfTaskAndUpdateAction()
	{
		string user = HttpContext.Current.User.Identity.Name.Replace(@"ANALIT\", string.Empty);
		var thisTask = ScheduleHelper.GetTaskOrCreate(taskService, reportsFolder, Convert.ToUInt64(_generalReport.Id), user, "temp_");

		var newAction = new ExecAction(ScheduleHelper.ScheduleAppPath,
			"/gr:" + _generalReport.Id +
				string.Format(" /inter:true /dtFrom:{0} /dtTo:{1} /manual:true", DateTime.Parse(dtFrom.Value).ToShortDateString(), DateTime.Parse(dtTo.Value).ToShortDateString()),
			ScheduleHelper.ScheduleWorkDir);

		var taskDefinition = thisTask.Definition;

		taskDefinition.Actions.RemoveAt(0);
		taskDefinition.Actions.Add(newAction);
		taskDefinition.RegistrationInfo.Description = user;
		taskDefinition.Settings.RunOnlyIfIdle = false;
		taskDefinition.Settings.StopIfGoingOnBatteries = false;
		taskDefinition.Settings.DisallowStartIfOnBatteries = false;
		taskDefinition.Settings.StopIfGoingOnBatteries = false;
		ScheduleHelper.UpdateTaskDefinition(taskService, reportsFolder, Convert.ToUInt64(_generalReport.Id), taskDefinition, "temp_");

		if (thisTask.State != TaskState.Running) {
			thisTask.Run();
			Session.Add("StartTaskTime", DateTime.Now);
			Response.Redirect("Schedule.aspx?r=" + _generalReport.Id);
		}
	}

	protected void Send_self()
	{
		WriteEmailList(GetSelfEmails());
	}

	private List<object[]> GetSelfEmails()
	{
		var userName = HttpContext.Current.User.Identity.Name.Replace(@"ANALIT\", string.Empty);
		return ObjectFromQuery(new[] { new MySqlParameter("?userName", userName) },
			@"SELECT Email FROM accessright.regionaladmins r where r.UserName = ?userName");
	}

	private void WriteEmailList(List<object[]> emails)
	{
		foreach (var email in emails) {
			var recordMail = new MailingAddresses {
				Mail = email[0].ToString(),
				GeneralReport = _generalReport
			};
			recordMail.SaveAndFlush();
		}
	}

	/// <summary>
	/// Получение списка имейлов от рассылки, через запятую.
	/// Находит как имейлы из текущей рассылки, так и самостоятельные подписки.
	/// </summary>
	/// <returns>Имейлы через запятую или пустую строку</returns>
	private string GetMailingAdresses()
	{
		var emails = new List<string>();
		var query = @"SELECT C.ContactText FROM reports.general_reports AS GR
				JOIN contacts.contacts AS C ON GR.ContactGroupId = C.ContactOwnerId
				WHERE gr.GeneralReportCode = ?reportId
				UNION
				SELECT C.ContactText FROM reports.general_reports AS GR
				JOIN contacts.contacts AS C ON GR.PublicSubscriptionsId = C.ContactOwnerId
				WHERE  GR.GeneralReportCode = ?reportId";
		var param = new MySqlParameter("?reportId", _generalReport.Id);
		var data = ObjectFromQuery(new[] { param }, query);

		if (data.Count > 0)
			emails = data.Select(i => i[0] as string).ToList();

		return emails.Implode(",\n");
	}

	protected void chbAllow_CheckedChanged(object sender, EventArgs e)
	{
	}

	protected void btnExecute_mailing(object sender, EventArgs e)
	{
#if DEBUG
		Thread.Sleep(5000);
#endif
		if (RadioSelf.Checked)
			Send_self();
		if (RadioMails.Checked)
			Send_in_Emails();
		RunSelfTaskAndUpdateAction();
	}

	public void CopyMonthTriggerValuesInToTable()
	{
		DS.Tables[dtScheduleMonth.TableName].Rows.Clear();
		foreach (GridViewRow drv in dgvScheduleMonth.Rows) {
			DataRow dataRow = DS.Tables[dtScheduleMonth.TableName].NewRow();

			string h = ((TextBox)drv.FindControl("tbStart")).Text;
			string m = ((TextBox)drv.FindControl("tbStart")).Text.Substring(h.IndexOf(':') + 1, h.Length - h.IndexOf(':') - 1);
			if (m.StartsWith("0"))
				m = m.Substring(1, 1);

			dataRow[MSStartHour.ColumnName] = Convert.ToInt16(h.Substring(0, h.IndexOf(':')));
			dataRow[MSStartMinute.ColumnName] = Convert.ToInt16(m);

			for (int i = 1; i <= 12; i++) {
				dataRow["m" + i] = Convert.ToByte(((CheckBox)drv.FindControl("m" + i)).Checked);
			}
			for (int i = 1; i <= 31; i++) {
				dataRow["d" + i] = Convert.ToByte(((CheckBox)drv.FindControl("d" + i)).Checked);
			}
			DS.Tables[dtScheduleMonth.TableName].Rows.Add(dataRow);
		}
	}

	protected void dgvScheduleMonth_RowCommand(object sender, GridViewCommandEventArgs e)
	{
		if (e.CommandName == "Add") {
			CopyMonthTriggerValuesInToTable();

			var dr = DS.Tables[dtScheduleMonth.TableName].NewRow();

			DS.Tables[dtScheduleMonth.TableName].Rows.Add(dr);

			DS.Tables[dtScheduleMonth.TableName].AcceptChanges();

			dgvScheduleMonth.DataSource = DS;
			dgvScheduleMonth.DataMember = dtScheduleMonth.TableName;
			dgvScheduleMonth.DataBind();
		}
	}

	protected void btnExecute_sendReady(object sender, EventArgs e)
	{
		var mails = new List<string>();
		if (RadioSelf.Checked) {
			mails.AddRange(GetSelfEmails().Select(selfEmail => selfEmail[0].ToString()));
		}
		if (RadioMails.Checked) {
			var adresses = mail_Text.Text.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a));
			mails.AddRange(adresses);
		}
		if (mails.Count <= 0) {
			ErrorMassage.Text = "Укажите получателя отчета !";
			ErrorMassage.BackColor = Color.Red;
			return;
		}

		var error = _generalReport.ResendReport(DbSession, mails);
		if (!String.IsNullOrEmpty(error)) {
			ErrorMassage.Text = error;
			ErrorMassage.BackColor = Color.Red;
			return;
		}

		ErrorMassage.Text = "Файл отчета успешно отправлен";
		ErrorMassage.BackColor = Color.LightGreen;
	}
}