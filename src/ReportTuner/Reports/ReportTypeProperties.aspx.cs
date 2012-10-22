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
using Common.MySql;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

public enum GridViewFields : int
{
	PID,
	PRTCode,
	PName,
	PDisplayName,
	PType,
	PDefaultValue,
	PEnumID,
	POptional,
	PStoredProc
}

public partial class Reports_ReportTypeProperties : System.Web.UI.Page
{
	private MySqlConnection MyCn = new MySqlConnection(ConnectionHelper.GetConnectionString());
	private MySqlCommand MyCmd = new MySqlCommand();
	private MySqlDataAdapter MyDA = new MySqlDataAdapter();
	private DataSet DS;
	private DataTable dtProperties;
	private DataColumn PID;
	private DataColumn PRTCode;
	private DataColumn PName;
	private DataColumn PDisplayName;
	private DataColumn PType;
	private DataColumn POptional;
	private DataColumn PEnumID;
	private DataColumn PStoredProc;
	public DataTable dtParamTypes;
	public DataTable dtEnumTypes;
	private DataColumn PDefaultValue;

	private const string DSReportTypes = "Inforoom.Reports.ReportTypeProperties.DSReportTypes";

	protected void Page_Init(object sender, System.EventArgs e)
	{
		InitializeComponent();

		dtParamTypes = new DataTable("ParamTypes");
		dtParamTypes.Columns.Add("ptName", typeof(string));
		dtParamTypes.Columns.Add("ptDisplayName", typeof(string));

		dtParamTypes.Rows.Add(new object[] { "BOOL", "Логический" });
		dtParamTypes.Rows.Add(new object[] { "INT", "Целый" });
		dtParamTypes.Rows.Add(new object[] { "ENUM", "Перечислимый" });
		dtParamTypes.Rows.Add(new object[] { "LIST", "Список" });
		dtParamTypes.Rows.Add(new object[] { "STRING", "Строковый" });
		dtParamTypes.Rows.Add(new object[] { "DATETIME", "Дата" });


		//foreach (GridViewRow gvr in dgvProperties.Rows)
		//{
		//    ((DropDownList)gvr.FindControl("ddlType")).DataSource = dtParamTypes;
		//    ((DropDownList)gvr.FindControl("ddlType")).DataTextField = "ptDisplayName";
		//    ((DropDownList)gvr.FindControl("ddlType")).DataValueField = "ptName";
		//}
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		if (Request["rtc"] == null)
			Response.Redirect("ReportTypes.aspx");
		FillTypes();
		if (!(Page.IsPostBack)) {
			MyCn.Open();
			MyCmd.Connection = MyCn;
			MyDA.SelectCommand = MyCmd;
			MyCmd.Parameters.Clear();
			MyCmd.Parameters.AddWithValue("rtCode", Request["rtc"]);
			MyCmd.CommandText = @"
SELECT
    ReportTypeName
FROM
    reports.reporttypes rt
WHERE ReportTypeCode = ?rtCode
";
			lblReportName.Text = MyCmd.ExecuteScalar().ToString();
			MyCn.Close();

			PostData();
		}
		else {
			DS = ((DataSet)Session[DSReportTypes]);
			if (DS == null) // вероятно, сессия завершилась и все ее данные утеряны
				Reports_GeneralReports.Redirect(this);
		}
		btnApply.Visible = dgvProperties.Rows.Count > 0;
	}

	private void PostData()
	{
		if (MyCn.State != ConnectionState.Open)
			MyCn.Open();
		MyCmd.Connection = MyCn;
		MyDA.SelectCommand = MyCmd;
		MyCmd.Parameters.Clear();
		MyCmd.CommandType = CommandType.Text;
		MyCmd.Parameters.AddWithValue("rtCode", Request["rtc"]);
		DS.Tables[dtProperties.TableName].Clear();
		MyCmd.CommandText = @"
SELECT
    ID as PID,
    ReportTypeCode as PRTCode,
    PropertyName as PName,
    DisplayName as PDisplayName,
    DefaultValue as PDefaultValue,
    PropertyType as PType,
    Optional as POptional,
    PropertyEnumID as PEnumID,
    SelectStoredProcedure as PStoredProc
FROM
    reports.report_type_properties rtc
WHERE ReportTypeCode = ?rtCode
";
		MyDA.Fill(DS, dtProperties.TableName);

		MyCn.Close();

		dgvProperties.DataSource = DS;
		dgvProperties.DataMember = DS.Tables[dtProperties.TableName].TableName;
		dgvProperties.DataBind();
		Session[DSReportTypes] = DS;
	}

	private void FillTypes()
	{
		if (MyCn.State != ConnectionState.Open)
			MyCn.Open();
		MyCmd.Connection = MyCn;

		dtEnumTypes = new DataTable("EnumTypes");
		dtEnumTypes.Columns.Add("etID", typeof(int));
		dtEnumTypes.Columns.Add("etName", typeof(string));

		MyCmd.CommandText = @"
SELECT
    ID as etID,
    EnumName as etName
from
    reports.property_enums";

		MyDA.SelectCommand = MyCmd;
		MyDA.Fill(dtEnumTypes);

		MyCn.Close();
	}

	protected void btnApply_Click(object sender, EventArgs e)
	{
		if (Page.IsValid) {
			CopyChangesToTable();

			MySqlTransaction trans;
			MyCn.Open();
			trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
			try {
				MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE
    reports.report_type_properties
SET
    PropertyName = ?PName,
    DisplayName = ?PDisplayName,
    DefaultValue = ?PDefaultValue,
    PropertyType = ?PType,
    Optional = ?POptional,
    PropertyEnumID = ?PEnumID,
    SelectStoredProcedure = ?PStoredProc
WHERE ID = ?PID", MyCn, trans);

				UpdCmd.Parameters.Clear();
				UpdCmd.Parameters.Add(new MySqlParameter("PName", MySqlDbType.VarString));
				UpdCmd.Parameters["PName"].Direction = ParameterDirection.Input;
				UpdCmd.Parameters["PName"].SourceColumn = PName.ColumnName;
				UpdCmd.Parameters["PName"].SourceVersion = DataRowVersion.Current;
				UpdCmd.Parameters.Add(new MySqlParameter("PDisplayName", MySqlDbType.VarString));
				UpdCmd.Parameters["PDisplayName"].Direction = ParameterDirection.Input;
				UpdCmd.Parameters["PDisplayName"].SourceColumn = PDisplayName.ColumnName;
				UpdCmd.Parameters["PDisplayName"].SourceVersion = DataRowVersion.Current;
				UpdCmd.Parameters.Add(new MySqlParameter("PDefaultValue", MySqlDbType.VarString));
				UpdCmd.Parameters["PDefaultValue"].Direction = ParameterDirection.Input;
				UpdCmd.Parameters["PDefaultValue"].SourceColumn = PDefaultValue.ColumnName;
				UpdCmd.Parameters["PDefaultValue"].SourceVersion = DataRowVersion.Current;
				UpdCmd.Parameters.Add(new MySqlParameter("PType", MySqlDbType.VarString));
				UpdCmd.Parameters["PType"].Direction = ParameterDirection.Input;
				UpdCmd.Parameters["PType"].SourceColumn = PType.ColumnName;
				UpdCmd.Parameters["PType"].SourceVersion = DataRowVersion.Current;
				UpdCmd.Parameters.Add(new MySqlParameter("POptional", MySqlDbType.Byte));
				UpdCmd.Parameters["POptional"].Direction = ParameterDirection.Input;
				UpdCmd.Parameters["POptional"].SourceColumn = POptional.ColumnName;
				UpdCmd.Parameters["POptional"].SourceVersion = DataRowVersion.Current;
				UpdCmd.Parameters.Add(new MySqlParameter("PEnumID", MySqlDbType.Int64));
				UpdCmd.Parameters["PEnumID"].Direction = ParameterDirection.Input;
				UpdCmd.Parameters["PEnumID"].SourceColumn = PEnumID.ColumnName;
				UpdCmd.Parameters["PEnumID"].SourceVersion = DataRowVersion.Current;
				UpdCmd.Parameters.Add(new MySqlParameter("PStoredProc", MySqlDbType.VarString));
				UpdCmd.Parameters["PStoredProc"].Direction = ParameterDirection.Input;
				UpdCmd.Parameters["PStoredProc"].SourceColumn = PStoredProc.ColumnName;
				UpdCmd.Parameters["PStoredProc"].SourceVersion = DataRowVersion.Current;
				UpdCmd.Parameters.Add(new MySqlParameter("PID", MySqlDbType.Int64));
				UpdCmd.Parameters["PID"].Direction = ParameterDirection.Input;
				UpdCmd.Parameters["PID"].SourceColumn = PID.ColumnName;
				UpdCmd.Parameters["PID"].SourceVersion = DataRowVersion.Current;

				MySqlCommand DelCmd = new MySqlCommand(@"
DELETE from reports.report_type_properties
WHERE ID = ?PDelID", MyCn, trans);

				DelCmd.Parameters.Clear();
				DelCmd.Parameters.Add(new MySqlParameter("PDelID", MySqlDbType.Int64));
				DelCmd.Parameters["PDelID"].Direction = ParameterDirection.Input;
				DelCmd.Parameters["PDelID"].SourceColumn = PID.ColumnName;
				DelCmd.Parameters["PDelID"].SourceVersion = DataRowVersion.Original;

				MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO
    reports.report_type_properties
SET
    PropertyName = ?PName,
    DisplayName = ?PDisplayName,
    DefaultValue = ?PDefaultValue,
    PropertyType = ?PType,
    PropertyEnumID = ?PEnumID,
    Optional = ?POptional,
    SelectStoredProcedure = ?PStoredProc,
    ReportTypeCode = ?rtc
", MyCn, trans);

				InsCmd.Parameters.Clear();
				InsCmd.Parameters.Add(new MySqlParameter("PName", MySqlDbType.VarString));
				InsCmd.Parameters["PName"].Direction = ParameterDirection.Input;
				InsCmd.Parameters["PName"].SourceColumn = PName.ColumnName;
				InsCmd.Parameters["PName"].SourceVersion = DataRowVersion.Current;
				InsCmd.Parameters.Add(new MySqlParameter("PDisplayName", MySqlDbType.VarString));
				InsCmd.Parameters["PDisplayName"].Direction = ParameterDirection.Input;
				InsCmd.Parameters["PDisplayName"].SourceColumn = PDisplayName.ColumnName;
				InsCmd.Parameters["PDisplayName"].SourceVersion = DataRowVersion.Current;
				InsCmd.Parameters.Add(new MySqlParameter("PDefaultValue", MySqlDbType.VarString));
				InsCmd.Parameters["PDefaultValue"].Direction = ParameterDirection.Input;
				InsCmd.Parameters["PDefaultValue"].SourceColumn = PDefaultValue.ColumnName;
				InsCmd.Parameters["PDefaultValue"].SourceVersion = DataRowVersion.Current;
				InsCmd.Parameters.Add(new MySqlParameter("PType", MySqlDbType.VarString));
				InsCmd.Parameters["PType"].Direction = ParameterDirection.Input;
				InsCmd.Parameters["PType"].SourceColumn = PType.ColumnName;
				InsCmd.Parameters["PType"].SourceVersion = DataRowVersion.Current;
				InsCmd.Parameters.Add(new MySqlParameter("POptional", MySqlDbType.Byte));
				InsCmd.Parameters["POptional"].Direction = ParameterDirection.Input;
				InsCmd.Parameters["POptional"].SourceColumn = POptional.ColumnName;
				InsCmd.Parameters["POptional"].SourceVersion = DataRowVersion.Current;
				InsCmd.Parameters.Add(new MySqlParameter("PEnumID", MySqlDbType.Int64));
				InsCmd.Parameters["PEnumID"].Direction = ParameterDirection.Input;
				InsCmd.Parameters["PEnumID"].SourceColumn = PEnumID.ColumnName;
				InsCmd.Parameters["PEnumID"].SourceVersion = DataRowVersion.Current;
				InsCmd.Parameters.Add(new MySqlParameter("PStoredProc", MySqlDbType.VarString));
				InsCmd.Parameters["PStoredProc"].Direction = ParameterDirection.Input;
				InsCmd.Parameters["PStoredProc"].SourceColumn = PStoredProc.ColumnName;
				InsCmd.Parameters["PStoredProc"].SourceVersion = DataRowVersion.Current;
				InsCmd.Parameters.Add(new MySqlParameter("rtc", Request["rtc"]));

				MyDA.UpdateCommand = UpdCmd;
				MyDA.DeleteCommand = DelCmd;
				MyDA.InsertCommand = InsCmd;

				string strHost = HttpContext.Current.Request.UserHostAddress;
				string strUser = HttpContext.Current.User.Identity.Name;
				if (strUser.StartsWith("ANALIT\\")) {
					strUser = strUser.Substring(7);
				}
				MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

				MyDA.Update(DS, DS.Tables[dtProperties.TableName].TableName);

				trans.Commit();

				PostData();
			}
			catch {
				trans.Rollback();
				throw;
			}
			finally {
				MyCn.Close();
			}
			btnApply.Visible = dgvProperties.Rows.Count > 0;
		}
	}

	private void CopyChangesToTable()
	{
		foreach (GridViewRow dr in dgvProperties.Rows) {
			if (DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex].Row.RowState == DataRowState.Added) {
				if (DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PName.ColumnName].ToString() != ((TextBox)dr.FindControl("tbName")).Text)
					DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PName.ColumnName] = ((TextBox)dr.FindControl("tbName")).Text;

				if (DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PDisplayName.ColumnName].ToString() != ((TextBox)dr.FindControl("tbDisplayName")).Text)
					DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PDisplayName.ColumnName] = ((TextBox)dr.FindControl("tbDisplayName")).Text;

				if (DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PType.ColumnName].ToString() != ((DropDownList)dr.FindControl("ddlType")).SelectedValue)
					DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PType.ColumnName] = ((DropDownList)dr.FindControl("ddlType")).SelectedValue;

				if (((DropDownList)dr.FindControl("ddlType")).SelectedValue == "ENUM") {
					if (DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PEnumID.ColumnName].ToString() != ((DropDownList)dr.FindControl("ddlEnum")).SelectedValue)
						DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PEnumID.ColumnName] = ((DropDownList)dr.FindControl("ddlEnum")).SelectedValue;
				}
				else {
					DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PEnumID.ColumnName] = DBNull.Value;
				}
			}

			if (DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PDefaultValue.ColumnName].ToString() != ((TextBox)dr.FindControl("tbDefault")).Text)
				DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PDefaultValue.ColumnName] = ((TextBox)dr.FindControl("tbDefault")).Text;

			if (DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][POptional.ColumnName].ToString() != Convert.ToByte(((CheckBox)dr.FindControl("chbOptional")).Checked).ToString())
				DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][POptional.ColumnName] = Convert.ToByte(((CheckBox)dr.FindControl("chbOptional")).Checked);

			if (DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PStoredProc.ColumnName].ToString() != ((TextBox)dr.FindControl("tbProc")).Text)
				DS.Tables[dtProperties.TableName].DefaultView[dr.RowIndex][PStoredProc.ColumnName] = ((TextBox)dr.FindControl("tbProc")).Text;
		}
	}
	#region Component Designer generated code
	private void InitializeComponent()
	{
		this.DS = new System.Data.DataSet();
		this.dtProperties = new System.Data.DataTable();
		this.PID = new System.Data.DataColumn();
		this.PRTCode = new System.Data.DataColumn();
		this.PName = new System.Data.DataColumn();
		this.PDisplayName = new System.Data.DataColumn();
		this.PType = new System.Data.DataColumn();
		this.POptional = new System.Data.DataColumn();
		this.PEnumID = new System.Data.DataColumn();
		this.PStoredProc = new System.Data.DataColumn();
		this.PDefaultValue = new System.Data.DataColumn();
		((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtProperties)).BeginInit();
		//
		// DS
		//
		this.DS.DataSetName = "NewDataSet";
		this.DS.Tables.AddRange(new System.Data.DataTable[] {
			this.dtProperties
		});
		//
		// dtProperties
		//
		this.dtProperties.Columns.AddRange(new System.Data.DataColumn[] {
			this.PID,
			this.PRTCode,
			this.PName,
			this.PDisplayName,
			this.PType,
			this.POptional,
			this.PEnumID,
			this.PStoredProc,
			this.PDefaultValue
		});
		this.dtProperties.TableName = "dtProperties";
		//
		// PID
		//
		this.PID.ColumnName = "PID";
		this.PID.DataType = typeof(long);
		//
		// PRTCode
		//
		this.PRTCode.ColumnName = "PRTCode";
		this.PRTCode.DataType = typeof(long);
		//
		// PName
		//
		this.PName.ColumnName = "PName";
		//
		// PDisplayName
		//
		this.PDisplayName.ColumnName = "PDisplayName";
		//
		// PType
		//
		this.PType.ColumnName = "PType";
		//
		// POptional
		//
		this.POptional.ColumnName = "POptional";
		this.POptional.DataType = typeof(byte);
		//
		// PEnumID
		//
		this.PEnumID.ColumnName = "PEnumID";
		this.PEnumID.DataType = typeof(long);
		//
		// PStoredProc
		//
		this.PStoredProc.ColumnName = "PStoredProc";
		//
		// PDefaultValue
		//
		this.PDefaultValue.ColumnName = "PDefaultValue";
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtProperties)).EndInit();
	}
	#endregion
	protected void dgvProperties_RowDataBound(object sender, GridViewRowEventArgs e)
	{
		if (e.Row.RowType == DataControlRowType.DataRow) {
			if (((Label)e.Row.Cells[(int)GridViewFields.PName].FindControl("lblName")).Text != "") {
				((TextBox)e.Row.Cells[(int)GridViewFields.PName].FindControl("tbName")).Visible = false;
				((Label)e.Row.Cells[(int)GridViewFields.PName].FindControl("lblName")).Visible = true;

				((TextBox)e.Row.Cells[(int)GridViewFields.PDisplayName].FindControl("tbDisplayName")).Visible = false;
				((Label)e.Row.Cells[(int)GridViewFields.PDisplayName].FindControl("lblDisplayName")).Visible = true;

				((CheckBox)e.Row.Cells[(int)GridViewFields.POptional].FindControl("chbOptional")).Visible = false;
				((Label)e.Row.Cells[(int)GridViewFields.POptional].FindControl("lblOptional")).Visible = true;

				if (((Label)e.Row.Cells[(int)GridViewFields.POptional].FindControl("lblOptional")).Text == "0")
					((Label)e.Row.Cells[(int)GridViewFields.POptional].FindControl("lblOptional")).Text = "Нет";
				else
					((Label)e.Row.Cells[(int)GridViewFields.POptional].FindControl("lblOptional")).Text = "Да";

				((DropDownList)(e.Row.Cells[(int)GridViewFields.PType].FindControl("ddlEnum"))).Visible = false;
				((Button)(e.Row.Cells[(int)GridViewFields.PType].FindControl("btnEditType"))).Visible = false;
				((DropDownList)(e.Row.Cells[(int)GridViewFields.PType].FindControl("ddlType"))).Visible = false;

				((Label)(e.Row.Cells[(int)GridViewFields.PType].FindControl("lblType"))).Visible = true;

				DataRow[] dtr = dtParamTypes.Select("ptName = '" + ((DataRowView)e.Row.DataItem)[PType.ColumnName] + "'");
				if (dtr.Length > 0) {
					((Label)(e.Row.Cells[(int)GridViewFields.PType].FindControl("lblType"))).Text = dtr[0]["ptDisplayName"].ToString();
					if (dtr[0]["ptName"].ToString() == "ENUM") {
						dtr = dtEnumTypes.Select("etID = " + ((DataRowView)e.Row.DataItem)[PEnumID.ColumnName]);
						((Label)(e.Row.Cells[(int)GridViewFields.PType].FindControl("lblType"))).Text += " - " + dtr[0]["etName"].ToString();
					}
				}
			}
			else {
				((TextBox)e.Row.Cells[(int)GridViewFields.PName].FindControl("tbName")).Visible = true;
				((Label)e.Row.Cells[(int)GridViewFields.PName].FindControl("lblName")).Visible = false;

				((TextBox)e.Row.Cells[(int)GridViewFields.PDisplayName].FindControl("tbDisplayName")).Visible = true;
				((Label)e.Row.Cells[(int)GridViewFields.PDisplayName].FindControl("lblDisplayName")).Visible = false;

				((CheckBox)e.Row.Cells[(int)GridViewFields.POptional].FindControl("chbOptional")).Visible = true;
				((Label)e.Row.Cells[(int)GridViewFields.POptional].FindControl("lblOptional")).Visible = false;
				((Label)(e.Row.Cells[(int)GridViewFields.PType].FindControl("lblType"))).Visible = false;

				((DropDownList)(e.Row.Cells[(int)GridViewFields.PType].FindControl("ddlEnum"))).Visible = true;
				DropDownList ddlTypes = ((DropDownList)e.Row.Cells[4].FindControl("ddlType"));
				ddlTypes.DataSource = dtParamTypes;
				ddlTypes.DataTextField = "ptDisplayName";
				ddlTypes.DataValueField = "ptName";
				if (!(((DataRowView)e.Row.DataItem)[PType.ColumnName] is DBNull))
					ddlTypes.SelectedValue = ((DataRowView)e.Row.DataItem)[PType.ColumnName].ToString();
				else
					ddlTypes.SelectedValue = "INT";
				ddlTypes.DataBind();

				if (((DropDownList)(e.Row.Cells[(int)GridViewFields.PType].FindControl("ddlType"))).SelectedValue == "ENUM") {
					((DropDownList)(e.Row.Cells[(int)GridViewFields.PType].FindControl("ddlEnum"))).Visible = true;
					((Button)(e.Row.Cells[(int)GridViewFields.PType].FindControl("btnEditType"))).Visible = true;
				}
				else {
					((DropDownList)(e.Row.Cells[(int)GridViewFields.PType].FindControl("ddlEnum"))).Visible = false;
					((Button)(e.Row.Cells[(int)GridViewFields.PType].FindControl("btnEditType"))).Visible = false;
				}

				if (e.Row.Cells[(int)GridViewFields.PType].FindControl("ddlEnum") != null) {
					DropDownList ddlEnums = ((DropDownList)e.Row.Cells[4].FindControl("ddlEnum"));
					ddlEnums.DataSource = dtEnumTypes;
					ddlEnums.DataTextField = "etName";
					ddlEnums.DataValueField = "etID";
					if (!(((DataRowView)e.Row.DataItem)[PEnumID.ColumnName] is DBNull))
						ddlEnums.SelectedValue = ((DataRowView)e.Row.DataItem)[PEnumID.ColumnName].ToString();
					ddlEnums.DataBind();
				}
			}
		}
	}

	protected void dgvProperties_RowCommand(object sender, GridViewCommandEventArgs e)
	{
		if (e.CommandName == "Add") {
			CopyChangesToTable();

			DataRow dr = DS.Tables[dtProperties.TableName].NewRow();
			dr[POptional.ColumnName] = 0;
			dr[PEnumID.ColumnName] = DBNull.Value;
			dr[PStoredProc.ColumnName] = String.Empty;

			DS.Tables[dtProperties.TableName].Rows.Add(dr);

			dgvProperties.DataSource = DS;
			dgvProperties.DataBind();

			btnApply.Visible = true;
		}
	}

	protected void dgvProperties_RowDeleting(object sender, GridViewDeleteEventArgs e)
	{
		CopyChangesToTable();
		DS.Tables[dtProperties.TableName].DefaultView[e.RowIndex].Delete();
		dgvProperties.DataSource = DS;
		dgvProperties.DataBind();
	}

	protected void btnEditType_Click(object sender, EventArgs e)
	{
		string url = String.Format("EnumValues.aspx?e={0}", ((DropDownList)(((Button)sender).Parent).FindControl("ddlEnum")).SelectedValue);
		Response.Redirect(url);
	}

	protected void ddlType_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (((DropDownList)sender).SelectedValue == "ENUM") {
			((DropDownList)(((DropDownList)sender).Parent).FindControl("ddlEnum")).Visible = true;
			((Button)(((DropDownList)sender).Parent).FindControl("btnEditType")).Visible = true;
		}
		else {
			((DropDownList)(((DropDownList)sender).Parent).FindControl("ddlEnum")).Visible = false;
			((Button)(((DropDownList)sender).Parent).FindControl("btnEditType")).Visible = false;
		}
	}

	protected void cvProc_ServerValidate(object source, ServerValidateEventArgs args)
	{
		TextBox tbProc = (TextBox)(((CustomValidator)source).Parent).FindControl("tbProc");
		string db = String.Empty;
		if ((tbProc).Text != String.Empty) {
			DataTable dt = new DataTable();

			try {
				if (MyCn.State != ConnectionState.Open)
					MyCn.Open();

				db = MyCn.Database;
				MyCn.ChangeDatabase("reports");
				MyCmd.Connection = MyCn;
				MyDA.SelectCommand = MyCmd;
				MyCmd.Parameters.Clear();
				//Если процедура есть, то она вернет какой-либо набор, возможно, пустой
				//Если не существует, то будет ошибка
				MyCmd.Parameters.AddWithValue("inFilter", null);
				MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
				MyCmd.CommandText = tbProc.Text;
				MyCmd.CommandType = CommandType.StoredProcedure;
				MyDA.Fill(dt);

				args.IsValid = true;
			}
			catch {
				args.IsValid = false;
			}
			finally {
				if (db != String.Empty)
					MyCn.ChangeDatabase(db);
				MyCn.Close();
			}
		}
	}
}