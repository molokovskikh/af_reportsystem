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

public partial class Reports_PropertyEnums : System.Web.UI.Page
{
    protected MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
    protected MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataSet DS;
    private DataTable dtEnums;
    private DataColumn eID;
    private DataColumn eName;

    private const string DSEnums = "Inforoom.Reports.PropertyEnums.DSEnums";

    protected void Page_Init(object sender, System.EventArgs e)
    {
        InitializeComponent();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            PostData();
            //dgReportTypes.DataSource = DS;
            //dgReportTypes.DataMember = DS.Tables[dtReportTypes.TableName].TableName;
        }
        else
        {
            DS = ((DataSet)Session[DSEnums]);
        }
        if (dgvEnums.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    private void PostData()
    {
        if(MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;
        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        DS.Tables[dtEnums.TableName].Clear();
        MyCmd.CommandText = @"
SELECT 
    ID as eID,
    EnumName as eName
FROM 
    testreports.property_enums pe
";
        MyDA.Fill(DS, dtEnums.TableName);
        MyCn.Close();

        Session.Add(DSEnums, DS);
        dgvEnums.DataSource = DS;
        dgvEnums.DataMember = DS.Tables[dtEnums.TableName].TableName;
        dgvEnums.DataBind();
    }

    private void InitializeComponent()
    {
        this.DS = new System.Data.DataSet();
        this.dtEnums = new System.Data.DataTable();
        this.eID = new System.Data.DataColumn();
        this.eName = new System.Data.DataColumn();
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtEnums)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtEnums});
        // 
        // dtEnums
        // 
        this.dtEnums.Columns.AddRange(new System.Data.DataColumn[] {
            this.eID,
            this.eName});
        this.dtEnums.TableName = "dtEnums";
        // 
        // eID
        // 
        this.eID.ColumnName = "eID";
        this.eID.DataType = typeof(long);
        // 
        // eName
        // 
        this.eName.ColumnName = "eName";
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtEnums)).EndInit();

    }
    protected void dgvEnums_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Add")
        {
            CopyChangesToTable();

            DataRow dr = DS.Tables[dtEnums.TableName].NewRow();
            DS.Tables[dtEnums.TableName].Rows.Add(dr);

            dgvEnums.DataSource = DS;
            dgvEnums.DataBind();

            btnApply.Visible = true;
        }
    }

    private void CopyChangesToTable()
    {
        foreach (GridViewRow dr in dgvEnums.Rows)
        {
            if (DS.Tables[dtEnums.TableName].DefaultView[dr.RowIndex][eName.ColumnName].ToString() != ((TextBox)dr.FindControl("tbEnumName")).Text)
                DS.Tables[dtEnums.TableName].DefaultView[dr.RowIndex][eName.ColumnName] = ((TextBox)dr.FindControl("tbEnumName")).Text;
        }
    }

    protected void dgvEnums_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        CopyChangesToTable();
        DS.Tables[dtEnums.TableName].DefaultView[e.RowIndex].Delete();
        dgvEnums.DataSource = DS;
        dgvEnums.DataBind();
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
    testreports.property_enums 
SET 
    EnumName = ?eName
WHERE ID = ?eID", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("eName", MySqlDbType.VarString));
            UpdCmd.Parameters["eName"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["eName"].SourceColumn = eName.ColumnName;
            UpdCmd.Parameters["eName"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("eID", MySqlDbType.Int64));
            UpdCmd.Parameters["eID"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["eID"].SourceColumn = eID.ColumnName;
            UpdCmd.Parameters["eID"].SourceVersion = DataRowVersion.Current;

            MySqlCommand DelCmd = new MySqlCommand(@"
DELETE from testreports.property_enums 
WHERE ID = ?eDelID", MyCn, trans);

            DelCmd.Parameters.Clear();
            DelCmd.Parameters.Add(new MySqlParameter("eDelID", MySqlDbType.Int64));
            DelCmd.Parameters["eDelID"].Direction = ParameterDirection.Input;
            DelCmd.Parameters["eDelID"].SourceColumn = eID.ColumnName;
            DelCmd.Parameters["eDelID"].SourceVersion = DataRowVersion.Original;

            MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO 
    testreports.property_enums 
SET 
    EnumName = ?eName", MyCn, trans);

            InsCmd.Parameters.Clear();
            InsCmd.Parameters.Add(new MySqlParameter("eName", MySqlDbType.VarString));
            InsCmd.Parameters["eName"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["eName"].SourceColumn = eName.ColumnName;
            InsCmd.Parameters["eName"].SourceVersion = DataRowVersion.Current;

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

            MyDA.Update(DS, DS.Tables[dtEnums.TableName].TableName);

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
            MyCmd.Dispose();
            MyCn.Close();
            MyCn.Dispose();
        }
        if (dgvEnums.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    protected void dgvEnums_RowDataBound(object sender, GridViewRowEventArgs e)
    {
    }
}
