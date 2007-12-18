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
    private DataColumn PRID;
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

    int PP;
    private const string DSValues = "Inforoom.Reports.ReportPropertyValues.DSValues";
	private DataColumn LReportCaption;
	private DataColumn LReportType;
    private const string PPCN = "Inforoom.Reports.ReportPropertyValues.PP";

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
            try
            {
                PP = Convert.ToInt32(Request.Cookies[PPCN].Value);
            }
            catch
            {
                PP = 10;
            }
            dgvListValues.PageSize = PP;
            ddlPages.Text = PP.ToString();
            Response.Cookies[PPCN].Value = PP.ToString();
            Response.Cookies[PPCN].Expires = DateTime.Now.AddYears(2);

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
  rp.ID as LReportPropertyID,
  r.ReportCaption LReportCaption,
  rt.ReportTypeName LReportType
from 
  report_properties rp, 
  report_type_properties rtp, 
  reports r, 
  general_reports gr,
  reporttypes rt
where 
    rtp.ID=rp.PropertyID
and rtp.ReportTypeCode = r.ReportTypeCode
and r.generalreportcode=gr.generalreportcode
and gr.generalreportcode=?r
and rp.ID=?rpv
and rt.ReportTypeCode = r.ReportTypeCode
";
            MyDA.Fill(DS, dtList.TableName);
            lblListName.Text = DS.Tables[dtList.TableName].Rows[0][LName.ColumnName].ToString();
			lblReportCaption.Text = DS.Tables[dtList.TableName].Rows[0][LReportCaption.ColumnName].ToString();
			lblReportType.Text = DS.Tables[dtList.TableName].Rows[0][LReportType.ColumnName].ToString();
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
        if (dgvListValues.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    private void PostData()
    {
        FillFromProc();
        FillEnabled();
        Session[DSValues] = DS;
		ApplyFilter();
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

        foreach (DataRow drEnabled in DS.Tables[dtEnabledValues.TableName].Rows)
        {
            DataRow[] dr = DS.Tables[dtProcResult.TableName].Select("ID = " + drEnabled[EVName.ColumnName].ToString());
            if(dr.Length > 0)
                dr[0][Enabled.ColumnName] = 1;
        }
        DS.Tables[dtProcResult.TableName].AcceptChanges();
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
		this.PRID = new System.Data.DataColumn();
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
		this.LReportCaption = new System.Data.DataColumn();
		this.LReportType = new System.Data.DataColumn();
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
            this.PRID,
            this.DisplayValue,
            this.Enabled});
		this.dtProcResult.TableName = "dtProcResult";
		// 
		// PRID
		// 
		this.PRID.ColumnName = "ID";
		this.PRID.DataType = typeof(long);
		// 
		// DisplayValue
		// 
		this.DisplayValue.ColumnName = "DisplayValue";
		// 
		// Enabled
		// 
		this.Enabled.ColumnName = "Enabled";
		this.Enabled.DataType = typeof(byte);
		this.Enabled.DefaultValue = ((byte)(0));
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
            this.LReportPropertyID,
            this.LReportCaption,
            this.LReportType});
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
		// 
		// LReportCaption
		// 
		this.LReportCaption.ColumnName = "LReportCaption";
		// 
		// LReportType
		// 
		this.LReportType.ColumnName = "LReportType";
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtProcResult)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtEnabledValues)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtList)).EndInit();

    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        ShowData();
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
            MyCmd.Parameters.Add("inFilter", null);
            MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
            MyCmd.Parameters.Add("inID", null);
            MyCmd.Parameters["inID"].Direction = ParameterDirection.Input;
            MyCmd.CommandText = ListProc;
            MyCmd.CommandType = CommandType.StoredProcedure;
            MyDA.Fill(DS, dtProcResult.TableName);

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

    private void CopyChangesToTable()
    {
        CheckBox cb;
        DataRow[] drProc;
        HtmlInputHidden ih;
        foreach (GridViewRow dr in dgvListValues.Rows)
        {
            if (dr.Visible == true)
            {
                ih = (HtmlInputHidden)dr.FindControl("RowID");
                cb = (CheckBox)dr.FindControl("chbEnabled");
                drProc = DS.Tables[dtProcResult.TableName].Select("ID = " + ih.Value);
                if (drProc.Length == 1)
                {
                    if (Convert.ToBoolean(drProc[0][Enabled.ColumnName]) != cb.Checked)
                        drProc[0][Enabled.ColumnName] = Convert.ToInt32(cb.Checked);
                }
            }
        } 
    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        CopyChangesToTable();
        string ins = String.Empty;
        string del = String.Empty;

        foreach (DataRow dr in DS.Tables[dtProcResult.TableName].Rows)
        {
            if (dr.RowState == DataRowState.Modified)
            {
                if (dr[Enabled.ColumnName, DataRowVersion.Original].ToString() == dr[Enabled.ColumnName, DataRowVersion.Current].ToString())
                    dr.RejectChanges();
            }
        }

        MySqlTransaction trans;
        MyCn.Open();
        trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            MySqlCommand UpdCmd = new MySqlCommand(@"
insert into report_property_values
(ReportPropertyID, Value)
select r.ID, ?Value
from
  report_properties r
where
 r.ID = ?RPID
 and ?Enabled = 1;
delete from report_property_values
where
    ReportPropertyID = ?RPID
and Value = ?Value
and ?Enabled = 0;", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("Value", MySqlDbType.Int64));
            UpdCmd.Parameters["Value"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["Value"].SourceColumn = PRID.ColumnName;
            UpdCmd.Parameters["Value"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("Enabled", MySqlDbType.Byte));
            UpdCmd.Parameters["Enabled"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["Enabled"].SourceColumn = Enabled.ColumnName;
            UpdCmd.Parameters["Enabled"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("RPID", ReportPropertyID));

            MyDA.UpdateCommand = UpdCmd;

            string strHost = HttpContext.Current.Request.UserHostAddress;
            string strUser = HttpContext.Current.User.Identity.Name;
            if (strUser.StartsWith("ANALIT\\"))
            {
                strUser = strUser.Substring(7);
            }
            MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

            MyDA.Update(DS, DS.Tables[dtProcResult.TableName].TableName);

            trans.Commit();

            DS.Tables[dtProcResult.TableName].AcceptChanges();
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

        if (dgvListValues.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

    protected void chbShowEnabled_CheckedChanged(object sender, EventArgs e)
    {
        ShowData();
    }

    protected void ddlPages_SelectedIndexChanged(object sender, EventArgs e)
    {
        ShowData();
    }

    protected void dgvListValues_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        dgvListValues.DataSource = DS;
        dgvListValues.DataMember = dtProcResult.TableName;
        dgvListValues.PageIndex = e.NewPageIndex;
        dgvListValues.DataBind();
    }

	private void ApplyFilter()
	{
		PP = Convert.ToInt32(ddlPages.SelectedValue);
		dgvListValues.PageSize = PP;
		if (dgvListValues.PageCount - 1 <= dgvListValues.PageIndex)
			dgvListValues.PageIndex = 0;
		string Filter = String.Empty;
		if (tbSearch.Text == String.Empty)
			Filter += String.Empty;
		else
			Filter = "DisplayValue like '%" + tbSearch.Text + "%'";

		if (!chbShowEnabled.Checked)
		{
			Filter += String.Empty;
		}
		else
		{
			if (Filter != String.Empty)
				Filter += " and ";
			Filter += "Enabled = 1";
		}

		DS.Tables[dtProcResult.TableName].DefaultView.RowFilter = Filter;

		if (Filter != String.Empty)
		{
			dgvListValues.DataSource = DS.Tables[dtProcResult.TableName].DefaultView;
			dgvListValues.DataMember = null;
		}
		else
		{
			dgvListValues.DataSource = DS;
			dgvListValues.DataMember = dtProcResult.TableName;
		}

		dgvListValues.DataBind();
		Response.Cookies[PPCN].Value = PP.ToString();
		Response.Cookies[PPCN].Expires = DateTime.Now.AddYears(2);
	}

    private void ShowData()
    {
        CopyChangesToTable();
		ApplyFilter();
    }

	protected void tbSearch_TextChanged(object sender, EventArgs e)
	{
		ShowData();
	}
}
