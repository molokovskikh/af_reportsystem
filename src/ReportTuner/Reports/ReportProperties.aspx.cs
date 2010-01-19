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
using ReportTuner.Models;

public partial class Reports_ReportProperties : System.Web.UI.Page
{
	private MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
	private MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataSet DS;
    private DataTable dtNonOptionalParams;
    private DataColumn PID;
    private DataColumn PParamName;
    private DataColumn PPropertyType;
    private DataColumn PPropertyValue;
    private DataColumn PPropertyEnumID;
    public DataTable dtEnumValues;
    private DataColumn PStoredProc;
    DataTable dtProcResult;
	private DataTable dtClient;
    private DataColumn CReportCaption;
    private DataTable dtOptionalParams;
    private DataColumn OPID;
    private DataColumn OPParamName;
    private DataColumn OPPropertyType;
    private DataColumn OPPropertyValue;
    private DataColumn OPPropertyEnumID;
    private DataColumn OPStoredProc;
    private DataTable dtDDLOptionalParams;
    private DataColumn OPrtpID;
	private DataColumn CReportType;

    private const string DSParams = "Inforoom.Reports.ReportProperties.DSParams";

    protected void Page_Init(object sender, System.EventArgs e)
    {
        InitializeComponent();
    }
    
    protected void Page_Load(object sender, EventArgs e)
    {
		if (String.IsNullOrEmpty(Request["r"]) && String.IsNullOrEmpty(Request["TemporaryId"]))
			Response.Redirect("GeneralReports.aspx");

		if (String.IsNullOrEmpty(Request["rp"]))
			if (!String.IsNullOrEmpty(Request["r"]))
				Response.Redirect("Reports.aspx?r=" + Request["r"]);
		    else
				Response.Redirect("TemporaryReport.aspx?TemporaryId=" + Request["TemporaryId"]);

		btnBack.Visible = !String.IsNullOrEmpty(Request["TemporaryId"]);
		btnNext.Visible = btnBack.Visible;

        if (!(Page.IsPostBack))
        {
            MyCn.Open();
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.AddWithValue("rp", Request["rp"]);
            MyCmd.CommandText = @"
SELECT
    rt.ReportCaption as CReportCaption, 
    rts.ReportTypeName as CReportType
FROM
    reports.reports rt, 
    reports.general_reports gr,
    reports.reporttypes rts
WHERE gr.GeneralReportCode=rt.GeneralReportCode
AND ReportCode = ?rp
and rts.ReportTypeCode = rt.ReportTypeCode
";
            MyDA.Fill(DS, dtClient.TableName);
            lblReport.Text = DS.Tables[dtClient.TableName].Rows[0][CReportCaption.ColumnName].ToString();
			lblReportType.Text = DS.Tables[dtClient.TableName].Rows[0][CReportType.ColumnName].ToString();

            MyCn.Close();

            PostData();
        }
        else
        {
            DS = ((DataSet)Session[DSParams]);
        }
        if (dgvNonOptional.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    private void PostData()
    {
        FillNonOptimal();
        FillOptimal();
    }

    private void FillNonOptimal()
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;

        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        MyCmd.Parameters.AddWithValue("rp", Request["rp"]);
        DS.Tables[dtNonOptionalParams.TableName].Clear();
        MyCmd.CommandText = @"
SELECT
    rp.ID as PID,
    rtp.DisplayName as PParamName,
    rtp.PropertyType as PPropertyType,
    rp.PropertyValue as PPropertyValue,
    rtp.PropertyEnumID as PPropertyEnumID,
    rtp.selectstoredprocedure as PStoredProc
FROM 
    reports.report_properties rp, reports.report_type_properties rtp
WHERE 
    rp.propertyID = rtp.ID
AND rtp.Optional=0
and rtp.PropertyName not in ('ByPreviousMonth', 'ReportInterval', 'StartDate', 'EndDate')
AND rp.reportCode=?rp
";
		if (!String.IsNullOrEmpty(Request["TemporaryId"]))
			MyCmd.CommandText += @"
union
SELECT
    rp.ID as PID,
    rtp.DisplayName as PParamName,
    rtp.PropertyType as PPropertyType,
    rp.PropertyValue as PPropertyValue,
    rtp.PropertyEnumID as PPropertyEnumID,
    rtp.selectstoredprocedure as PStoredProc
FROM 
    reports.report_properties rp, reports.report_type_properties rtp
WHERE 
    rp.propertyID = rtp.ID
AND rtp.Optional=0
and rtp.PropertyName in ('StartDate', 'EndDate')
AND rp.reportCode=?rp
";
		else
			MyCmd.CommandText += @"
union
SELECT
    rp.ID as PID,
    rtp.DisplayName as PParamName,
    rtp.PropertyType as PPropertyType,
    rp.PropertyValue as PPropertyValue,
    rtp.PropertyEnumID as PPropertyEnumID,
    rtp.selectstoredprocedure as PStoredProc
FROM 
    reports.report_properties rp, reports.report_type_properties rtp
WHERE 
    rp.propertyID = rtp.ID
AND rtp.Optional=0
and rtp.PropertyName in ('ByPreviousMonth', 'ReportInterval')
AND rp.reportCode=?rp
";

		MyDA.Fill(DS, dtNonOptionalParams.TableName);

        MyCn.Close();

        dgvNonOptional.DataSource = DS;
        dgvNonOptional.DataMember = DS.Tables[dtNonOptionalParams.TableName].TableName;
        dgvNonOptional.DataBind();
        Session[DSParams] = DS;
    }

    private void FillOptimal()
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;

        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        MyCmd.Parameters.AddWithValue("rp", Request["rp"]);
        DS.Tables[dtOptionalParams.TableName].Clear();
        MyCmd.CommandText = @"
SELECT
    rp.ID as OPID,
    rtp.ID as OPrtpID,
    rtp.DisplayName as OPParamName,
    rtp.PropertyType as OPPropertyType,
    rp.PropertyValue as OPPropertyValue,
    rtp.PropertyEnumID as OPPropertyEnumID,
    rtp.selectstoredprocedure as OPStoredProc
FROM 
    reports.report_properties rp, reports.report_type_properties rtp
WHERE 
    rp.propertyID = rtp.ID
AND Optional=1
AND rp.reportCode=?rp
";
        MyDA.Fill(DS, dtOptionalParams.TableName);

        MyCn.Close();

        dgvOptional.DataSource = DS;
        dgvOptional.DataMember = DS.Tables[dtOptionalParams.TableName].TableName;
        dgvOptional.DataBind();
        Session[DSParams] = DS;
    }

    private void InitializeComponent()
    {
		this.DS = new System.Data.DataSet();
		this.dtNonOptionalParams = new System.Data.DataTable();
		this.PID = new System.Data.DataColumn();
		this.PParamName = new System.Data.DataColumn();
		this.PPropertyType = new System.Data.DataColumn();
		this.PPropertyValue = new System.Data.DataColumn();
		this.PPropertyEnumID = new System.Data.DataColumn();
		this.PStoredProc = new System.Data.DataColumn();
		this.dtClient = new System.Data.DataTable();
		this.CReportCaption = new System.Data.DataColumn();
		this.CReportType = new System.Data.DataColumn();
		this.dtOptionalParams = new System.Data.DataTable();
		this.OPID = new System.Data.DataColumn();
		this.OPParamName = new System.Data.DataColumn();
		this.OPPropertyType = new System.Data.DataColumn();
		this.OPPropertyValue = new System.Data.DataColumn();
		this.OPPropertyEnumID = new System.Data.DataColumn();
		this.OPStoredProc = new System.Data.DataColumn();
		this.OPrtpID = new System.Data.DataColumn();
		((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtNonOptionalParams)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtClient)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtOptionalParams)).BeginInit();
		// 
		// DS
		// 
		this.DS.DataSetName = "NewDataSet";
		this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtNonOptionalParams,
            this.dtClient,
            this.dtOptionalParams});
		// 
		// dtNonOptionalParams
		// 
		this.dtNonOptionalParams.Columns.AddRange(new System.Data.DataColumn[] {
            this.PID,
            this.PParamName,
            this.PPropertyType,
            this.PPropertyValue,
            this.PPropertyEnumID,
            this.PStoredProc});
		this.dtNonOptionalParams.TableName = "dtNonOptionalParams";
		// 
		// PID
		// 
		this.PID.ColumnName = "PID";
		this.PID.DataType = typeof(long);
		// 
		// PParamName
		// 
		this.PParamName.ColumnName = "PParamName";
		// 
		// PPropertyType
		// 
		this.PPropertyType.ColumnName = "PPropertyType";
		// 
		// PPropertyValue
		// 
		this.PPropertyValue.ColumnName = "PPropertyValue";
		// 
		// PPropertyEnumID
		// 
		this.PPropertyEnumID.ColumnName = "PPropertyEnumID";
		this.PPropertyEnumID.DataType = typeof(long);
		// 
		// PStoredProc
		// 
		this.PStoredProc.ColumnName = "PStoredProc";
		// 
		// dtClient
		// 
		this.dtClient.Columns.AddRange(new System.Data.DataColumn[] {
            this.CReportCaption,
            this.CReportType});
		this.dtClient.TableName = "dtClient";
		// 
		// CReportCaption
		// 
		this.CReportCaption.ColumnName = "CReportCaption";
		// 
		// CReportType
		// 
		this.CReportType.ColumnName = "CReportType";
		// 
		// dtOptionalParams
		// 
		this.dtOptionalParams.Columns.AddRange(new System.Data.DataColumn[] {
            this.OPID,
            this.OPParamName,
            this.OPPropertyType,
            this.OPPropertyValue,
            this.OPPropertyEnumID,
            this.OPStoredProc,
            this.OPrtpID});
		this.dtOptionalParams.TableName = "dtOptionalParams";
		// 
		// OPID
		// 
		this.OPID.ColumnName = "OPID";
		this.OPID.DataType = typeof(long);
		// 
		// OPParamName
		// 
		this.OPParamName.ColumnName = "OPParamName";
		// 
		// OPPropertyType
		// 
		this.OPPropertyType.ColumnName = "OPPropertyType";
		// 
		// OPPropertyValue
		// 
		this.OPPropertyValue.ColumnName = "OPPropertyValue";
		// 
		// OPPropertyEnumID
		// 
		this.OPPropertyEnumID.ColumnName = "OPPropertyEnumID";
		this.OPPropertyEnumID.DataType = typeof(long);
		// 
		// OPStoredProc
		// 
		this.OPStoredProc.ColumnName = "OPStoredProc";
		// 
		// OPrtpID
		// 
		this.OPrtpID.ColumnName = "OPrtpID";
		this.OPrtpID.DataType = typeof(long);
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtNonOptionalParams)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtClient)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtOptionalParams)).EndInit();

    }

    protected void dgvNonOptional_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            ((Button)e.Row.Cells[1].FindControl("btnFind")).CommandArgument = e.Row.RowIndex.ToString();
            ((Button)e.Row.Cells[1].FindControl("btnListValue")).CommandArgument = ((DataRowView)e.Row.DataItem)[PID.ColumnName].ToString();

			if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "DATETIME")
			{
				((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
				((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
				((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
				((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;
				((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
				((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
				((TextBox)e.Row.Cells[1].FindControl("tbDate")).Visible = true;
				string dateValue = ((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName].ToString();
				if (dateValue.Equals("NOW", StringComparison.OrdinalIgnoreCase))
					((TextBox)e.Row.Cells[1].FindControl("tbDate")).Text = DateTime.Now.ToString("yyyy-MM-dd");
				else
					((TextBox)e.Row.Cells[1].FindControl("tbDate")).Text = dateValue;
			}
			else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "BOOL")
            {
                ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;
                ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = true;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Checked = Convert.ToBoolean(Convert.ToInt32(((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName]));
            }
            else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "ENUM")
            {
                ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;


                DropDownList ddlValues = ((DropDownList)e.Row.Cells[1].FindControl("ddlValue"));
                ddlValues.Visible = true;
                FillDDL(Convert.ToInt64(((DataRowView)e.Row.DataItem)[PPropertyEnumID.ColumnName]));
                ddlValues.DataSource = dtEnumValues;
                ddlValues.DataTextField = "evName";
                ddlValues.DataValueField = "evValue";
                if (!(((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName] is DBNull))
                    ddlValues.SelectedValue = ((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName].ToString();
                ddlValues.DataBind();
            }
            else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "INT")
            {
                if (((DataRowView)e.Row.DataItem)[PStoredProc.ColumnName].ToString() == String.Empty)
                {
                    ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = true;
                    ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                    ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                    ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;

                }
                else
                {
                    ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                    ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;


                    if (((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName].ToString() != String.Empty)
                    {
                        ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = true;
                        ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                        ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;

                        FillDDL(
							((DataRowView)e.Row.DataItem)[PStoredProc.ColumnName].ToString(), 
							"", 
							((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName].ToString());
                        ShowSearchedParam(((DropDownList)e.Row.Cells[1].FindControl("ddlValue")), ((TextBox)e.Row.Cells[1].FindControl("tbSearch")), ((Button)e.Row.Cells[1].FindControl("btnFind")));

                    }
                    else
                    {
                        ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible =false;
                        ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = true;
                        ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = true;
                    }
                }
            }
            else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "LIST")
            {
                ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = true;

                ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;

            }
            else
            {
                ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = true;
                ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;
            }
        }
    }

    private void FillDDL(Int64 PropertyEnumID)
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;

        dtEnumValues = new DataTable("EnumValues");
        dtEnumValues.Columns.Add("evValue", typeof(int));
        dtEnumValues.Columns.Add("evName", typeof(string));

        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        MyCmd.Parameters.AddWithValue("PEID", PropertyEnumID);
        MyCmd.CommandText = @"
SELECT distinct
    Value as evValue,
    DisplayValue as evName
FROM 
    reports.report_type_properties rtp, reports.Property_Enums pe, reports.Enum_Values ev
WHERE 
    rtp.PropertyEnumID = pe.ID
AND pe.ID = ev.PropertyEnumID
AND rtp.PropertyEnumID=?PEID
";
        MyDA.Fill(dtEnumValues);

        MyCn.Close();
    }

    private void ApplyNonOptimal(MySqlTransaction trans)
    {
        MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE 
    reports.report_properties 
SET 
    PropertyValue = ?PPropertyValue
WHERE ID = ?PID", MyCn, trans);

        UpdCmd.Parameters.Clear();
        UpdCmd.Parameters.Add(new MySqlParameter("PID", MySqlDbType.Int64));
        UpdCmd.Parameters["PID"].Direction = ParameterDirection.Input;
        UpdCmd.Parameters["PID"].SourceColumn = PID.ColumnName;
        UpdCmd.Parameters["PID"].SourceVersion = DataRowVersion.Current;
        UpdCmd.Parameters.Add(new MySqlParameter("PPropertyValue", MySqlDbType.VarString));
        UpdCmd.Parameters["PPropertyValue"].Direction = ParameterDirection.Input;
        UpdCmd.Parameters["PPropertyValue"].SourceColumn = PPropertyValue.ColumnName;
        UpdCmd.Parameters["PPropertyValue"].SourceVersion = DataRowVersion.Current;

        MyDA.UpdateCommand = UpdCmd;

        string strHost = HttpContext.Current.Request.UserHostAddress;
        string strUser = HttpContext.Current.User.Identity.Name;
        if (strUser.StartsWith("ANALIT\\"))
        {
            strUser = strUser.Substring(7);
        }
        MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

        MyDA.Update(DS, DS.Tables[dtNonOptionalParams.TableName].TableName);
    }

    private void ApplyOptimal(MySqlTransaction trans)
    {
        MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE 
    reports.report_properties 
SET 
    PropertyValue = ?OPPropertyValue
WHERE ID = ?OPID", MyCn, trans);

        UpdCmd.Parameters.Clear();
        UpdCmd.Parameters.Add(new MySqlParameter("OPID", MySqlDbType.Int64));
        UpdCmd.Parameters["OPID"].Direction = ParameterDirection.Input;
        UpdCmd.Parameters["OPID"].SourceColumn = OPID.ColumnName;
        UpdCmd.Parameters["OPID"].SourceVersion = DataRowVersion.Current;
        UpdCmd.Parameters.Add(new MySqlParameter("OPPropertyValue", MySqlDbType.VarString));
        UpdCmd.Parameters["OPPropertyValue"].Direction = ParameterDirection.Input;
        UpdCmd.Parameters["OPPropertyValue"].SourceColumn = OPPropertyValue.ColumnName;
        UpdCmd.Parameters["OPPropertyValue"].SourceVersion = DataRowVersion.Current;

        MySqlCommand InsCmd = new MySqlCommand(@"
INSERT 
    reports.report_properties 
SET 
    ReportCode = ?rp,
    PropertyID = ?OPrtpID,
    PropertyValue = ?OPPropertyValue
", MyCn, trans);

        InsCmd.Parameters.Clear();
        InsCmd.Parameters.Add(new MySqlParameter("OPID", MySqlDbType.Int64));
        InsCmd.Parameters["OPID"].Direction = ParameterDirection.Input;
        InsCmd.Parameters["OPID"].SourceColumn = OPID.ColumnName;
        InsCmd.Parameters["OPID"].SourceVersion = DataRowVersion.Current;
        InsCmd.Parameters.Add(new MySqlParameter("OPPropertyValue", MySqlDbType.VarString));
        InsCmd.Parameters["OPPropertyValue"].Direction = ParameterDirection.Input;
        InsCmd.Parameters["OPPropertyValue"].SourceColumn = OPPropertyValue.ColumnName;
        InsCmd.Parameters["OPPropertyValue"].SourceVersion = DataRowVersion.Current;
        InsCmd.Parameters.Add(new MySqlParameter("OPrtpID", MySqlDbType.Int64));
        InsCmd.Parameters["OPrtpID"].Direction = ParameterDirection.Input;
        InsCmd.Parameters["OPrtpID"].SourceColumn = OPrtpID.ColumnName;
        InsCmd.Parameters["OPrtpID"].SourceVersion = DataRowVersion.Current;
        InsCmd.Parameters.Add(new MySqlParameter("rp", Request["rp"]));

        MySqlCommand DelCmd = new MySqlCommand(@"
DELETE FROM 
    reports.report_properties 
WHERE ID = ?OPID", MyCn, trans);

        DelCmd.Parameters.Clear();
        DelCmd.Parameters.Add(new MySqlParameter("OPID", MySqlDbType.Int64));
        DelCmd.Parameters["OPID"].Direction = ParameterDirection.Input;
        DelCmd.Parameters["OPID"].SourceColumn = OPID.ColumnName;
        DelCmd.Parameters["OPID"].SourceVersion = DataRowVersion.Original;

        MyDA.UpdateCommand = UpdCmd;
        MyDA.InsertCommand = InsCmd;
        MyDA.DeleteCommand = DelCmd;

        string strHost = HttpContext.Current.Request.UserHostAddress;
        string strUser = HttpContext.Current.User.Identity.Name;
        if (strUser.StartsWith("ANALIT\\"))
        {
            strUser = strUser.Substring(7);
        }
        MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

        MyDA.Update(DS, DS.Tables[dtOptionalParams.TableName].TableName);

    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
        CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

        MyCn.Open();
        foreach (DataRow dr in DS.Tables[dtOptionalParams.TableName].Rows)
        {
            if (dr.RowState == DataRowState.Added)
            {
                dr[OPPropertyValue.ColumnName] = MySqlHelper.ExecuteScalar(MyCn, "SELECT DefaultValue FROM reports.report_type_properties WHERE ID=" + dr[OPrtpID.ColumnName].ToString());
            }
        }
        MyCn.Close();

        MySqlTransaction trans;
        MyCn.Open();
        trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            ApplyNonOptimal(trans);
            ApplyOptimal(trans);
            trans.Commit();

            PostData();
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
        if (dgvNonOptional.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    private void CopyChangesToTable(GridView dgv, DataTable dt, string Column)
    {
        foreach (GridViewRow dr in dgv.Rows)
        {
            if (((DropDownList)dr.FindControl("ddlValue")).Visible == true)
            {
                if (DS.Tables[dt.TableName].DefaultView[dr.RowIndex][Column].ToString() != ((DropDownList)dr.FindControl("ddlValue")).SelectedValue)
                    DS.Tables[dt.TableName].DefaultView[dr.RowIndex][Column] = ((DropDownList)dr.FindControl("ddlValue")).SelectedValue;
            }
            else if (((CheckBox)dr.FindControl("chbValue")).Visible == true)
            {
                if (DS.Tables[dt.TableName].DefaultView[dr.RowIndex][Column].ToString() != ((CheckBox)dr.FindControl("chbValue")).Checked.ToString())
                    DS.Tables[dt.TableName].DefaultView[dr.RowIndex][Column] = Convert.ToInt32(((CheckBox)dr.FindControl("chbValue")).Checked).ToString();
            }
            else if (((TextBox)dr.FindControl("tbDate")).Visible == true)
			{
				if (DS.Tables[dt.TableName].DefaultView[dr.RowIndex][Column].ToString() != ((TextBox)dr.FindControl("tbDate")).Text)
					DS.Tables[dt.TableName].DefaultView[dr.RowIndex][Column] = ((TextBox)dr.FindControl("tbDate")).Text;
			}
			else if (((TextBox)dr.FindControl("tbValue")).Visible == true)
            {
                if (DS.Tables[dt.TableName].DefaultView[dr.RowIndex][Column].ToString() != ((TextBox)dr.FindControl("tbValue")).Text)
                    DS.Tables[dt.TableName].DefaultView[dr.RowIndex][Column] = ((TextBox)dr.FindControl("tbValue")).Text;
            }

            if (dgv == dgvOptional)
            {
                if (((DropDownList)dr.FindControl("ddlName")).Visible == true)
                {
                    if (DS.Tables[dt.TableName].DefaultView[dr.RowIndex][OPrtpID.ColumnName].ToString() != ((DropDownList)dr.FindControl("ddlName")).SelectedValue)
                    {
                        DS.Tables[dt.TableName].DefaultView[dr.RowIndex][OPrtpID.ColumnName] = ((DropDownList)dr.FindControl("ddlName")).SelectedValue;
                    }
                }
            }
        }
    }

    private void FillDDL(string proc, string filter, string id)
    {
        string db = String.Empty;
        try
        {
            if (MyCn.State != ConnectionState.Open)
                MyCn.Open();
            dtProcResult = new DataTable();
            db = MyCn.Database;
            MyCn.ChangeDatabase("reports");
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.AddWithValue("inFilter", filter);
            MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
            if(id == String.Empty)
                MyCmd.Parameters.AddWithValue("inID", DBNull.Value);
            else
                MyCmd.Parameters.AddWithValue("inID", Convert.ToInt64(id));
            MyCmd.Parameters["inID"].Direction = ParameterDirection.Input;
            MyCmd.CommandText = proc;
            MyCmd.CommandType = CommandType.StoredProcedure;
            MyDA.Fill(dtProcResult);
        }
        finally
        {
            if (db != String.Empty)
                MyCn.ChangeDatabase(db);
            MyCmd.CommandType = CommandType.Text;
            MyCn.Close();
        }
    }

    protected void dgvNonOptional_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Find")
        {
            CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
            CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

            DropDownList ddlValues = ((DropDownList)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("ddlValue"));
            TextBox tbFind = ((TextBox)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("tbSearch"));
            Button btnFind = ((Button)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("btnFind"));

            FillDDL(
				DS.Tables[dtNonOptionalParams.TableName].DefaultView[Convert.ToInt32(e.CommandArgument)][PStoredProc.ColumnName].ToString(),
				tbFind.Text, 
				String.Empty);
            ShowSearchedParam(ddlValues, tbFind, btnFind);
        }
        else if (e.CommandName == "ShowValues")
        {
			string url = String.Empty;
			var prop = ReportProperty.Find(Convert.ToUInt64(e.CommandArgument));

			if (!String.IsNullOrEmpty(Request["TemporaryId"]))
				url = String.Format("ReportPropertyValues.aspx?TemporaryId={0}&rp={1}&rpv={2}",
					Request["TemporaryId"], 
					Request["rp"], 
					e.CommandArgument);
			else if (prop.PropertyType.PropertyName == "BusinessRivals")
				url = String.Format("../ReportsTuning/SelectClients.rails?r={0}&report={1}&rpv={2}&firmType=0",
					Request["r"],
					Request["rp"],
					e.CommandArgument);
			else
				url = String.Format("ReportPropertyValues.aspx?r={0}&rp={1}&rpv={2}",
					Request["r"],
					Request["rp"],
					e.CommandArgument);
			Response.Redirect(url);
        }
    }

    protected void ddlValue_SelectedIndexChanged(object sender, EventArgs e)
    {
        if(((DropDownList)sender).SelectedValue == "-1")
        {
            ((DropDownList)sender).Visible = false;
            ((TextBox)((DropDownList)sender).Parent.FindControl("tbSearch")).Visible = true;
            ((TextBox)((DropDownList)sender).Parent.FindControl("tbSearch")).Text = string.Empty;
            ((Button)((DropDownList)sender).Parent.FindControl("btnFind")).Visible = true;
        }
    }

    private void ShowSearchedParam(DropDownList ddl, TextBox tb, Button btn)
    {
        if (dtProcResult.Rows.Count > 0)
        {
            ddl.Visible = true;
            tb.Visible = false;
            btn.Visible = false;
            ddl.DataSource = dtProcResult;
            ddl.DataTextField = "DisplayValue";
            ddl.DataValueField = "ID";
            ddl.DataBind();
            ListItem li = new ListItem();
            li.Text = "<изменить>";
            li.Value = "-1";
            ddl.Items.Insert(0, li);
            ddl.SelectedIndex = 1;
        }
        else
        {
            ddl.Visible = false;
            tb.Visible = true;
            tb.Text = String.Empty;
            btn.Visible = true;
        }

    }

    protected void dgvOptional_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Find")
        {
            CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
            CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

            DropDownList ddlValues = ((DropDownList)dgvOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("ddlValue"));
            TextBox tbFind = ((TextBox)dgvOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("tbSearch"));
            Button btnFind = ((Button)dgvOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("btnFind"));

            FillDDL(
				DS.Tables[dtOptionalParams.TableName].DefaultView[Convert.ToInt32(e.CommandArgument)][OPStoredProc.ColumnName].ToString(),
				tbFind.Text, 
				String.Empty);
            ShowSearchedParam(ddlValues, tbFind, btnFind);
        }
        else if (e.CommandName == "ShowValues")
        {
			string url = String.Empty;
			if (!String.IsNullOrEmpty(Request["TemporaryId"]))
				url = String.Format("ReportPropertyValues.aspx?TemporaryId={0}&rp={1}&rpv={2}",
					Request["TemporaryId"],
					Request["rp"],
					e.CommandArgument);
			else
			{
				var prop = ReportProperty.Find(Convert.ToUInt64(e.CommandArgument));
				if (prop.PropertyType.PropertyName == "ClientCodeEqual")
					url = String.Format("../ReportsTuning/SelectClients.rails?r={0}&report={1}&rpv={2}&firmType=1",
						Request["r"],
						Request["rp"],
						e.CommandArgument);
				else if(prop.PropertyType.PropertyName == "FirmCodeEqual")
					url = String.Format("../ReportsTuning/SelectClients.rails?r={0}&report={1}&rpv={2}&firmType=0",
						Request["r"],
						Request["rp"],
						e.CommandArgument);
				else
					url = String.Format("ReportPropertyValues.aspx?r={0}&rp={1}&rpv={2}",
						Request["r"],
						Request["rp"],
						e.CommandArgument);
			}

            Response.Redirect(url);
        }
        else if (e.CommandName == "Add")
        {
            bool AddedExist = false;
            foreach (DataRow dr in DS.Tables[dtOptionalParams.TableName].Rows)
            {
                if (dr.RowState == DataRowState.Added)
                {
                    AddedExist = true;
                    break;
                }
            }
            if (!AddedExist)
            {
                CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
                CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

                DataRow dr = DS.Tables[dtOptionalParams.TableName].NewRow();
                //dr[GRAllow.ColumnName] = 0;
                DS.Tables[dtOptionalParams.TableName].Rows.Add(dr);

                dgvOptional.DataSource = DS;

                dgvOptional.DataBind();

                btnApply.Visible = true;
            }
        }
    }

    private void FillDDLOptimal()
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;

        dtDDLOptionalParams = new DataTable("DDLOptionalParams");
        dtDDLOptionalParams.Columns.Add("opID", typeof(int));
        dtDDLOptionalParams.Columns.Add("opName", typeof(string));
        dtDDLOptionalParams.Columns.Add("opRemove", typeof(byte));

        MyDA.SelectCommand = MyCmd;
		MyCmd.Parameters.Clear();
		MyCmd.Parameters.AddWithValue("?ReportCode", Request["rp"]);
        MyCmd.CommandText = @"
Select
    rtp.ID as opID,
    rtp.DisplayName as opName
FROM
    reports.report_type_properties rtp,
    reports.reports r
WHERE
    rtp.Optional=1
and r.ReportCode = ?ReportCode
and rtp.ReportTypeCode = r.ReportTypeCode";
        MyDA.Fill(dtDDLOptionalParams);

        MyCn.Close();

        foreach (DataRow dr in dtDDLOptionalParams.Rows)
        {
            DataRow[] dtr = DS.Tables[dtOptionalParams.TableName].Select(OPrtpID.ColumnName + "=" + dr["opID"].ToString());
            if (dtr.Length > 0)
                dr["opRemove"] = 1;
            else
                dr["opRemove"] = 0;
        }

        DataRow[] dtrRemove = dtDDLOptionalParams.Select("opRemove = 1");
        for(int i=0; i<dtrRemove.Length; i++)
        {
            if (dtDDLOptionalParams.Select("opID=" + dtrRemove[i]["opID"]).Length > 0)
            {
                if (dtrRemove[i]["opRemove"].ToString() == "1")
                    dtDDLOptionalParams.Rows.Remove(dtrRemove[i]);
            }
        }
        dtDDLOptionalParams.AcceptChanges();
    }

    protected void dgvOptional_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            if (((Label)e.Row.Cells[0].FindControl("lblName")).Text == String.Empty)
            {
                DropDownList ddlName = ((DropDownList)e.Row.Cells[0].FindControl("ddlName"));
                ddlName.Visible = true;
                ((Label)e.Row.Cells[0].FindControl("lblName")).Visible = false;
                FillDDLOptimal();
                ddlName.DataSource = dtDDLOptionalParams;
                ddlName.DataTextField = "opName";
                ddlName.DataValueField = "opID";
                ddlName.DataBind();
            }
            else
            {
                ((DropDownList)e.Row.Cells[0].FindControl("ddlName")).Visible = false;
                ((Label)e.Row.Cells[0].FindControl("lblName")).Visible = true; 

                ((Button)e.Row.Cells[1].FindControl("btnFind")).CommandArgument = e.Row.RowIndex.ToString();
                ((Button)e.Row.Cells[1].FindControl("btnListValue")).CommandArgument = ((DataRowView)e.Row.DataItem)[OPID.ColumnName].ToString();

				if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "DATETIME")
				{
					((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
					((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
					((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
					((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;
					((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
					((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
					((TextBox)e.Row.Cells[1].FindControl("tbDate")).Visible = true;
					string dateValue = ((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName].ToString();
					if (dateValue.Equals("NOW", StringComparison.OrdinalIgnoreCase))
						((TextBox)e.Row.Cells[1].FindControl("tbDate")).Text = DateTime.Now.ToString("yyyy-MM-dd");
					else
						((TextBox)e.Row.Cells[1].FindControl("tbDate")).Text = dateValue;
				}
				else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "BOOL")
                {
                    ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                    ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;
                    ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                    ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = true;
                    ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Checked = Convert.ToBoolean(Convert.ToInt32(((DataRowView)e.Row.DataItem)[OPPropertyValue.ColumnName]));
                }
                else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "ENUM")
                {
                    ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                    ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                    ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;


                    DropDownList ddlValues = ((DropDownList)e.Row.Cells[1].FindControl("ddlValue"));
                    ddlValues.Visible = true;
                    FillDDL(Convert.ToInt64(((DataRowView)e.Row.DataItem)[OPPropertyEnumID.ColumnName]));
                    ddlValues.DataSource = dtEnumValues;
                    ddlValues.DataTextField = "evName";
                    ddlValues.DataValueField = "evValue";
                    if (!(((DataRowView)e.Row.DataItem)[OPPropertyValue.ColumnName] is DBNull))
                        ddlValues.SelectedValue = ((DataRowView)e.Row.DataItem)[OPPropertyValue.ColumnName].ToString();
                    ddlValues.DataBind();
                }
                else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "INT")
                {
                    if (((DataRowView)e.Row.DataItem)[OPStoredProc.ColumnName].ToString() == String.Empty)
                    {
                        ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = true;
                        ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                        ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                        ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                        ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                        ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;

                    }
                    else
                    {
                        ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                        ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                        ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;


                        if (((DataRowView)e.Row.DataItem)[OPPropertyValue.ColumnName].ToString() != String.Empty)
                        {
                            ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = true;
                            ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                            ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;

                            FillDDL(
								((DataRowView)e.Row.DataItem)[OPStoredProc.ColumnName].ToString(), 
								"", 
								((DataRowView)e.Row.DataItem)[OPPropertyValue.ColumnName].ToString());
                            ShowSearchedParam(((DropDownList)e.Row.Cells[1].FindControl("ddlValue")), ((TextBox)e.Row.Cells[1].FindControl("tbSearch")), ((Button)e.Row.Cells[1].FindControl("btnFind")));

                        }
                        else
                        {
                            ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                            ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = true;
                            ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = true;
                        }
                    }
                }
                else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "LIST")
                {
                    ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = true;

                    ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                    ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                    ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                    ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;

                }
                else
                {
                    ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = true;
                    ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                    ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnFind")).Visible = false;
                    ((TextBox)e.Row.Cells[1].FindControl("tbSearch")).Visible = false;
                    ((Button)e.Row.Cells[1].FindControl("btnListValue")).Visible = false;
                }
            }
        }    
    }

    protected void dgvOptional_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
        CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);
        DS.Tables[dtOptionalParams.TableName].DefaultView[e.RowIndex].Delete();
        dgvOptional.DataSource = DS;
        dgvOptional.DataBind();
    }

	protected void btnBack_Click(object sender, EventArgs e)
	{
		Response.Redirect("TemporaryReport.aspx?TemporaryId=" + Request["TemporaryId"]);
	}

	protected void btnNext_Click(object sender, EventArgs e)
	{
		Response.Redirect("TemporaryReportSchedule.aspx?TemporaryId=" + Request["TemporaryId"]);
	}
}
