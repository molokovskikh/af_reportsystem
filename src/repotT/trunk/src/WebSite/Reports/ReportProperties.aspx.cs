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

public partial class Reports_ReportProperties : System.Web.UI.Page
{
    protected MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
    protected MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataSet DS;
    private DataTable dtNonOptimalParams;
    private DataColumn PID;
    private DataColumn PParamName;
    private DataColumn PPropertyType;
    private DataColumn PPropertyValue;
    private DataColumn PPropertyEnumID;
    public DataTable dtEnumValues;
    private DataColumn PStoredProc;
    DataTable dtProcResult;
    private DataTable dtClient;
    private DataColumn CFirmCode;
    private DataColumn CReportCaption;
    Int64 FirmCode;

    private const string DSParams = "Inforoom.Reports.ReportProperties.DSParams";

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
        if (!(Page.IsPostBack))
        {
            MyCn.Open();
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.Add("rp", Request["rp"]);
            MyCmd.CommandText = @"
SELECT
    rt.ReportCaption as CReportCaption, 
    gr.FirmCode as CFirmCode
FROM
    testreports.reports rt, testreports.general_reports gr
WHERE gr.GeneralReportCode=rt.GeneralReportCode
AND ReportCode = ?rp
";
            MyDA.Fill(DS, dtClient.TableName);
            lblReport.Text = DS.Tables[dtClient.TableName].Rows[0][CReportCaption.ColumnName].ToString();
            FirmCode = Convert.ToInt64(DS.Tables[dtClient.TableName].Rows[0][CFirmCode.ColumnName]);

            MyCn.Close();

            PostData();
        }
        else
        {
            DS = ((DataSet)Session[DSParams]);
            FirmCode = Convert.ToInt64(DS.Tables[dtClient.TableName].Rows[0][CFirmCode.ColumnName]);
        }
        //lblReport.Text = DS.Tables[dtClient.TableName].Rows[0][CReportCaption.ColumnName].ToString();
        //FirmCode = Convert.ToInt64(DS.Tables[dtClient.TableName].Rows[0][CFirmCode.ColumnName]);
        if (dgvNonOptional.Rows.Count > 0)
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
        MyCmd.Parameters.Add("rp", Request["rp"]);
        DS.Tables[dtNonOptimalParams.TableName].Clear();
        MyCmd.CommandText = @"
SELECT
    rp.ID as PID,
    rtp.DisplayName as PParamName,
    rtp.PropertyType as PPropertyType,
    rp.PropertyValue as PPropertyValue,
    rtp.PropertyEnumID as PPropertyEnumID,
    rtp.selectstoredprocedure as PStoredProc
FROM 
    testreports.report_properties rp, testreports.report_type_properties rtp
WHERE 
    rp.propertyID = rtp.ID
AND Optional=0
AND rp.reportCode=?rp
";
        MyDA.Fill(DS, dtNonOptimalParams.TableName);

        MyCn.Close();

        dgvNonOptional.DataSource = DS;
        dgvNonOptional.DataMember = DS.Tables[dtNonOptimalParams.TableName].TableName;
        dgvNonOptional.DataBind();
        Session[DSParams] = DS;
    }

    private void InitializeComponent()
    {
        this.DS = new System.Data.DataSet();
        this.dtNonOptimalParams = new System.Data.DataTable();
        this.PID = new System.Data.DataColumn();
        this.PParamName = new System.Data.DataColumn();
        this.PPropertyType = new System.Data.DataColumn();
        this.PPropertyValue = new System.Data.DataColumn();
        this.PPropertyEnumID = new System.Data.DataColumn();
        this.PStoredProc = new System.Data.DataColumn();
        this.dtClient = new System.Data.DataTable();
        this.CFirmCode = new System.Data.DataColumn();
        this.CReportCaption = new System.Data.DataColumn();
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtNonOptimalParams)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtClient)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtNonOptimalParams,
            this.dtClient});
        // 
        // dtNonOptimalParams
        // 
        this.dtNonOptimalParams.Columns.AddRange(new System.Data.DataColumn[] {
            this.PID,
            this.PParamName,
            this.PPropertyType,
            this.PPropertyValue,
            this.PPropertyEnumID,
            this.PStoredProc});
        this.dtNonOptimalParams.TableName = "dtNonOptimalParams";
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
            this.CFirmCode,
            this.CReportCaption});
        this.dtClient.TableName = "dtClient";
        // 
        // CFirmCode
        // 
        this.CFirmCode.ColumnName = "CFirmCode";
        this.CFirmCode.DataType = typeof(long);
        // 
        // CReportCaption
        // 
        this.CReportCaption.ColumnName = "CReportCaption";
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtNonOptimalParams)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtClient)).EndInit();

    }

    protected void dgvNonOptional_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            ((Button)e.Row.Cells[1].FindControl("btnFind")).CommandArgument = e.Row.RowIndex.ToString();
            ((Button)e.Row.Cells[1].FindControl("btnListValue")).CommandArgument = ((DataRowView)e.Row.DataItem)[PID.ColumnName].ToString();

            if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "BOOL")
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

                        FillDDL(((DataRowView)e.Row.DataItem)[PStoredProc.ColumnName].ToString(), FirmCode, "", ((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName].ToString());
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
        MyCmd.Parameters.Add("PEID", PropertyEnumID);
        MyCmd.CommandText = @"
SELECT distinct
    Value as evValue,
    DisplayValue as evName
FROM 
    testreports.report_type_properties rtp, testreports.Property_Enums pe, testreports.Enum_Values ev
WHERE 
    rtp.PropertyEnumID = pe.ID
AND pe.ID = ev.PropertyEnumID
AND rtp.PropertyEnumID=?PEID
";
        MyDA.Fill(dtEnumValues);

        MyCn.Close();
    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        CopyChangesToTable();

        MySqlTransaction trans;
        MyCn.Open();
        trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE 
    testreports.report_properties 
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

            MyDA.Update(DS, DS.Tables[dtNonOptimalParams.TableName].TableName);

            trans.Commit();

            PostData();
        }
        catch (Exception err)
        {
            trans.Rollback();
        }
        finally
        {
            MyCmd.Dispose();
            MyCn.Close();
            MyCn.Dispose();
        }
        if (dgvNonOptional.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    private void CopyChangesToTable()
    {
        foreach (GridViewRow dr in dgvNonOptional.Rows)
        {
            if (((DropDownList)dr.FindControl("ddlValue")).Visible == true)
            {
                if (DS.Tables[dtNonOptimalParams.TableName].DefaultView[dr.RowIndex][PPropertyValue.ColumnName].ToString() != ((DropDownList)dr.FindControl("ddlValue")).SelectedValue)
                    DS.Tables[dtNonOptimalParams.TableName].DefaultView[dr.RowIndex][PPropertyValue.ColumnName] = ((DropDownList)dr.FindControl("ddlValue")).SelectedValue;
            }
            else if (((CheckBox)dr.FindControl("chbValue")).Visible == true)
            {
                if (DS.Tables[dtNonOptimalParams.TableName].DefaultView[dr.RowIndex][PPropertyValue.ColumnName].ToString() != ((CheckBox)dr.FindControl("chbValue")).Checked.ToString())
                    DS.Tables[dtNonOptimalParams.TableName].DefaultView[dr.RowIndex][PPropertyValue.ColumnName] = Convert.ToInt32(((CheckBox)dr.FindControl("chbValue")).Checked).ToString();
            }
            else if (((TextBox)dr.FindControl("tbValue")).Visible == true)
            {
                if (DS.Tables[dtNonOptimalParams.TableName].DefaultView[dr.RowIndex][PPropertyValue.ColumnName].ToString() != ((TextBox)dr.FindControl("tbValue")).Text)
                    DS.Tables[dtNonOptimalParams.TableName].DefaultView[dr.RowIndex][PPropertyValue.ColumnName] = ((TextBox)dr.FindControl("tbValue")).Text;
            }
        }
    }

    private void FillDDL(string proc, Int64 fc, string filter, string id)
    {
        string db = String.Empty;
        try
        {
            if (MyCn.State != ConnectionState.Open)
                MyCn.Open();
            dtProcResult = new DataTable();
            db = MyCn.Database;
            MyCn.ChangeDatabase("testreports");
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.Add("inFirmCode", fc);
            MyCmd.Parameters["inFirmCode"].Direction = ParameterDirection.Input;
            MyCmd.Parameters.Add("inFilter", filter);
            MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
            if(id == String.Empty)
                MyCmd.Parameters.Add("inID", DBNull.Value);
            else
                MyCmd.Parameters.Add("inID", Convert.ToInt64(id));
            MyCmd.Parameters["inID"].Direction = ParameterDirection.Input;
            MyCmd.CommandText = proc;
            MyCmd.CommandType = CommandType.StoredProcedure;
            MyDA.Fill(dtProcResult);
        }
        catch(Exception e)
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

    protected void dgvNonOptional_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Find")
        {
            CopyChangesToTable();
            DropDownList ddlValues = ((DropDownList)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("ddlValue"));
            TextBox tbFind = ((TextBox)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("tbSearch"));
            Button btnFind = ((Button)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("btnFind"));

            FillDDL(DS.Tables[dtNonOptimalParams.TableName].DefaultView[Convert.ToInt32(e.CommandArgument)][PStoredProc.ColumnName].ToString(), FirmCode, ((TextBox)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("tbSearch")).Text, String.Empty);
            ShowSearchedParam(ddlValues, tbFind, btnFind);
        }
        else if (e.CommandName == "ShowValues")
        {
            string url = String.Format("ReportPropertyValues.aspx?r={0}&rp={1}&rpv={2}", Request["r"], Request["rp"], e.CommandArgument);
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
    protected void btnListValue_Click(object sender, EventArgs e)
    {
        string url = String.Format("ReportPropertyValues.aspx?r={0}&rp={1}", Request["r"], Request["rp"]);
        Response.Redirect(url);
    }
}
