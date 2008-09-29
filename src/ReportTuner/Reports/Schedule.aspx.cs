using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using TaskScheduler;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.DirectoryServices;

public partial class Reports_schedule : System.Web.UI.Page
{
	private MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
	private MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();

    string asWorkDir = String.Empty;
    string asApp = String.Empty;
    string asComp = String.Empty;
	string ScheduleUserName = String.Empty;
	string SchedulePassword = String.Empty;
	ScheduledTasks st = null;
    string taskName = String.Empty;
    private DataSet DS;
    private DataTable dtSchedule;
    private DataColumn SWeek;
    private DataColumn SMonday;
    private DataColumn STuesday;
    private DataColumn SWednesday;
    private DataColumn SThursday;
    private DataColumn SFriday;
    private DataColumn SSaturday;
    private DataColumn SSunday;
    Task currentTask;
    DaysOfTheWeek triggerDays = 0;
    private DataColumn SStartHour;
    private DataColumn SStartMinute;
    private const string DSSchedule = "Inforoom.Reports.Schedule.DSSchedule";

	private const string StatusRunning = "��������� �������";
	private const string StatusNotRunning = "�����������...";

    protected void Page_Init(object sender, System.EventArgs e)
    {
        InitializeComponent();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["r"] == null)
            Response.Redirect("GeneralReports.aspx");

        taskName = "GR" + Request["r"];
        asWorkDir = System.Configuration.ConfigurationManager.AppSettings["asWorkDir"];
        asApp = System.Configuration.ConfigurationManager.AppSettings["asApp"];
        asComp = System.Configuration.ConfigurationManager.AppSettings["asComp"];
		ScheduleUserName = System.Configuration.ConfigurationManager.AppSettings["asScheduleUserName"];
		SchedulePassword = System.Configuration.ConfigurationManager.AppSettings["asSchedulePassword"];
		st = new ScheduledTasks(asComp);
        currentTask = FindTask(st);
		btnExecute.Enabled = currentTask.Status != TaskStatus.Running;
		btnExecute.Text = (btnExecute.Enabled) ? StatusRunning : StatusNotRunning;

        if (!Page.IsPostBack)
        {
            MyCn.Open();
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.AddWithValue("?r", Request["r"]);
            MyCmd.CommandText = @"
SELECT
    convert(concat(cd.FirmCode, ' - ', cd.ShortName) using cp1251)
FROM
    reports.general_reports gr, usersettings.clientsdata cd
WHERE cd.FirmCode=gr.FirmCode
and gr.GeneralReportCode = ?r
";
            lblClient.Text = MyCmd.ExecuteScalar().ToString();
            MyCn.Close();

            lblWork.Text = taskName;

            lblWork.Text = currentTask.ApplicationName + " " + currentTask.Parameters;
            lblFolder.Text = currentTask.WorkingDirectory;
            if ((currentTask.Flags & TaskFlags.Disabled) > 0)
                chbAllow.Checked = false;
            else 
                chbAllow.Checked = true;
            tbComment.Text = currentTask.Comment;
            tbUserName.Text = currentTask.AccountName;
            TriggerList TL = currentTask.Triggers;
            DaysOfTheWeek days;
            for (int i = 0; i < TL.Count; i++)
            {
                DataRow dr = DS.Tables[dtSchedule.TableName].NewRow();
                WeeklyTrigger trigger = ((WeeklyTrigger)TL[i]);
                dr[SStartHour.ColumnName] = ((WeeklyTrigger)(trigger)).StartHour;
                dr[SStartMinute.ColumnName] = ((WeeklyTrigger)(trigger)).StartMinute;
                //               dr[SStart.ColumnName] = ((WeeklyTrigger)(trigger)).StartHour + ":" + ((WeeklyTrigger)(trigger)).StartMinute;
                days = ((WeeklyTrigger)(trigger)).WeekDays;
                //days = days | DaysOfTheWeek.Friday | DaysOfTheWeek.Thursday;
                System.Diagnostics.Debug.WriteLine(days);

                SetWeekDays(dr, DaysOfTheWeek.Monday, days);
                SetWeekDays(dr, DaysOfTheWeek.Tuesday, days);
                SetWeekDays(dr, DaysOfTheWeek.Wednesday, days);
                SetWeekDays(dr, DaysOfTheWeek.Thursday, days);
                SetWeekDays(dr, DaysOfTheWeek.Friday, days);
                SetWeekDays(dr, DaysOfTheWeek.Saturday, days);
                SetWeekDays(dr, DaysOfTheWeek.Sunday, days);

                DS.Tables[dtSchedule.TableName].Rows.Add(dr);
            }
            DS.Tables[dtSchedule.TableName].AcceptChanges();
            dgvSchedule.DataSource = DS;
            dgvSchedule.DataMember = dtSchedule.TableName;
            dgvSchedule.DataBind();
            Session[DSSchedule] = DS;
            //��������� ������ � ��������
            st.Dispose();
        }
        else
        {
            DS = ((DataSet)Session[DSSchedule]);
        }
    }

    private void SetWeekDays(DataRow dr, DaysOfTheWeek weekDay, DaysOfTheWeek days)
    {
        string column = "S" + weekDay.ToString();
        if ((weekDay & days) == weekDay)
            dr[column] = 1;
        else
            dr[column] = 0;
    }

    private Task FindTask(ScheduledTasks st)
    {
        Task t = null;
        string[] taskNames = st.GetTaskNames();
        bool find = false;
        foreach (string name in taskNames)
        {
            if (name == taskName+".job")
            {
                find = true;
                t = st.OpenTask(name);
                break;
            }
        }
        if (!find)
        {
            t = CreateNewTask(st);
            t = st.OpenTask(taskName);
        }
        return t;
    }

    private Task CreateNewTask(ScheduledTasks st)
    {
        Task t = st.CreateTask(taskName);
        
        t.ApplicationName = asApp;
        t.Parameters = "/gr:" + Request["r"];
		if (String.IsNullOrEmpty(ScheduleUserName))
			t.SetAccountInformation(String.Empty, null);
		else
			t.SetAccountInformation(ScheduleUserName, SchedulePassword);
        t.WorkingDirectory = asWorkDir;
        t.Save();
        t.Close();
        return t;
    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        if (this.IsValid)
        {
            CopyChangesToTable();

            SaveTriggers();
            SaveTaskChanges();
        }
        //��������� ������ � ��������
        st.Dispose();
    }

    private void CopyChangesToTable()
    {
        DS.Tables[dtSchedule.TableName].Rows.Clear();
        foreach (GridViewRow drv in dgvSchedule.Rows)
        {
            DataRow dr = DS.Tables[dtSchedule.TableName].NewRow();
            //dr[SStart.ColumnName] = ((TextBox)drv.FindControl("tbStart")).Text;
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
        currentTask.Comment = tbComment.Text;
        if (!chbAllow.Checked)
            currentTask.Flags = currentTask.Flags | TaskFlags.Disabled;
        else
            currentTask.Flags = currentTask.Flags & (~TaskFlags.Disabled);
        if (String.IsNullOrEmpty(tbUserName.Text))
            currentTask.SetAccountInformation("", null);
        else
            if (!String.IsNullOrEmpty(tbPassword.Text))
                currentTask.SetAccountInformation(tbUserName.Text, tbPassword.Text);
 
        currentTask.Save();
        currentTask.Close();
    }

    private void SaveTriggers()
    {
        currentTask.Triggers.Clear();

        foreach(DataRow dr in DS.Tables[dtSchedule.TableName].Rows)
        {
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

            WeeklyTrigger trigger = new WeeklyTrigger(h, m, triggerDays);
            currentTask.Triggers.Add(trigger);
        }
    }

    private void AddDay(DataRow dr, DaysOfTheWeek weekDay)
    {
        string column = "S" + weekDay.ToString();
        if (dr[column].ToString() == "1")
        {
            if (triggerDays == 0)
                triggerDays = weekDay;
            else
                triggerDays = triggerDays | weekDay;
        }
    }

    private void InitializeComponent()
    {
        this.DS = new System.Data.DataSet();
        this.dtSchedule = new System.Data.DataTable();
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
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtSchedule)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtSchedule});
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
            this.SStartMinute});
        this.dtSchedule.TableName = "dtSchedule";
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
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtSchedule)).EndInit();

    }
    protected void dgvSchedule_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Add")
        {
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
    protected void dgvSchedule_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            TextBox tb = ((TextBox)e.Row.Cells[0].FindControl("tbStart"));
            tb.Text = ((DataRowView)e.Row.DataItem)[SStartHour.ColumnName].ToString() + ":" + ((DataRowView)e.Row.DataItem)[SStartMinute.ColumnName].ToString().PadLeft(2,'0');
        }
    }
    protected void btnExecute_Click(object sender, EventArgs e)
    {
		if (this.IsValid && (currentTask.Status != TaskStatus.Running))
        {
            currentTask.Run();
			btnExecute.Enabled = false;
			btnExecute.Text = StatusRunning;
        }
        currentTask.Close();
        //��������� ������ � ��������
        st.Dispose();
    }

    bool IsUserExist(string domain, string login, string password)
    {
        DirectoryEntry entry = null;
        string path = "LDAP://" + domain;
        try
        {
            entry = new DirectoryEntry(path, login, password);
            path = entry.Name;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (entry != null)
                entry.Dispose();
        }
    }

    protected void CustomValidator1_ServerValidate(object source, ServerValidateEventArgs args)
    {
        args.IsValid = (String.IsNullOrEmpty(tbPassword.Text)) || IsUserExist("analit", tbUserName.Text, tbPassword.Text);
    }
}