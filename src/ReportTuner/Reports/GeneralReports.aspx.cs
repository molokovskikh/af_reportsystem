using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using MySql.Data;
using MySql.Data.MySqlClient;
using Microsoft.Win32.TaskScheduler;

public partial class Reports_GeneralReports : System.Web.UI.Page
{
	private MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
	private MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataSet DS;
    private DataTable dtGeneralReports;
    private DataColumn GRCode;
    private DataColumn GRFirmCode;
    private DataColumn GRReportCode;
    private DataColumn GRCaption;
	private DataColumn GRRTCode;
    private DataColumn GRSubject;
    private DataColumn GRFileName;
    private DataColumn GRArchName;
    private DataColumn GRAllow;
    private DataTable dtClients;
    private DataColumn CCaption;
    private DataColumn CFirmCode;
    private DataColumn GRFirmName;

    private const string DSReports = "Inforoom.Reports.GeneralReports.DSReports";

    protected void Page_Init(object sender, System.EventArgs e)
    {
        InitializeComponent();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            PostData();
        }
        else
        {
            DS = ((DataSet)Session[DSReports]);
        }
        if (dgvReports.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    private void PostData()
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;
        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        DS.Tables[dtGeneralReports.TableName].Clear();
        MyCmd.CommandText = @"
SELECT 
    gr.GeneralReportCode as GRCode,
    gr.FirmCode as GRFirmCode,
    convert(concat(cd.FirmCode, ' - ', cd.ShortName) using cp1251) as GRFirmName,
    Allow as GRAllow,
    EMailSubject as GRSubject,
    ReportFileName as GRFileName,
    ReportArchName as GRArchName
FROM 
    reports.general_reports gr, usersettings.clientsdata cd
WHERE cd.FirmCode=gr.FirmCode
and gr.GeneralReportCode <> ?TemplateReportId
and gr.Temporary = 0
Order by gr.GeneralReportCode
";
		MyCmd.Parameters.AddWithValue("?TemplateReportId", ConfigurationManager.AppSettings["TemplateReportId"]);
        MyDA.Fill(DS, dtGeneralReports.TableName);
        MyCn.Close();

        Session.Add(DSReports, DS);
        dgvReports.DataSource = DS;
        dgvReports.DataMember = DS.Tables[dtGeneralReports.TableName].TableName;
        dgvReports.DataBind();
    }

    private void InitializeComponent()
    {
		this.DS = new System.Data.DataSet();
		this.dtGeneralReports = new System.Data.DataTable();
		this.GRCode = new System.Data.DataColumn();
		this.GRFirmCode = new System.Data.DataColumn();
		this.GRReportCode = new System.Data.DataColumn();
		this.GRCaption = new System.Data.DataColumn();
		this.GRRTCode = new System.Data.DataColumn();
		this.GRSubject = new System.Data.DataColumn();
		this.GRFileName = new System.Data.DataColumn();
		this.GRArchName = new System.Data.DataColumn();
		this.GRAllow = new System.Data.DataColumn();
		this.GRFirmName = new System.Data.DataColumn();
		this.dtClients = new System.Data.DataTable();
		this.CCaption = new System.Data.DataColumn();
		this.CFirmCode = new System.Data.DataColumn();
		((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtClients)).BeginInit();
		// 
		// DS
		// 
		this.DS.DataSetName = "NewDataSet";
		this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtGeneralReports,
            this.dtClients});
		// 
		// dtGeneralReports
		// 
		this.dtGeneralReports.Columns.AddRange(new System.Data.DataColumn[] {
            this.GRCode,
            this.GRFirmCode,
            this.GRReportCode,
            this.GRCaption,
            this.GRRTCode,
            this.GRSubject,
            this.GRFileName,
            this.GRArchName,
            this.GRAllow,
            this.GRFirmName});
		this.dtGeneralReports.TableName = "dtGeneralReports";
		// 
		// GRCode
		// 
		this.GRCode.ColumnName = "GRCode";
		this.GRCode.DataType = typeof(long);
		// 
		// GRFirmCode
		// 
		this.GRFirmCode.ColumnName = "GRFirmCode";
		this.GRFirmCode.DataType = typeof(long);
		// 
		// GRReportCode
		// 
		this.GRReportCode.ColumnName = "GRReportCode";
		this.GRReportCode.DataType = typeof(long);
		// 
		// GRCaption
		// 
		this.GRCaption.ColumnName = "GRCaption";
		// 
		// GRRTCode
		// 
		this.GRRTCode.ColumnName = "GRRTCode";
		this.GRRTCode.DataType = typeof(long);
		// 
		// GRSubject
		// 
		this.GRSubject.ColumnName = "GRSubject";
		// 
		// GRFileName
		// 
		this.GRFileName.ColumnName = "GRFileName";
		// 
		// GRArchName
		// 
		this.GRArchName.ColumnName = "GRArchName";
		// 
		// GRAllow
		// 
		this.GRAllow.ColumnName = "GRAllow";
		this.GRAllow.DataType = typeof(byte);
		// 
		// GRFirmName
		// 
		this.GRFirmName.ColumnName = "GRFirmName";
		// 
		// dtClients
		// 
		this.dtClients.Columns.AddRange(new System.Data.DataColumn[] {
            this.CCaption,
            this.CFirmCode});
		this.dtClients.TableName = "dtClients";
		// 
		// CCaption
		// 
		this.CCaption.ColumnName = "CCaption";
		// 
		// CFirmCode
		// 
		this.CFirmCode.ColumnName = "CFirmCode";
		this.CFirmCode.DataType = typeof(long);
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtClients)).EndInit();

    }

    protected void dgvReports_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Add")
        {
            CopyChangesToTable();

            DataRow dr = DS.Tables[dtGeneralReports.TableName].NewRow();
            dr[GRAllow.ColumnName] = 0;
            DS.Tables[dtGeneralReports.TableName].Rows.Add(dr);

            dgvReports.DataSource = DS;

            dgvReports.DataBind();

            btnApply.Visible = true;
        }
    }

    protected void dgvReports_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        CopyChangesToTable();
        DS.Tables[dtGeneralReports.TableName].DefaultView[e.RowIndex].Delete();
        dgvReports.DataSource = DS;
        dgvReports.DataBind();
    }

    private void FillDDL(string Name)
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;
        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        MyCmd.Parameters.AddWithValue("Name", "%" + Name + "%");
        DS.Tables[dtClients.TableName].Clear();
        MyCmd.CommandText = @"
SELECT
    cd.FirmCode as CFirmCode,
    cd.ShortName,
    convert(concat(cd.FirmCode, ' - ', cd.ShortName) using cp1251) as CCaption
FROM
     usersettings.clientsdata cd
 WHERE
  cd.ShortName like ?Name
Order by FirmCode, ShortName
";
        MyDA.Fill(DS, DS.Tables[dtClients.TableName].TableName);
        MyCn.Close();
        Session.Add(DSReports, DS);
    }

    private void CopyChangesToTable()
    {
        foreach (GridViewRow dr in dgvReports.Rows)
        {
            if (((DropDownList)dr.FindControl("ddlNames")).SelectedValue != String.Empty)
            {
                if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRFirmCode.ColumnName].ToString() != ((DropDownList)dr.FindControl("ddlNames")).SelectedValue)
                    DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRFirmCode.ColumnName] = ((DropDownList)dr.FindControl("ddlNames")).SelectedValue;
            }

            if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRAllow.ColumnName].ToString() != Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked).ToString())
                DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRAllow.ColumnName] = Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked);

            if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRSubject.ColumnName].ToString() != ((TextBox)dr.FindControl("tbSubject")).Text)
                DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRSubject.ColumnName] = ((TextBox)dr.FindControl("tbSubject")).Text;

            if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRFileName.ColumnName].ToString() != ((TextBox)dr.FindControl("tbFile")).Text)
                DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRFileName.ColumnName] = ((TextBox)dr.FindControl("tbFile")).Text;

            if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRArchName.ColumnName].ToString() != ((TextBox)dr.FindControl("tbArch")).Text)
                DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRArchName.ColumnName] = ((TextBox)dr.FindControl("tbArch")).Text;
        }
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        FillDDL(((TextBox)(((Button)sender).Parent).FindControl("tbSearch")).Text);
        DropDownList ddlNames = (DropDownList)(((Button)sender).Parent).FindControl("ddlNames");
        ddlNames.DataSource = DS.Tables[dtClients.TableName];
        ddlNames.DataTextField = "CCaption";
        ddlNames.DataValueField = "CFirmCode";
        ddlNames.DataBind();
		ddlNames.Focus();
    }

    protected void dgvReports_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            if (((Label)e.Row.Cells[1].FindControl("lblFirmName")).Text != "")
            {
                ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                ((Button)e.Row.Cells[1].FindControl("btnSearch")).Visible = false;
                ((DropDownList)e.Row.Cells[1].FindControl("ddlNames")).Visible = false;
                ((Label)e.Row.Cells[1].FindControl("lblFirmName")).Visible = true;
                e.Row.Cells[7].Enabled = true;
            }
            else
            {
                ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = true;
				((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Focus();
                ((Button)e.Row.Cells[1].FindControl("btnSearch")).Visible = true;

                DropDownList ddlReports = ((DropDownList)e.Row.Cells[0].FindControl("ddlNames"));
                ddlReports.Visible = true;
				//Делаем недоступными столбцы
				//"Рассылки"
				e.Row.Cells[3].Enabled = false;
				//"Отчеты"
				e.Row.Cells[7].Enabled = false;
				//"Расписание"
				e.Row.Cells[8].Enabled = false;
				((Label)e.Row.Cells[1].FindControl("lblFirmName")).Visible = false;
            }
        }
    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        CopyChangesToTable();

		List<ulong> _deletedReports = new List<ulong>();

        MySqlTransaction trans;
        MyCn.Open();
        trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE 
    reports.general_reports 
SET 
    FirmCode = ?GRFirmCode,
    Allow = ?GRAllow,
    EMailSubject = ?GRSubject,
    ReportFileName = ?GRFileName,
    ReportArchName = ?GRArchName
WHERE GeneralReportCode = ?GRCode", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("GRFirmCode", MySqlDbType.Int64));
            UpdCmd.Parameters["GRFirmCode"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRFirmCode"].SourceColumn = GRFirmCode.ColumnName;
            UpdCmd.Parameters["GRFirmCode"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("GRAllow", MySqlDbType.Byte));
            UpdCmd.Parameters["GRAllow"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRAllow"].SourceColumn = GRAllow.ColumnName;
            UpdCmd.Parameters["GRAllow"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("GRSubject", MySqlDbType.VarString));
            UpdCmd.Parameters["GRSubject"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRSubject"].SourceColumn = GRSubject.ColumnName;
            UpdCmd.Parameters["GRSubject"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("GRFileName", MySqlDbType.VarString));
            UpdCmd.Parameters["GRFileName"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRFileName"].SourceColumn = GRFileName.ColumnName;
            UpdCmd.Parameters["GRFileName"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("GRArchName", MySqlDbType.VarString));
            UpdCmd.Parameters["GRArchName"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRArchName"].SourceColumn = GRArchName.ColumnName;
            UpdCmd.Parameters["GRArchName"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("GRCode", MySqlDbType.Int64));
            UpdCmd.Parameters["GRCode"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRCode"].SourceColumn = GRCode.ColumnName;
            UpdCmd.Parameters["GRCode"].SourceVersion = DataRowVersion.Current;

            MySqlCommand DelCmd = new MySqlCommand(@"
DELETE from reports.general_reports 
WHERE GeneralReportCode = ?GRDelCode", MyCn, trans);

            DelCmd.Parameters.Clear();
            DelCmd.Parameters.Add(new MySqlParameter("GRDelCode", MySqlDbType.Int64));
            DelCmd.Parameters["GRDelCode"].Direction = ParameterDirection.Input;
            DelCmd.Parameters["GRDelCode"].SourceColumn = GRCode.ColumnName;
            DelCmd.Parameters["GRDelCode"].SourceVersion = DataRowVersion.Original;

            MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO 
    reports.general_reports 
SET 
    FirmCode = ?GRFirmCode,
    Allow = ?GRAllow,
    EMailSubject = ?GRSubject,
    ReportFileName = ?GRFileName,
    ReportArchName = ?GRArchName
", MyCn, trans);

            InsCmd.Parameters.Clear();
            InsCmd.Parameters.Add(new MySqlParameter("GRAllow", MySqlDbType.Byte));
            InsCmd.Parameters["GRAllow"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["GRAllow"].SourceColumn = GRAllow.ColumnName;
            InsCmd.Parameters["GRAllow"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("GRFirmCode", MySqlDbType.Int64));
            InsCmd.Parameters["GRFirmCode"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["GRFirmCode"].SourceColumn = GRFirmCode.ColumnName;
            InsCmd.Parameters["GRFirmCode"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("GRSubject", MySqlDbType.VarString));
            InsCmd.Parameters["GRSubject"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["GRSubject"].SourceColumn = GRSubject.ColumnName;
            InsCmd.Parameters["GRSubject"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("GRFileName", MySqlDbType.VarString));
            InsCmd.Parameters["GRFileName"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["GRFileName"].SourceColumn = GRFileName.ColumnName;
            InsCmd.Parameters["GRFileName"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("GRArchName", MySqlDbType.VarString));
            InsCmd.Parameters["GRArchName"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["GRArchName"].SourceColumn = GRArchName.ColumnName;
            InsCmd.Parameters["GRArchName"].SourceVersion = DataRowVersion.Current;

            MyDA.UpdateCommand = UpdCmd;
            MyDA.DeleteCommand = DelCmd;
            MyDA.InsertCommand = InsCmd;

            string strHost = HttpContext.Current.Request.UserHostAddress;
            string strUser = HttpContext.Current.User.Identity.Name;
            if (strUser.StartsWith("ANALIT\\"))
            {
                strUser = strUser.Substring(7);
            }
            MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

			DataTable dtDeleted = DS.Tables[dtGeneralReports.TableName].GetChanges(DataRowState.Deleted);
			if (dtDeleted != null)
				foreach (DataRow drDeleted in dtDeleted.Rows)
					_deletedReports.Add(Convert.ToUInt64(drDeleted[GRCode.ColumnName, DataRowVersion.Original]));

            MyDA.Update(DS, DS.Tables[dtGeneralReports.TableName].TableName);

            trans.Commit();
        }
        catch 
        {
            trans.Rollback();
            throw;
        }
        finally
        {
            MyCn.Close();
        }

		//Удаляем задания для отчетов
		if (_deletedReports.Count > 0)
		{
			using (TaskService taskService = new TaskService(
				ConfigurationManager.AppSettings["asComp"],
				ConfigurationManager.AppSettings["asScheduleUserName"],
				ConfigurationManager.AppSettings["asScheduleDomainName"],
				ConfigurationManager.AppSettings["asSchedulePassword"]))
			using (TaskFolder reportsFolder = taskService.GetFolder(ConfigurationManager.AppSettings["asReportsFolderName"]))
			{
				foreach (ulong _deletedReportId in _deletedReports)
					reportsFolder.DeleteTask("GR" + _deletedReportId + ".job");
			}
		}

		PostData();

		if (dgvReports.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }
}
