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

public partial class Reports_EnumValues : System.Web.UI.Page
{
    protected MySqlConnection MyCn = new MySqlConnection("server=testSQL.analit.net; user id=system; password=123;");
    protected MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataTable dtEnumValues;
    private DataColumn evID;
    private DataColumn evPEID;
    private DataColumn evValue;
    private DataColumn evDisplayValue;
    private DataSet DS;

    private const string DSEnumValues = "Inforoom.Reports.EnumValues.DSEnumValues";

    protected void Page_Init(object sender, System.EventArgs e)
    {
        InitializeComponent();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!(Page.IsPostBack))
        {
            MyCn.Open();
            MyCmd.Connection = MyCn;
            MyDA.SelectCommand = MyCmd;
            MyCmd.Parameters.Clear();
            MyCmd.Parameters.Add("evPECode", Request["e"]);
            MyCmd.CommandText = @"
SELECT 
    EnumName
FROM 
    testreports.property_enums pe
WHERE ID = ?evPECode
";
            lblEnumName.Text = MyCmd.ExecuteScalar().ToString();
            MyCn.Close();
            PostData();
        }
        else
            DS = ((DataSet)Session[DSEnumValues]);
    }

    private void PostData()
    {
        if (MyCn.State != ConnectionState.Open)
            MyCn.Open();
        MyCmd.Connection = MyCn;
        MyDA.SelectCommand = MyCmd;
        MyCmd.Parameters.Clear();
        MyCmd.Parameters.Add("evPECode", Request["e"]);
        DS.Tables[dtEnumValues.TableName].Clear();
        MyCmd.CommandText = @"
SELECT 
    ID as evID,
    PropertyEnumID as evPEID,
    Value as evValue,
    DisplayValue as evDisplayValue
FROM 
    testreports.enum_values ev
WHERE PropertyEnumID = ?evPECode
";
        MyDA.Fill(DS, dtEnumValues.TableName);

        MyCn.Close();

        dgvEnumValues.DataSource = DS;
        dgvEnumValues.DataMember = DS.Tables[dtEnumValues.TableName].TableName;
        dgvEnumValues.DataBind();
        Session[DSEnumValues] = DS;

    }

    private void InitializeComponent()
    {
        this.DS = new System.Data.DataSet();
        this.dtEnumValues = new System.Data.DataTable();
        this.evID = new System.Data.DataColumn();
        this.evPEID = new System.Data.DataColumn();
        this.evValue = new System.Data.DataColumn();
        this.evDisplayValue = new System.Data.DataColumn();
        ((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtEnumValues)).BeginInit();
        // 
        // DS
        // 
        this.DS.DataSetName = "NewDataSet";
        this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtEnumValues});
        // 
        // dtEnumValues
        // 
        this.dtEnumValues.Columns.AddRange(new System.Data.DataColumn[] {
            this.evID,
            this.evPEID,
            this.evValue,
            this.evDisplayValue});
        this.dtEnumValues.TableName = "dtEnumValues";
        // 
        // evID
        // 
        this.evID.ColumnName = "evID";
        this.evID.DataType = typeof(long);
        // 
        // evPEID
        // 
        this.evPEID.ColumnName = "evPEID";
        this.evPEID.DataType = typeof(long);
        // 
        // evValue
        // 
        this.evValue.ColumnName = "evValue";
        // 
        // evDisplayValue
        // 
        this.evDisplayValue.ColumnName = "evDisplayValue";
        ((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dtEnumValues)).EndInit();

    }

    protected void dgvEnumValues_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Add")
        {
            CopyChangesToTable();

            DS.Tables[dtEnumValues.TableName].Rows.Add(DS.Tables[dtEnumValues.TableName].NewRow());
            dgvEnumValues.DataSource = DS;
            dgvEnumValues.DataBind();
        }
    }

    protected void dgvEnumValues_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        CopyChangesToTable();
        DS.Tables[dtEnumValues.TableName].DefaultView[e.RowIndex].Delete();
        dgvEnumValues.DataSource = DS;
        dgvEnumValues.DataBind();
    }

    private void CopyChangesToTable()
    {
        foreach (GridViewRow dr in dgvEnumValues.Rows)
        {
            if (DS.Tables[dtEnumValues.TableName].DefaultView[dr.RowIndex][evValue.ColumnName].ToString() != ((TextBox)dr.FindControl("tbValue")).Text)
                DS.Tables[dtEnumValues.TableName].DefaultView[dr.RowIndex][evValue.ColumnName] = ((TextBox)dr.FindControl("tbValue")).Text;

            if (DS.Tables[dtEnumValues.TableName].DefaultView[dr.RowIndex][evDisplayValue.ColumnName].ToString() != ((TextBox)dr.FindControl("tbDisplayValue")).Text)
                DS.Tables[dtEnumValues.TableName].DefaultView[dr.RowIndex][evDisplayValue.ColumnName] = ((TextBox)dr.FindControl("tbDisplayValue")).Text;
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
    testreports.enum_values 
SET 
    Value = ?evValue,
    DisplayValue = ?evDisplayValue
WHERE ID = ?evID", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("evValue", MySqlDbType.VarString));
            UpdCmd.Parameters["evValue"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["evValue"].SourceColumn = evValue.ColumnName;
            UpdCmd.Parameters["evValue"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("evDisplayValue", MySqlDbType.VarString));
            UpdCmd.Parameters["evDisplayValue"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["evDisplayValue"].SourceColumn = evDisplayValue.ColumnName;
            UpdCmd.Parameters["evDisplayValue"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("evID", MySqlDbType.Int64));
            UpdCmd.Parameters["evID"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["evID"].SourceColumn = evID.ColumnName;
            UpdCmd.Parameters["evID"].SourceVersion = DataRowVersion.Current;

            MySqlCommand DelCmd = new MySqlCommand(@"
DELETE from testreports.enum_values 
WHERE ID = ?evDelID", MyCn, trans);

            DelCmd.Parameters.Clear();
            DelCmd.Parameters.Add(new MySqlParameter("evDelID", MySqlDbType.Int64));
            DelCmd.Parameters["evDelID"].Direction = ParameterDirection.Input;
            DelCmd.Parameters["evDelID"].SourceColumn = evID.ColumnName;
            DelCmd.Parameters["evDelID"].SourceVersion = DataRowVersion.Original;

            MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO 
    testreports.enum_values 
SET 
    Value = ?evValue,
    DisplayValue = ?evDisplayValue,
    PropertyEnumID = ?evPEID", MyCn, trans);

            InsCmd.Parameters.Clear();
            InsCmd.Parameters.Add(new MySqlParameter("evValue", MySqlDbType.VarString));
            InsCmd.Parameters["evValue"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["evValue"].SourceColumn = evValue.ColumnName;
            InsCmd.Parameters["evValue"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("evDisplayValue", MySqlDbType.VarString));
            InsCmd.Parameters["evDisplayValue"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["evDisplayValue"].SourceColumn = evDisplayValue.ColumnName;
            InsCmd.Parameters["evDisplayValue"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("evPEID", Request["e"]));

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

            MyDA.Update(DS, DS.Tables[dtEnumValues.TableName].TableName);

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
    }
    protected void dgvEnumValues_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (dgvEnumValues.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }
}
