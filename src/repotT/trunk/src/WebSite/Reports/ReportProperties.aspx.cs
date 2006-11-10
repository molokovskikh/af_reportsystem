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
    protected MySqlConnection MyCn = new MySqlConnection("server=testSQL.analit.net; user id=system; password=123;");
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

    private const string DSParams = "Inforoom.Reports.ReportProperties.DSParams";

    protected void Page_Init(object sender, System.EventArgs e)
    {
        InitializeComponent();
    }
    
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["rp"] == null)
            Response.Redirect("Reports.aspx");
        if (!(Page.IsPostBack))
        {
            MyCn.Open();
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.Add("rp", Request["rp"]);
            MyCmd.CommandText = @"
SELECT 
    ReportCaption
FROM 
    testreports.reports rt
WHERE ReportCode = ?rp
";
            lblReport.Text = MyCmd.ExecuteScalar().ToString();
            MyCn.Close();

            PostData();
        }
        else
            DS = ((DataSet)Session[DSParams]);
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
rtp.PropertyEnumID as PPropertyEnumID
FROM testreports.report_properties rp, testreports.report_type_properties rtp
where rp.propertyID = rtp.ID
and Optional=0
and rp.reportCode=?rp
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
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtNonOptimalParams)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtNonOptimalParams});
        // 
        // dtNonOptimalParams
        // 
        this.dtNonOptimalParams.Columns.AddRange(new System.Data.DataColumn[] {
            this.PID,
            this.PParamName,
            this.PPropertyType,
            this.PPropertyValue,
            this.PPropertyEnumID});
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
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtNonOptimalParams)).EndInit();

    }

    protected void dgvNonOptional_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "BOOL")
            {
                ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = true;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Checked = Convert.ToBoolean(Convert.ToInt32(((DataRowView)e.Row.DataItem)[PPropertyValue.ColumnName]));
            }
            else if (((Label)e.Row.Cells[1].FindControl("lblType")).Text == "ENUM")
            {
                ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = false;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;

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
            else
            {
                ((TextBox)e.Row.Cells[1].FindControl("tbValue")).Visible = true;
                ((DropDownList)e.Row.Cells[1].FindControl("ddlValue")).Visible = false;
                ((CheckBox)e.Row.Cells[1].FindControl("chbValue")).Visible = false;
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
FROM testreports.report_type_properties rtp, testreports.Property_Enums pe, testreports.Enum_Values ev
where rtp.PropertyEnumID = pe.ID
and pe.ID = ev.PropertyEnumID
and rtp.PropertyEnumID=?PEID
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
}
