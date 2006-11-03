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
    private DataTable dtClients;
    private DataColumn CCaption;
    private DataColumn CFirmCode;

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
        MyCmd.Parameters.Add("Name", "'%" + Name + "%'");
        DS.Tables[dtClients.TableName].Clear();
        MyCmd.CommandText = @"
SELECT 
    cd.FirmCode as CFirmCode,
    concat(cd.FirmCode, '.', cd.ShortName) as CCaption
FROM 
     testreports.general_reports gr, usersettings.clientsdata cd
WHERE 
     cd.ShortName like ?Name
";
        MyDA.Fill(DS, DS.Tables[dtClients.TableName].TableName);
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
                ddlNames.DataSource = dtClients;
                ddlNames.DataTextField = "CCaption";
                ddlNames.DataValueField = "CFirmCode";
                ddlNames.DataBind();
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
    testreports.general_reports 
SET 
    Allow = ?GRAllow,
    EMailAddress = ?GRAddress,
    EMailSubject = ?GRSubject,
    ReportFileName = ?GRFileName,
    ReportArchName = ?GRArchName,
WHERE GeneralReportCode = ?GRCode", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("GRAllow", MySqlDbType.Byte));
            UpdCmd.Parameters["GRAllow"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRAllow"].SourceColumn = GRAllow.ColumnName;
            UpdCmd.Parameters["GRAllow"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("GRAddress", MySqlDbType.VarString));
            UpdCmd.Parameters["GRAddress"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRAddress"].SourceColumn = GRAddress.ColumnName;
            UpdCmd.Parameters["GRAddress"].SourceVersion = DataRowVersion.Current;
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
DELETE from testreports.general_reports 
WHERE GeneralReportCode = ?GRDelCode", MyCn, trans);

            DelCmd.Parameters.Clear();
            DelCmd.Parameters.Add(new MySqlParameter("GRDelCode", MySqlDbType.Int64));
            DelCmd.Parameters["GRDelCode"].Direction = ParameterDirection.Input;
            DelCmd.Parameters["GRDelCode"].SourceColumn = GRCode.ColumnName;
            DelCmd.Parameters["GRDelCode"].SourceVersion = DataRowVersion.Original;

            MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO 
    testreports.general_reports 
SET 
    Allow = ?GRAllow,
    EMailAddress = ?GRAddress,
    EMailSubject = ?GRSubject,
    ReportFileName = ?GRFileName,
    ReportArchName = ?GRArchName,
", MyCn, trans);

            InsCmd.Parameters.Clear();
            InsCmd.Parameters.Add(new MySqlParameter("GRAllow", MySqlDbType.Byte));
            InsCmd.Parameters["GRAllow"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["GRAllow"].SourceColumn = GRAllow.ColumnName;
            InsCmd.Parameters["GRAllow"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("GRAddress", MySqlDbType.VarString));
            InsCmd.Parameters["GRAddress"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["GRAddress"].SourceColumn = GRAddress.ColumnName;
            InsCmd.Parameters["GRAddress"].SourceVersion = DataRowVersion.Current;
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

            MyDA.Update(DS, DS.Tables[dtGeneralReports.TableName].TableName);

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
        if (dgvReports.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }
}
