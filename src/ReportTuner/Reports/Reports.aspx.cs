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
using Castle.ActiveRecord;

public partial class Reports_Reports : System.Web.UI.Page
{
	private MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
	private MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataSet DS;
    private DataTable dtReports;
    private DataColumn RReportCode;
    private DataColumn RReportTypeCode;
    private DataColumn RReportCaption;
    private DataColumn RReportTypeName;
    private DataTable dtTypes;
    private DataColumn ReportTypeName;
    private DataColumn ReportTypeCode;
    private DataColumn REnabled;

    private const string DSReports = "Inforoom.Reports.Reports.DSReports";

    protected void Page_Init(object sender, System.EventArgs e)
    {
        InitializeComponent();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if ((Request["r"] == null))
        {
            Response.Redirect("GeneralReports.aspx");
        }
        else
			if (Request["r"].Equals(ConfigurationManager.AppSettings["TemplateReportId"], StringComparison.OrdinalIgnoreCase))
				Response.Redirect("TemplateReports.aspx");
			else
				((HyperLinkField)dgvReports.Columns[3]).DataNavigateUrlFormatString = @"ReportProperties.aspx?rp={0}&r=" + Request["r"].ToString();

        if (!(Page.IsPostBack))
        {
            PostData();
        }
        else
            DS = ((DataSet)Session[DSReports]);
    }

    private void PostData()
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();

		GeneralReport report = GeneralReport.Find(Convert.ToUInt64(Request["r"]));
		tbEMailSubject.Text = report.EMailSubject;
		tbReportFileName.Text = report.ReportFileName;
		tbReportArchName.Text = report.ReportArchName;

        MyCmd.Connection = MyCn;
        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        DS.Tables[dtReports.TableName].Clear();
        MyCmd.Parameters.AddWithValue("rCode", Request["r"]);
        MyCmd.CommandText = @"
SELECT
    ReportTypeName as RReportTypeName,
    ReportCode as RReportCode,
    r.ReportTypeCode as RReportTypeCode,
    ReportCaption as RReportCaption,
    r.Enabled as REnabled
FROM
    reports.reports r, reports.reporttypes rt
WHERE
    r.reportTypeCode = rt.ReportTypeCode
AND GeneralReportCode = ?rCode
Order by r.ReportCode
";
        MyDA.Fill(DS, dtReports.TableName);

        MyCn.Close();

        dgvReports.DataSource = DS;
        dgvReports.DataMember = DS.Tables[dtReports.TableName].TableName;
        dgvReports.DataBind();
        Session[DSReports] = DS;
    }

    private void FillDDL()
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;
        MyDA.SelectCommand = MyCmd;
        DS.Tables[dtTypes.TableName].Clear();
        MyCmd.CommandText = @"
SELECT 
    ReportTypeName,
    ReportTypeCode
FROM 
    reports.reporttypes
order by ReportTypeName 
";
        MyDA.Fill(DS, DS.Tables[dtTypes.TableName].TableName);
        MyCn.Close();
        
        Session.Add(DSReports, DS);
    }

    private void InitializeComponent()
    {
        this.DS = new System.Data.DataSet();
        this.dtReports = new System.Data.DataTable();
        this.RReportCode = new System.Data.DataColumn();
        this.RReportTypeCode = new System.Data.DataColumn();
        this.RReportCaption = new System.Data.DataColumn();
        this.RReportTypeName = new System.Data.DataColumn();
        this.dtTypes = new System.Data.DataTable();
        this.ReportTypeName = new System.Data.DataColumn();
        this.ReportTypeCode = new System.Data.DataColumn();
        this.REnabled = new System.Data.DataColumn();
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtReports)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtTypes)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtReports,
            this.dtTypes});
        // 
        // dtReports
        // 
        this.dtReports.Columns.AddRange(new System.Data.DataColumn[] {
            this.RReportCode,
            this.RReportTypeCode,
            this.RReportCaption,
            this.RReportTypeName,
            this.REnabled});
        this.dtReports.TableName = "dtReports";
        // 
        // RReportCode
        // 
        this.RReportCode.ColumnName = "RReportCode";
        this.RReportCode.DataType = typeof(long);
        // 
        // RReportTypeCode
        // 
        this.RReportTypeCode.ColumnName = "RReportTypeCode";
        this.RReportTypeCode.DataType = typeof(long);
        // 
        // RReportCaption
        // 
        this.RReportCaption.ColumnName = "RReportCaption";
        // 
        // RReportTypeName
        // 
        this.RReportTypeName.ColumnName = "RReportTypeName";
        // 
        // dtTypes
        // 
        this.dtTypes.Columns.AddRange(new System.Data.DataColumn[] {
            this.ReportTypeName,
            this.ReportTypeCode});
        this.dtTypes.TableName = "dtTypes";
        // 
        // ReportTypeName
        // 
        this.ReportTypeName.ColumnName = "ReportTypeName";
        // 
        // ReportTypeCode
        // 
        this.ReportTypeCode.ColumnName = "ReportTypeCode";
        this.ReportTypeCode.DataType = typeof(long);
        // 
        // REnabled
        // 
        this.REnabled.ColumnName = "REnabled";
        this.REnabled.DataType = typeof(byte);
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtReports)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtTypes)).EndInit();

    }

    protected void dgvReports_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Add")
        {
            CopyChangesToTable();

            DataRow dr = DS.Tables[dtReports.TableName].NewRow();
			dr[REnabled.ColumnName] = 0;
            DS.Tables[dtReports.TableName].Rows.Add(dr);
            dgvReports.DataSource = DS;
            dgvReports.DataBind();
        }
    }

    private void CopyChangesToTable()
    {
        foreach (GridViewRow dr in dgvReports.Rows)
        {
            if (((DropDownList)dr.FindControl("ddlReports")).Visible == true)
            {
                if (DS.Tables[dtReports.TableName].DefaultView[dr.RowIndex][RReportTypeCode.ColumnName].ToString() != ((DropDownList)dr.FindControl("ddlReports")).SelectedValue)
                    DS.Tables[dtReports.TableName].DefaultView[dr.RowIndex][RReportTypeCode.ColumnName] = ((DropDownList)dr.FindControl("ddlReports")).SelectedValue;

            }

            if (DS.Tables[dtReports.TableName].DefaultView[dr.RowIndex][RReportCaption.ColumnName].ToString() != ((TextBox)dr.FindControl("tbCaption")).Text)
                DS.Tables[dtReports.TableName].DefaultView[dr.RowIndex][RReportCaption.ColumnName] = ((TextBox)dr.FindControl("tbCaption")).Text;

            if (DS.Tables[dtReports.TableName].DefaultView[dr.RowIndex][REnabled.ColumnName].ToString() != Convert.ToByte(((CheckBox)dr.FindControl("chbEnable")).Checked).ToString())
                DS.Tables[dtReports.TableName].DefaultView[dr.RowIndex][REnabled.ColumnName] = Convert.ToByte(((CheckBox)dr.FindControl("chbEnable")).Checked);

        }
    }

    protected void dgvReports_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            if (((Label)e.Row.Cells[0].FindControl("lblReports")).Text != "")
            {
                ((DropDownList)e.Row.Cells[0].FindControl("ddlReports")).Visible = false;
                ((Label)e.Row.Cells[0].FindControl("lblReports")).Visible = true;
                e.Row.Cells[2].Enabled = true;
            }
            else
            {
                DropDownList ddlReports = ((DropDownList)e.Row.Cells[0].FindControl("ddlReports"));
                ddlReports.Visible = true;
                e.Row.Cells[2].Enabled = false;
                FillDDL();
                ddlReports.DataSource = DS.Tables[dtTypes.TableName];
                ddlReports.DataTextField = "ReportTypeName";
                ddlReports.DataValueField = "ReportTypeCode";
                ddlReports.DataBind();
                ((Label)e.Row.Cells[0].FindControl("lblReports")).Visible = false;
            }
        }
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
    reports.reports 
SET 
    ReportCaption = ?RReportCaption,
    ReportTypeCode = ?RReportTypeCode,
    GeneralReportCode = ?RGeneralReportCode,
    Enabled = ?REnabled
WHERE ReportCode = ?RReportCode", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("RReportCaption", MySqlDbType.VarString));
            UpdCmd.Parameters["RReportCaption"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["RReportCaption"].SourceColumn = RReportCaption.ColumnName;
            UpdCmd.Parameters["RReportCaption"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("RReportTypeCode", MySqlDbType.Int64));
            UpdCmd.Parameters["RReportTypeCode"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["RReportTypeCode"].SourceColumn = RReportTypeCode.ColumnName;
            UpdCmd.Parameters["RReportTypeCode"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("RReportCode", MySqlDbType.Int64));
            UpdCmd.Parameters["RReportCode"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["RReportCode"].SourceColumn = RReportCode.ColumnName;
            UpdCmd.Parameters["RReportCode"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("REnabled", MySqlDbType.Byte));
            UpdCmd.Parameters["REnabled"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["REnabled"].SourceColumn = REnabled.ColumnName;
            UpdCmd.Parameters["REnabled"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("RGeneralReportCode", Request["r"]));

            MySqlCommand DelCmd = new MySqlCommand(@"
DELETE from reports.reports 
WHERE ReportCode = ?RDelReportCode", MyCn, trans);

            DelCmd.Parameters.Clear();
            DelCmd.Parameters.Add(new MySqlParameter("RDelReportCode", MySqlDbType.Int64));
            DelCmd.Parameters["RDelReportCode"].Direction = ParameterDirection.Input;
            DelCmd.Parameters["RDelReportCode"].SourceColumn = RReportCode.ColumnName;
            DelCmd.Parameters["RDelReportCode"].SourceVersion = DataRowVersion.Original;

            MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO 
    reports.reports 
SET 
    ReportCaption = ?RReportCaption,
    ReportTypeCode = ?RReportTypeCode,
    GeneralReportCode = ?RGeneralReportCode,
    Enabled = ?REnabled
", MyCn, trans);

            InsCmd.Parameters.Clear();
            InsCmd.Parameters.Add(new MySqlParameter("RReportCaption", MySqlDbType.VarString));
            InsCmd.Parameters["RReportCaption"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["RReportCaption"].SourceColumn = RReportCaption.ColumnName;
            InsCmd.Parameters["RReportCaption"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("RReportTypeCode", MySqlDbType.Int64));
            InsCmd.Parameters["RReportTypeCode"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["RReportTypeCode"].SourceColumn = RReportTypeCode.ColumnName;
            InsCmd.Parameters["RReportTypeCode"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("REnabled", MySqlDbType.Byte));
            InsCmd.Parameters["REnabled"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["REnabled"].SourceColumn = REnabled.ColumnName;
            InsCmd.Parameters["REnabled"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("RGeneralReportCode", Request["r"]));

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

            MyDA.Update(DS, DS.Tables[dtReports.TableName].TableName);

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

		using (new TransactionScope())
		{
			GeneralReport report = GeneralReport.Find(Convert.ToUInt64(Request["r"]));
			report.EMailSubject = tbEMailSubject.Text;
			report.ReportFileName = tbReportFileName.Text;
			report.ReportArchName = tbReportArchName.Text;
			report.Save();
		}

		PostData();
    }

    protected void dgvReports_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        CopyChangesToTable();
        DS.Tables[dtReports.TableName].DefaultView[e.RowIndex].Delete();
        dgvReports.DataSource = DS;
        dgvReports.DataBind();
    }
}
