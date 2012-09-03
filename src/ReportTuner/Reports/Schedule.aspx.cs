using System;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Common.MySql;
using Common.Tools;
using MySql.Data.MySqlClient;
using Microsoft.Win32.TaskScheduler;
using System.Threading;
using ReportTuner.Models;
using ReportTuner.Helpers;


public partial class Reports_schedule : Page
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
	private Task temp1Task;
	private TaskDefinition currentTaskDefinition;
	private DaysOfTheWeek triggerDays = 0;
	private DataColumn SStartHour;
	private DataColumn SStartMinute;

	private DataColumn MSStartHour;
	private DataColumn MSStartMinute;

	private string ftpDirectory;

	private const string DSSchedule = "Inforoom.Reports.Schedule.DSSchedule";

	private const string StatusRunning = "Выполнить задание";
	private const string StatusNotRunning = "Выполняется...";

	protected void Page_Init(object sender, EventArgs e)
	{
		InitializeComponent();
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		if (Request["r"] == null)
			Response.Redirect("GeneralReports.aspx");

		_generalReport = GeneralReport.Find(Convert.ToUInt64(Request["r"]));

		taskService = ScheduleHelper.GetService();
		reportsFolder = ScheduleHelper.GetReportsFolder(taskService);
		currentTask = ScheduleHelper.GetTask(taskService, reportsFolder, _generalReport.Id, _generalReport.Comment, "GR");
		currentTaskDefinition = currentTask.Definition;

		temp1Task = Report.CreateTemporaryTaskForRunFromInterface(taskService, reportsFolder, currentTask, "/gr:" + _generalReport.Id + string.Format(" /manual:true"));

		btnExecute.Enabled = currentTask.State != TaskState.Running && temp1Task.State != TaskState.Running;
		btnExecute.Text = (currentTask.State == TaskState.Running) ? StatusNotRunning : StatusRunning;

		var tempTask = ScheduleHelper.GetTask(taskService, reportsFolder, Convert.ToUInt64(0), "tempTask", "temp");
		var userName = HttpContext.Current.User.Identity.Name.Replace(@"ANALIT\", string.Empty);

		ErrorMassage.Text = string.Empty;

		var description = tempTask.State == TaskState.Running ? string.Format("(запустил: {0})", tempTask.Definition.RegistrationInfo.Description) : string.Empty;

		if (tempTask.State == TaskState.Running || temp1Task.State == TaskState.Running) {
			var prefix = tempTask.State == TaskState.Running ? "Успешно запущен разовый отчет" : "Отчет запущен";
			if (tempTask.Definition.RegistrationInfo.Description == userName) {
				ErrorMassage.Text = string.Format("{0}, ожидайте окончания выполнения операции", prefix);
				ErrorMassage.BackColor = Color.LightGreen;
			}
			else {
				ErrorMassage.Text = String.Format("{1}, выполнение данного очета отложено {0}", description, prefix);
				ErrorMassage.BackColor = Color.Red;
			}
			btn_Mailing.Enabled = false;
			RadioSelf.Enabled = false;
			RadioMails.Enabled = false;
		}
		if (tempTask.State == TaskState.Queued || temp1Task.State == TaskState.Queued) {
			var prefix = tempTask.State == TaskState.Running ? "Запускается разовый отчет" : "Отчет запускается";
			if (tempTask.Definition.RegistrationInfo.Description == userName) {
				ErrorMassage.Text = string.Format("{0}, ожидайте окончания выполнения операции", prefix);
				ErrorMassage.BackColor = Color.LightGreen;
			}
			else {
				ErrorMassage.Text = string.Format("{1} {0}, выполнение данного очета отложено)", description, prefix);
				ErrorMassage.BackColor = Color.Red;
			}
			btn_Mailing.Enabled = false;
			RadioSelf.Enabled = false;
			RadioMails.Enabled = false;
		}
		if ((tempTask.State == TaskState.Ready && temp1Task.State != TaskState.Running && temp1Task.State != TaskState.Queued) ||
			(temp1Task.State == TaskState.Ready && tempTask.State != TaskState.Running && tempTask.State != TaskState.Queued)) {
			if (tempTask.Definition.RegistrationInfo.Description == userName) {
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
		if ((tempTask.State == TaskState.Disabled && temp1Task.State != TaskState.Running && temp1Task.State != TaskState.Queued) ||
			(temp1Task.State == TaskState.Disabled && tempTask.State != TaskState.Running && tempTask.State != TaskState.Queued)) {
			if (Session["StartTaskTime"] != null) {
				Session.Remove("StartTaskTime");
				ErrorMassage.Text = "Операция отменена";
				ErrorMassage.BackColor = Color.Red;
			}
			else
				ErrorMassage.Text = "";
		}


		var otherTriggers = new List<Trigger>();
		if (!Page.IsPostBack) {
			var selfMail = GetSelfEmails();
			if ((selfMail.Count != 0) && (selfMail[0].Length != 0)) {
				RadioSelf.Text = "Выполнить и отослать на: " + selfMail[0][0];
			}

			dtFrom.Value = DateTime.Now.AddDays(-7).ToShortDateString();
			dtTo.Value = DateTime.Now.ToShortDateString();
			mail_Text.Text = GetMailingAdresses().Select(a => a[0].ToString()).Implode(", \r");

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
			}
			finally {
				MyCn.Close();
			}

			chbAllow.Checked = currentTask.Enabled;
			lblWork.Text = ((ExecAction)currentTask.Definition.Actions[0]).Path + " " + ((ExecAction)currentTask.Definition.Actions[0]).Arguments;
			lblFolder.Text = ((ExecAction)currentTask.Definition.Actions[0]).WorkingDirectory;
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

		ftpDirectory = Path.Combine(ConfigurationManager.AppSettings["FTPOptBoxPath"], _generalReport.FirmCode.Value.ToString("000"), "Reports");
#if DEBUG
		ftpDirectory = Path.Combine(ScheduleHelper.ScheduleWorkDir, "OptBox", _generalReport.FirmCode.Value.ToString("000"), "Reports");
#endif
		send_created_report.Visible = Directory.Exists(ftpDirectory);
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

	protected void btnApply_Click(object sender, EventArgs e)
	{
		if (this.IsValid) {
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

		btnExecute.Enabled = currentTask.State != TaskState.Running && temp1Task.State != TaskState.Running;
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
			columnsForAdd.Add(new DataColumn("m" + i, typeof(byte)) { DefaultValue = ((byte)0) });
		}
		for (var i = 1; i <= 31; i++) {
			columnsForAdd.Add(new DataColumn("d" + i, typeof(byte)) { DefaultValue = ((byte)0) });
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
		if (Page.IsValid) {
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
		if (IsValid && (currentTask.State != TaskState.Running) && (temp1Task.State != TaskState.Running)) {
			temp1Task.Run();
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
		if (temp1Task != null) {
			temp1Task.Dispose();
			temp1Task = null;
		}
		if (taskService != null) {
			taskService.Dispose();
			taskService = null;
		}
	}

	private void RunSelfTaskAndUpdateAction()
	{
		const int tempNum = 0;
		string user = HttpContext.Current.User.Identity.Name.Replace(@"ANALIT\", string.Empty);
		var thisTask = ScheduleHelper.GetTask(taskService, reportsFolder, Convert.ToUInt64(tempNum), user, "temp");

		var newAction = new ExecAction(ScheduleHelper.ScheduleAppPath,
			"/gr:" + _generalReport.Id +
				string.Format(" /inter:true /dtFrom:{0} /dtTo:{1} /manual:true", DateTime.Parse(dtFrom.Value).ToShortDateString(), DateTime.Parse(dtTo.Value).ToShortDateString()),
			ScheduleHelper.ScheduleWorkDir);

		var taskDefinition = thisTask.Definition;

		taskDefinition.Actions.RemoveAt(0);
		taskDefinition.Actions.Add(newAction);
		taskDefinition.RegistrationInfo.Description = user;
		ScheduleHelper.UpdateTaskDefinition(taskService, reportsFolder, Convert.ToUInt64(tempNum), taskDefinition, "temp");

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


	private List<object[]> GetMailingAdresses()
	{
		var sqlSelectReports = ObjectFromQuery(new[] { new MySqlParameter("?GeneralReportID", _generalReport.Id) },
			@"
SELECT    ContactGroupId 
FROM    reports.general_reports cr,
		billing.payers p
WHERE   
	 p.PayerId = cr.PayerId
and cr.generalreportcode = ?GeneralReportID");
		if (sqlSelectReports.Count > 0) {
			var emails = ObjectFromQuery(new[] {
				new MySqlParameter("?ContactGroupId", sqlSelectReports[0][0]),
				new MySqlParameter("?ContactGroupType", 6),
				new MySqlParameter("?ContactType", 0.ToString())
			},
				@"
select lower(c.contactText)
from
  contacts.contact_groups cg
  join contacts.contacts c on cg.Id = c.ContactOwnerId
where
	cg.Id = ?ContactGroupId
and cg.Type = ?ContactGroupType
and c.Type = ?ContactType
union
select lower(c.contactText)
from
  contacts.contact_groups cg
  join contacts.persons p on cg.id = p.ContactGroupId
  join contacts.contacts c on p.Id = c.ContactOwnerId
where
	cg.Id = ?ContactGroupId
and cg.Type = ?ContactGroupType
and c.Type = ?ContactType");
			return emails;
		}
		return null;
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
		if (_generalReport.FirmCode == null)
			return;

		var message = new MailMessage();
		message.From = new MailAddress("report@analit.net", "АК Инфорум");
		message.Subject = _generalReport.EMailSubject;
		if (RadioSelf.Checked)
			foreach (var selfEmail in GetSelfEmails()) {
				message.To.Add(new MailAddress(selfEmail[0].ToString()));
			}
		if (RadioMails.Checked) {
			var adresses = mail_Text.Text.Split(',').Select(a => a.Trim(new[] { ' ', '\n', '\r' })).Where(a => !string.IsNullOrEmpty(a));
			foreach (var adress in adresses) {
				message.To.Add(new MailAddress(adress));
			}
		}
		if (message.To.Count <= 0) {
			ErrorMassage.Text = "Укажите получателя отчета !";
			ErrorMassage.BackColor = Color.Red;
			return;
		}
		if (Directory.Exists(ftpDirectory))
			foreach (var file in Directory.GetFiles(ftpDirectory)) {
				message.Attachments.Add(new Attachment(file));
			}
		if (message.Attachments.Count <= 0) {
			ErrorMassage.Text = "Файл отчета не найден";
			ErrorMassage.BackColor = Color.Red;
			return;
		}
		var client = new SmtpClient(ConfigurationManager.AppSettings["SMTPHost"]);
		message.BodyEncoding = System.Text.Encoding.UTF8;
		client.Send(message);
		ErrorMassage.Text = "Файл отчета успешно отправлен";
		ErrorMassage.BackColor = Color.LightGreen;
	}
}