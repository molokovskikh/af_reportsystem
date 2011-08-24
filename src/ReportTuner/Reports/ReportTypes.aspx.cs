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

public partial class Reports_ReportTypes : System.Web.UI.Page
{
	private MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
	private MySqlCommand MyCmd = new MySqlCommand();
    private DataSet DS;
    private DataTable dtReportTypes;
    private DataColumn RTCode;
    private DataColumn RTName;
    private DataColumn RTPrefix;
    private DataColumn RTSubject;
    private DataColumn RTClass;
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();

    private const string DSReports = "Inforoom.Reports.ReportTypes.DSReports";

    private void InitializeComponent()
    {
        this.DS = new System.Data.DataSet();
        this.dtReportTypes = new System.Data.DataTable();
        this.RTCode = new System.Data.DataColumn();
        this.RTName = new System.Data.DataColumn();
        this.RTPrefix = new System.Data.DataColumn();
        this.RTSubject = new System.Data.DataColumn();
        this.RTClass = new System.Data.DataColumn();
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtReportTypes)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtReportTypes});
        // 
        // dtReportTypes
        // 
        this.dtReportTypes.Columns.AddRange(new System.Data.DataColumn[] {
            this.RTCode,
            this.RTName,
            this.RTPrefix,
            this.RTSubject,
            this.RTClass});
        this.dtReportTypes.TableName = "dtReportTypes";
        // 
        // RTCode
        // 
        this.RTCode.ColumnName = "RTCode";
        this.RTCode.DataType = typeof(long);
        // 
        // RTName
        // 
        this.RTName.ColumnName = "RTName";
        // 
        // RTPrefix
        // 
        this.RTPrefix.ColumnName = "RTPrefix";
        // 
        // RTSubject
        // 
        this.RTSubject.ColumnName = "RTSubject";
        // 
        // RTClass
        // 
        this.RTClass.ColumnName = "RTClass";
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtReportTypes)).EndInit();

    }

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
			if (DS == null) // вероятно, сессия завершилась и все ее данные утеряны
				Reports_GeneralReports.Redirect(this);
        }
    	btnApply.Visible = dgvReportTypes.Rows.Count > 0;
    }

	private void PostData()
    {
        if(MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;
        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        DS.Tables[dtReportTypes.TableName].Clear();
        MyCmd.CommandText = @"
SELECT 
    ReportTypeCode as RTCode,
    ReportTypeName as RTName,
    ReportTypeFilePrefix as RTPrefix,
    AlternateSubject as RTSubject,
    ReportClassName as RTClass
FROM 
    reports.reporttypes rt
";
        MyDA.Fill(DS, dtReportTypes.TableName);
        MyCn.Close();
        
        Session.Add(DSReports, DS);
        dgvReportTypes.DataSource = DS;
        dgvReportTypes.DataMember = DS.Tables[dtReportTypes.TableName].TableName;
        dgvReportTypes.DataBind();
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
    reports.reporttypes 
SET 
    ReportTypeName = ?RTName,
    ReportTypeFilePrefix = ?RTPrefix,
    AlternateSubject = ?RTSubject,
    ReportClassName = ?RTClass
WHERE ReportTypeCode = ?RTCode", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("RTName", MySqlDbType.VarString));
            UpdCmd.Parameters["RTName"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["RTName"].SourceColumn = RTName.ColumnName;
            UpdCmd.Parameters["RTName"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("RTPrefix", MySqlDbType.VarString));
            UpdCmd.Parameters["RTPrefix"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["RTPrefix"].SourceColumn = RTPrefix.ColumnName;
            UpdCmd.Parameters["RTPrefix"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("RTSubject", MySqlDbType.VarString));
            UpdCmd.Parameters["RTSubject"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["RTSubject"].SourceColumn = RTSubject.ColumnName;
            UpdCmd.Parameters["RTSubject"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("RTClass", MySqlDbType.VarString));
            UpdCmd.Parameters["RTClass"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["RTClass"].SourceColumn = RTClass.ColumnName;
            UpdCmd.Parameters["RTClass"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("RTCode", MySqlDbType.Int64));
            UpdCmd.Parameters["RTCode"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["RTCode"].SourceColumn = RTCode.ColumnName;
            UpdCmd.Parameters["RTCode"].SourceVersion = DataRowVersion.Current;

            MySqlCommand DelCmd = new MySqlCommand(@"
DELETE from reports.reporttypes 
WHERE ReportTypeCode = ?RTDelCode", MyCn, trans);

            DelCmd.Parameters.Clear();
            DelCmd.Parameters.Add(new MySqlParameter("RTDelCode", MySqlDbType.Int64));
            DelCmd.Parameters["RTDelCode"].Direction = ParameterDirection.Input;
            DelCmd.Parameters["RTDelCode"].SourceColumn = RTCode.ColumnName;
            DelCmd.Parameters["RTDelCode"].SourceVersion = DataRowVersion.Original;

            MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO 
    reports.reporttypes 
SET 
    ReportTypeName = ?RTName,
    ReportTypeFilePrefix = ?RTPrefix,
    AlternateSubject = ?RTSubject,
    ReportClassName = ?RTClass", MyCn, trans);

            InsCmd.Parameters.Clear();
            InsCmd.Parameters.Add(new MySqlParameter("RTName", MySqlDbType.VarString));
            InsCmd.Parameters["RTName"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["RTName"].SourceColumn = RTName.ColumnName;
            InsCmd.Parameters["RTName"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("RTPrefix", MySqlDbType.VarString));
            InsCmd.Parameters["RTPrefix"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["RTPrefix"].SourceColumn = RTPrefix.ColumnName;
            InsCmd.Parameters["RTPrefix"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("RTSubject", MySqlDbType.VarString));
            InsCmd.Parameters["RTSubject"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["RTSubject"].SourceColumn = RTSubject.ColumnName;
            InsCmd.Parameters["RTSubject"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("RTClass", MySqlDbType.VarString));
            InsCmd.Parameters["RTClass"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["RTClass"].SourceColumn = RTClass.ColumnName;
            InsCmd.Parameters["RTClass"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("RTCode", MySqlDbType.Int64));
            InsCmd.Parameters["RTCode"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["RTCode"].SourceColumn = RTCode.ColumnName;
            InsCmd.Parameters["RTCode"].SourceVersion = DataRowVersion.Current;

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

            MyDA.Update(DS, DS.Tables[dtReportTypes.TableName].TableName);

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
        if (dgvReportTypes.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

     private void CopyChangesToTable()
    {
        foreach (GridViewRow dr in dgvReportTypes.Rows)
        {
            if (DS.Tables[dtReportTypes.TableName].DefaultView[dr.RowIndex][RTName.ColumnName].ToString() != ((TextBox)dr.FindControl("tbName")).Text)
                DS.Tables[dtReportTypes.TableName].DefaultView[dr.RowIndex][RTName.ColumnName] = ((TextBox)dr.FindControl("tbName")).Text;

            if (DS.Tables[dtReportTypes.TableName].DefaultView[dr.RowIndex][RTPrefix.ColumnName].ToString() != ((TextBox)dr.FindControl("tbPrefix")).Text)
                DS.Tables[dtReportTypes.TableName].DefaultView[dr.RowIndex][RTPrefix.ColumnName] = ((TextBox)dr.FindControl("tbPrefix")).Text;

            if (DS.Tables[dtReportTypes.TableName].DefaultView[dr.RowIndex][RTSubject.ColumnName].ToString() != ((TextBox)dr.FindControl("tbSubject")).Text)
                DS.Tables[dtReportTypes.TableName].DefaultView[dr.RowIndex][RTSubject.ColumnName] = ((TextBox)dr.FindControl("tbSubject")).Text;

            if (DS.Tables[dtReportTypes.TableName].DefaultView[dr.RowIndex][RTClass.ColumnName].ToString() != ((TextBox)dr.FindControl("tbClass")).Text)
                DS.Tables[dtReportTypes.TableName].DefaultView[dr.RowIndex][RTClass.ColumnName] = ((TextBox)dr.FindControl("tbClass")).Text;
        }
    }

    protected void dgvReportTypes_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Add")
        {
            CopyChangesToTable();

            DataRow dr = DS.Tables[dtReportTypes.TableName].NewRow();
            dr[RTName.ColumnName] = String.Empty;
            dr[RTPrefix.ColumnName] = String.Empty;
            dr[RTSubject.ColumnName] = String.Empty;
            dr[RTClass.ColumnName] = String.Empty;
            DS.Tables[dtReportTypes.TableName].Rows.Add(dr);

            dgvReportTypes.DataSource = DS;
            dgvReportTypes.DataBind();

            btnApply.Visible = true;
        }

    }
    protected void dgvReportTypes_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        CopyChangesToTable();
        DS.Tables[dtReportTypes.TableName].DefaultView[e.RowIndex].Delete();
        dgvReportTypes.DataSource = DS;
        dgvReportTypes.DataBind();

    }

    protected void dgvReportTypes_RowDataBound(object sender, GridViewRowEventArgs e)
    {
    }
}
