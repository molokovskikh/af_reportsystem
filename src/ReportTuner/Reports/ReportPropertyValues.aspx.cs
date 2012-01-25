using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using MySql.Data.MySqlClient;

public partial class Reports_ReportPropertyValues : Page
{
	private MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
	private MySqlCommand MyCmd = new MySqlCommand();
	private MySqlDataAdapter MyDA = new MySqlDataAdapter();

	string ListProc = String.Empty;
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
	private DataColumn LProc;
	private DataColumn LName;
	private DataColumn LReportPropertyID;

	int PP;
	private const string DSValues = "Inforoom.Reports.ReportPropertyValues.DSValues";
	private DataColumn LReportCaption;
	private DataColumn LReportType;
	private const string PPCN = "Inforoom.Reports.ReportPropertyValues.PP";

	private string inFilter; // параметры для хранимых процедур
	private long? inID = null;

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

		if (String.IsNullOrEmpty(Request["rpv"]))
			if (!String.IsNullOrEmpty(Request["r"]))
				Response.Redirect(String.Format("ReportProperties.aspx?r={0}&rp={1}", Request["r"], Request["rp"]));
			else
				Response.Redirect(String.Format("ReportProperties.aspx?TemporaryId={0}&rp={1}", Request["TemporaryId"], Request["rp"]));
		if(!String.IsNullOrEmpty(Request["inID"]))
		{
			long id;
			if (long.TryParse(Request["inID"], out id)) 
				inID = id;
		}

		if(!String.IsNullOrEmpty(Request["inFilter"])) {
			inFilter = Request["inFilter"];
		}

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
			MyCmd.Parameters.AddWithValue("rpv", Request["rpv"]);
			MyCmd.Parameters.AddWithValue("r", (!String.IsNullOrEmpty(Request["r"])) ? Request["r"] : Request["TemporaryId"]);
			MyCmd.CommandText = @"
select
  rtp.displayname as LName,
  rtp.selectstoredprocedure as LProc,
  rp.ID as LReportPropertyID,
  r.ReportCaption LReportCaption,
  rt.ReportTypeName LReportType
from 
  reports.report_properties rp, 
  reports.report_type_properties rtp, 
  reports.reports r, 
  reports.general_reports gr,
  reports.reporttypes rt
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
			ReportPropertyID = Convert.ToInt64(DS.Tables[dtList.TableName].Rows[0][LReportPropertyID.ColumnName]);

			MyCn.Close();
			PostData();
		}
		else
		{
			DS = ((DataSet)Session[DSValues]);
			if (DS == null) // вероятно, сессия завершилась и все ее данные утеряны
				Reports_GeneralReports.Redirect(this);
			ListProc = DS.Tables[dtList.TableName].Rows[0][LProc.ColumnName].ToString();
			ReportPropertyID = Convert.ToInt64(DS.Tables[dtList.TableName].Rows[0][LReportPropertyID.ColumnName]);
			dgvListValues.DataSource = DS.Tables[dtProcResult.TableName].DefaultView;
		}
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
		MyCmd.Parameters.AddWithValue("rpv", Request["rpv"]);
		DS.Tables[dtEnabledValues.TableName].Clear();
		MyCmd.CommandText = @"
SELECT
	rpv.ID as EVID,
	rpv.Value as EVName
FROM 
	reports.report_property_values rpv
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
			this.LProc,
			this.LName,
			this.LReportPropertyID,
			this.LReportCaption,
			this.LReportType});
		this.dtList.TableName = "dtList";
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
			MyCn.ChangeDatabase("reports");
			MyCmd.Connection = MyCn;
			MyDA.SelectCommand = MyCmd;
			DS.Tables[dtProcResult.TableName].Clear();
			MyCmd.Parameters.Clear();
			MyCmd.Parameters.AddWithValue("inFilter", String.IsNullOrEmpty(inFilter) ? null : inFilter);
			MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
			MyCmd.Parameters.AddWithValue("inID", !inID.HasValue ? null : inID);
			MyCmd.Parameters["inID"].Direction = ParameterDirection.Input;
			MyCmd.CommandText = ListProc;
			MyCmd.CommandType = CommandType.StoredProcedure;
			MyDA.Fill(DS, dtProcResult.TableName);
		}
		finally
		{
			if (db != String.Empty)
				MyCn.ChangeDatabase(db);
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
insert into reports.report_property_values
(ReportPropertyID, Value)
select r.ID, ?Value
from
  reports.report_properties r
where
 r.ID = ?RPID
 and ?Enabled = 1;
delete from reports.report_property_values
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
			MyCn.Close();
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
		CopyChangesToTable();
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

		dgvListValues.DataSource = DS.Tables[dtProcResult.TableName].DefaultView;

		dgvListValues.DataBind();

		if (dgvListValues.Rows.Count > 0)
			btnApply.Visible = true;
		else
			btnApply.Visible = false;

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

	protected void cbSet_CheckedChanged(object sender, EventArgs e)
	{
		foreach (DataRowView dr in DS.Tables[dtProcResult.TableName].DefaultView)
			dr[Enabled.ColumnName] = ((CheckBox)sender).Checked;
		ApplyFilter();
	}

	protected void dgvListValues_DataBound(object sender, EventArgs e)
	{
		if (dgvListValues.Rows.Count == 0) return;
		CheckBox cb = (CheckBox)dgvListValues.HeaderRow.Cells[0].FindControl("cbSet");
		DataRow[] drs = ((DataView)dgvListValues.DataSource).ToTable().Select("Enabled = 1");
		cb.Checked = (drs.Length == ((DataView)dgvListValues.DataSource).Count);
	}
}
