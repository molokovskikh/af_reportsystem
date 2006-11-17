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

public partial class Reports_ReportPropertyValues : System.Web.UI.Page
{
    protected MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
    protected MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();

    string ListProc = String.Empty;
    Int64 FirmCode;
    Int64 ReportPropertyID;
    private DataSet DS;
    private DataTable dtProcResult;
    private DataColumn ID;
    private DataColumn DisplayValue;
    private DataColumn Enabled;
    private DataTable dtEnabledValues;
    private DataColumn EVID;
    private DataColumn EVName;
    private DataTable dtList;
    private DataColumn LFirmCode;
    private DataColumn LProc;
    private DataColumn LName;
    private DataColumn LReportPropertyID;

    private const string DSValues = "Inforoom.Reports.ReportValues.DSValues";

    protected void Page_Init(object sender, System.EventArgs e)
    {
        InitializeComponent();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["rp"] == null)
            Response.Redirect("Reports.aspx");
        if (Request["r"] == null)
            Response.Redirect("GeneralReports.aspx");
        if (Request["rpv"] == null)
            Response.Redirect("ReportProperties.aspx");
        if (!(Page.IsPostBack))
        {
            MyCn.Open();
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.Add("rpv", Request["rpv"]);
            MyCmd.Parameters.Add("r", Request["r"]);
            MyCmd.CommandText = @"
select
 rtp.displayname as LName,
 rtp.selectstoredprocedure as LProc,
 gr.FirmCode as LFirmCode,
 rp.ID as LReportPropertyID
from report_properties rp, report_type_properties rtp, reports r, general_reports gr
where rtp.ID=rp.PropertyID
and rtp.ReportTypeCode = r.ReportTypeCode
and r.generalreportcode=gr.generalreportcode
and gr.generalreportcode=?r
and rp.ID=?rpv
";
            MyDA.Fill(DS, dtList.TableName);
            lblListName.Text = DS.Tables[dtList.TableName].Rows[0][LName.ColumnName].ToString();
            ListProc = DS.Tables[dtList.TableName].Rows[0][LProc.ColumnName].ToString();
            FirmCode = Convert.ToInt64(DS.Tables[dtList.TableName].Rows[0][LFirmCode.ColumnName]);
            ReportPropertyID = Convert.ToInt64(DS.Tables[dtList.TableName].Rows[0][LReportPropertyID.ColumnName]);

            MyCn.Close();
            PostData();
        }
        else
        {
            DS = ((DataSet)Session[DSValues]);
            ListProc = DS.Tables[dtList.TableName].Rows[0][LProc.ColumnName].ToString();
            FirmCode = Convert.ToInt64(DS.Tables[dtList.TableName].Rows[0][LFirmCode.ColumnName]);
            ReportPropertyID = Convert.ToInt64(DS.Tables[dtList.TableName].Rows[0][LReportPropertyID.ColumnName]);
        }
        dgvListValues.DataBind();
        if (dgvListValues.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    private void PostData()
    {
        FillFromProc();
        FillEnabled();

    }

    private void FillEnabled()
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;

        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        MyCmd.Parameters.Add("rpv", Request["rpv"]);
        DS.Tables[dtEnabledValues.TableName].Clear();
        MyCmd.CommandText = @"
SELECT
    rpv.ID as EVID,
    rpv.Value as EVName
FROM 
    testreports.report_property_values rpv
WHERE 
    ReportPropertyID = ?rpv
";
        MyDA.Fill(DS, dtEnabledValues.TableName);

        MyCn.Close();

        Session[DSValues] = DS;
    }

    private bool ShowEnabled(String id)
    {
        bool find = false;
        foreach (DataRow drEnabled in DS.Tables[dtEnabledValues.TableName].Rows)
        {
            if (id == drEnabled[EVName.ColumnName].ToString())
            {
                find = true;
                break;
            }
        }
        return find;
    }

    private void InitializeComponent()
    {
        this.DS = new System.Data.DataSet();
        this.dtProcResult = new System.Data.DataTable();
        this.ID = new System.Data.DataColumn();
        this.DisplayValue = new System.Data.DataColumn();
        this.Enabled = new System.Data.DataColumn();
        this.dtEnabledValues = new System.Data.DataTable();
        this.EVID = new System.Data.DataColumn();
        this.EVName = new System.Data.DataColumn();
        this.dtList = new System.Data.DataTable();
        this.LFirmCode = new System.Data.DataColumn();
        this.LProc = new System.Data.DataColumn();
        this.LName = new System.Data.DataColumn();
        this.LReportPropertyID = new System.Data.DataColumn();
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtProcResult)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtEnabledValues)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtList)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtProcResult,
            this.dtEnabledValues,
            this.dtList});
        // 
        // dtProcResult
        // 
        this.dtProcResult.Columns.AddRange(new System.Data.DataColumn[] {
            this.ID,
            this.DisplayValue,
            this.Enabled});
        this.dtProcResult.TableName = "dtProcResult";
        // 
        // ID
        // 
        this.ID.ColumnName = "ID";
        this.ID.DataType = typeof(long);
        // 
        // DisplayValue
        // 
        this.DisplayValue.ColumnName = "DisplayValue";
        // 
        // Enabled
        // 
        this.Enabled.ColumnName = "Enabled";
        this.Enabled.DataType = typeof(byte);
        // 
        // dtEnabledValues
        // 
        this.dtEnabledValues.Columns.AddRange(new System.Data.DataColumn[] {
            this.EVID,
            this.EVName});
        this.dtEnabledValues.TableName = "dtEnabledValues";
        // 
        // EVID
        // 
        this.EVID.ColumnName = "EVID";
        this.EVID.DataType = typeof(long);
        // 
        // EVName
        // 
        this.EVName.ColumnName = "EVName";
        // 
        // dtList
        // 
        this.dtList.Columns.AddRange(new System.Data.DataColumn[] {
            this.LFirmCode,
            this.LProc,
            this.LName,
            this.LReportPropertyID});
        this.dtList.TableName = "dtList";
        // 
        // LFirmCode
        // 
        this.LFirmCode.ColumnName = "LFirmCode";
        this.LFirmCode.DataType = typeof(long);
        // 
        // LProc
        // 
        this.LProc.ColumnName = "LProc";
        // 
        // LName
        // 
        this.LName.ColumnName = "LName";
        // 
        // LReportPropertyID
        // 
        this.LReportPropertyID.ColumnName = "LReportPropertyID";
        this.LReportPropertyID.DataType = typeof(long);
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtProcResult)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtEnabledValues)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtList)).EndInit();

    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        FillFromProc();
        dgvListValues.DataBind();
    }

    private void FillFromProc()
    {
        string db = String.Empty;
        try
        {
            if (MyCn.State != ConnectionState.Open)
                MyCn.Open();
            db = MyCn.Database;
            MyCn.ChangeDatabase("testreports");
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            DS.Tables[dtProcResult.TableName].Clear();
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.Add("inFirmCode", FirmCode);
            MyCmd.Parameters["inFirmCode"].Direction = ParameterDirection.Input;
            MyCmd.Parameters.Add("inFilter", tbSearch.Text);
            MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
            MyCmd.Parameters.Add("inID", null);
            MyCmd.Parameters["inID"].Direction = ParameterDirection.Input;
            MyCmd.CommandText = ListProc;
            MyCmd.CommandType = CommandType.StoredProcedure;
            MyDA.Fill(DS, dtProcResult.TableName);

            dgvListValues.DataSource = DS;
            dgvListValues.DataMember = DS.Tables[dtProcResult.TableName].TableName;
//            dgvListValues.DataBind();
            Session[DSValues] = DS;
        }
        catch (Exception ex)
        {
        }
        finally
        {
            if (db != String.Empty)
                MyCn.ChangeDatabase(db);
            MyCmd.Dispose();
            MyCmd.CommandType = CommandType.Text;
            MyCn.Close();
        }
    }

    protected void dgvListValues_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            ((DataRowView)e.Row.DataItem)[Enabled.ColumnName] = Convert.ToByte(ShowEnabled(((DataRowView)e.Row.DataItem)[DisplayValue.ColumnName].ToString()));
            ((CheckBox)e.Row.Cells[0].FindControl("chbEnabled")).Checked = Convert.ToBoolean(((DataRowView)e.Row.DataItem)[Enabled.ColumnName]);
            if (chbShowEnabled.Checked)
            {
                if (!((CheckBox)e.Row.Cells[0].FindControl("chbEnabled")).Checked)
                    e.Row.Visible = false;
            }
        }
    }

    private void CopyChangesToTable()
    {
        foreach (GridViewRow dr in dgvListValues.Rows)
        {
            if (((CheckBox)dr.FindControl("chbEnabled")).Visible == true)
            {
                if (DS.Tables[dtProcResult.TableName].DefaultView[dr.RowIndex][Enabled.ColumnName].ToString() != ((CheckBox)dr.FindControl("chbEnabled")).Checked.ToString())
                    DS.Tables[dtProcResult.TableName].DefaultView[dr.RowIndex][Enabled.ColumnName] = Convert.ToInt32(((CheckBox)dr.FindControl("chbEnabled")).Checked).ToString();
            }
        }
    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        CopyChangesToTable();

        foreach (DataRow dr in DS.Tables[dtProcResult.TableName].Rows)
        {
            if (dr.RowState == DataRowState.Modified)
            {

                //((DataRowVersion)((DataRowView)dr.ItemArray).RowVersion)
            }
        }

        if (dgvListValues.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }
}
