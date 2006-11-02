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
using MySql.Data;
using MySql.Data.MySqlClient;

public partial class Reports_GeneralReports : System.Web.UI.Page
{
    protected MySqlConnection MyCn = new MySqlConnection("server=testSQL.analit.net; user id=system; password=123;");
    protected MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataSet DS;
    private DataTable dtGeneralReports;
    private DataColumn GRCode;
    private DataColumn GRFirmCode;
    private DataColumn GRReportCode;
    private DataColumn GRCaption;
    private DataColumn GRRTCode;
    private DataColumn GRAddress;
    private DataColumn GRSubject;
    private DataColumn GRFileName;
    private DataColumn GRArchName;
    private DataColumn GRAllow;
    private DataTable dtReports;
    private DataColumn RGeneralReportCode;
    private DataColumn RReportCode;
    private DataColumn RCaption;
    private DataColumn RRTCode;

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
    FirmCode as GRFirmCode,
    Allow as GRAllow,
    EMailAddress as GRAddress,
    EMailSubject as GRSubject,
    ReportFileName as GRFileName,
    ReportArchName as GRArchName
FROM 
    testreports.general_reports gr
";
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
        this.GRAddress = new System.Data.DataColumn();
        this.GRSubject = new System.Data.DataColumn();
        this.GRFileName = new System.Data.DataColumn();
        this.GRArchName = new System.Data.DataColumn();
        this.GRAllow = new System.Data.DataColumn();
        this.dtReports = new System.Data.DataTable();
        this.RGeneralReportCode = new System.Data.DataColumn();
        this.RReportCode = new System.Data.DataColumn();
        this.RCaption = new System.Data.DataColumn();
        this.RRTCode = new System.Data.DataColumn();
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtReports)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtGeneralReports,
            this.dtReports});
        // 
        // dtGeneralReports
        // 
        this.dtGeneralReports.Columns.AddRange(new System.Data.DataColumn[] {
            this.GRCode,
            this.GRFirmCode,
            this.GRReportCode,
            this.GRCaption,
            this.GRRTCode,
            this.GRAddress,
            this.GRSubject,
            this.GRFileName,
            this.GRArchName,
            this.GRAllow});
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
        // GRAddress
        // 
        this.GRAddress.ColumnName = "GRAddress";
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
        // dtReports
        // 
        this.dtReports.Columns.AddRange(new System.Data.DataColumn[] {
            this.RGeneralReportCode,
            this.RReportCode,
            this.RCaption,
            this.RRTCode});
        this.dtReports.TableName = "dtReports";
        // 
        // RGeneralReportCode
        // 
        this.RGeneralReportCode.ColumnName = "RGeneralReportCode";
        this.RGeneralReportCode.DataType = typeof(long);
        // 
        // RReportCode
        // 
        this.RReportCode.ColumnName = "RReportCode";
        this.RReportCode.DataType = typeof(long);
        // 
        // RCaption
        // 
        this.RCaption.ColumnName = "RCaption";
        // 
        // RRTCode
        // 
        this.RRTCode.ColumnName = "RRTCode";
        this.RRTCode.DataType = typeof(long);
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtReports)).EndInit();

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
        MyCmd.Parameters.Add("Name", "%" + Name + "%");
        DS.Tables[dtReports.TableName].Clear();
        MyCmd.CommandText = @"
SELECT 
    r.GeneralReportCode as RGeneralReportCode,
    ReportCode as RReportCode,
    concat(ReportCode, '.', ReportCaption) as RCaption,
    ReportTypeCode as RRTCode
FROM 
     testreports.general_reports gr, testreports.reports r
WHERE 
    gr.GeneralReportCode = r.GeneralReportCode
    and r.ReportCaption like ?Name
";
        MyDA.Fill(DS, dtReports.TableName);
        MyCn.Close();

        Session.Add(DSReports, DS);
    }

    private void CopyChangesToTable()
    {
        foreach (GridViewRow dr in dgvReports.Rows)
        {
            if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRAllow.ColumnName].ToString() != Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked).ToString())
                DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRAllow.ColumnName] = Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked);

            if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRAddress.ColumnName].ToString() != ((TextBox)dr.FindControl("tbEMail")).Text)
                DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRAddress.ColumnName] = ((TextBox)dr.FindControl("tbEMail")).Text;

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
    }

    protected void dgvReports_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            if (e.Row.Cells[0].FindControl("ddlName") != null)
            {
                DropDownList ddlNames = ((DropDownList)e.Row.Cells[0].FindControl("ddlName"));
                ddlNames.DataSource = dtReports;
                ddlNames.DataTextField = "RCaption";
                ddlNames.DataValueField = "RReportCode";
                ddlNames.DataBind();
            }
        }
    }
}
