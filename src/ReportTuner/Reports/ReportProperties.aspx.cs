﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Common.MySql;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using ReportTuner.Helpers;
using ReportTuner.Models;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;
using System.Globalization;

public partial class Reports_ReportProperties : BasePage
{
	private MySqlConnection MyCn = new MySqlConnection(ConnectionHelper.GetConnectionString());
	private MySqlCommand MyCmd = new MySqlCommand();
	private MySqlDataAdapter MyDA = new MySqlDataAdapter();
	private DataSet DS;
	private DataTable dtNonOptionalParams;
	private DataColumn PID;
	private DataColumn PParamName;
	private DataColumn PPropertyType;
	private DataColumn PPropertyValue;
	private DataColumn PPropertyEnumID;
	private DataColumn PReportTypeCode;
	private DataColumn PPropertyName;
	public DataTable dtEnumValues;
	private DataColumn PStoredProc;
	private DataColumn CReportCaption;
	private DataTable dtOptionalParams;
	private DataColumn OPID;
	private DataColumn OPParamName;
	private DataColumn OPPropertyType;
	private DataColumn OPPropertyValue;
	private DataColumn OPPropertyEnumID;
	private DataColumn OPStoredProc;
	private DataColumn OPPropertyName;
	private DataTable dtDDLOptionalParams;
	private DataColumn OPrtpID;
	private DataColumn OPReportTypeCode;
	private DataColumn CReportType;

	private const string DSParams = "Inforoom.Reports.ReportProperties.DSParams";
	private const string PropHelper = "Inforoom.Reports.ReportProperties.PropHelper";

	private PropertiesHelper propertiesHelper;

	protected void Page_Init(object sender, System.EventArgs e)
	{
		InitializeComponent();
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		decimal? asdf = 0.5m;

		var sdf = $"asdf {asdf.Value} sdff";

		if (String.IsNullOrEmpty(Request["r"]))
			Response.Redirect("GeneralReports.aspx");

		if (String.IsNullOrEmpty(Request["rp"]))
			if (!String.IsNullOrEmpty(Request["r"]))
				Response.Redirect("Reports.aspx?r=" + Request["r"]);

		btnBack.Visible = !String.IsNullOrEmpty(Request["TemporaryId"]);
		btnNext.Visible = btnBack.Visible;

		if (!IsPostBack) {
			var report = DbSession.Load<Report>(Convert.ToUInt64(Request["rp"]));
			lblReport.Text = report.ReportCaption;
			lblReportType.Text = report.ReportType.ReportTypeName;

			PostData();
			var property = report.Properties.FirstOrDefault(x => x.PropertyType.PropertyName == "ReportPeriod");
			if (property != null) {
				var value = (ReportPeriod)Convert.ToInt32(property.Value);
				SetRowVisibility("Интервал отчета (дни) от текущей даты", value == ReportPeriod.ByInterval);
			}
			propertiesHelper = new PropertiesHelper(report.Id, dtNonOptionalParams, dtOptionalParams);
			Session[PropHelper] = propertiesHelper;
		}
		else {
			DS = ((DataSet)Session[DSParams]);
			propertiesHelper = (PropertiesHelper)Session[PropHelper];
			if (DS == null || propertiesHelper == null) {
				// вероятно, сессия завершилась и все ее данные утеряны
				Reports_GeneralReports.Redirect(this);
			}
		}
		btnApply.Visible = dgvNonOptional.Rows.Count > 0;
	}

	private void PostData()
	{
		FillNonOptimal();
		FillOptimal();
		RecalculateOptionVisibility();
	}

	protected void FillNonOptimal()
	{
		if (MyCn.State != ConnectionState.Open)
			MyCn.Open();
		MyCmd.Connection = MyCn;

		MyDA.SelectCommand = MyCmd;
		MyCmd.Parameters.Clear();
		MyCmd.Parameters.AddWithValue("rp", Request["rp"]);
		DS.Tables[dtNonOptionalParams.TableName].Clear();
		MyCmd.CommandText = @"
SELECT
	rp.ID as PID,
	rtp.DisplayName as PParamName,
	rtp.PropertyType as PPropertyType,
	rp.PropertyValue as PPropertyValue,
	rtp.PropertyEnumID as PPropertyEnumID,
	rtp.selectstoredprocedure as PStoredProc,
	rtp.ReportTypeCode as PReportTypeCode,
	rtp.PropertyName as PPropertyName
FROM
	reports.report_properties rp, reports.report_type_properties rtp
WHERE
	rp.propertyID = rtp.ID
	AND rtp.Optional=0
	AND rp.reportCode=?rp
order by rtp.Position, rp.Id";
		MyDA.Fill(DS, dtNonOptionalParams.TableName);

		MyCn.Close();

		dgvNonOptional.DataSource = DS;
		dgvNonOptional.DataMember = DS.Tables[dtNonOptionalParams.TableName].TableName;
		dgvNonOptional.DataBind();
		Session[DSParams] = DS;
	}

	private void FillOptimal()
	{
		if (MyCn.State != ConnectionState.Open)
			MyCn.Open();
		MyCmd.Connection = MyCn;

		MyDA.SelectCommand = MyCmd;
		MyCmd.Parameters.Clear();
		MyCmd.Parameters.AddWithValue("rp", Request["rp"]);
		DS.Tables[dtOptionalParams.TableName].Clear();
		MyCmd.CommandText = @"
SELECT
	rp.ID as OPID,
	rtp.ID as OPrtpID,
	rtp.DisplayName as OPParamName,
	rtp.PropertyType as OPPropertyType,
	rp.PropertyValue as OPPropertyValue,
	rtp.PropertyEnumID as OPPropertyEnumID,
	rtp.selectstoredprocedure as OPStoredProc,
	rtp.ReportTypeCode as OPReportTypeCode,
	rtp.PropertyName as OPPropertyName

FROM
	reports.report_properties rp, reports.report_type_properties rtp
WHERE
	rp.propertyID = rtp.ID
	AND Optional=1
	AND rp.reportCode=?rp
order by rtp.Position
";
		MyDA.Fill(DS, dtOptionalParams.TableName);

		MyCn.Close();

		dgvOptional.DataSource = DS;
		dgvOptional.DataMember = DS.Tables[dtOptionalParams.TableName].TableName;
		dgvOptional.DataBind();
		Session[DSParams] = DS;
	}

	#region Component Designer generated code

	private void InitializeComponent()
	{
		this.DS = new System.Data.DataSet();
		this.dtNonOptionalParams = new System.Data.DataTable();
		this.PID = new System.Data.DataColumn();
		this.PParamName = new System.Data.DataColumn();
		this.PPropertyType = new System.Data.DataColumn();
		this.PPropertyValue = new System.Data.DataColumn();
		this.PPropertyEnumID = new System.Data.DataColumn();
		this.PPropertyName = new System.Data.DataColumn();
		this.PStoredProc = new System.Data.DataColumn();
		this.PReportTypeCode = new System.Data.DataColumn();
		this.CReportCaption = new System.Data.DataColumn();
		this.CReportType = new System.Data.DataColumn();
		this.dtOptionalParams = new System.Data.DataTable();
		this.OPID = new System.Data.DataColumn();
		this.OPParamName = new System.Data.DataColumn();
		this.OPPropertyType = new System.Data.DataColumn();
		this.OPPropertyValue = new System.Data.DataColumn();
		this.OPPropertyEnumID = new System.Data.DataColumn();
		this.OPPropertyName = new System.Data.DataColumn();
		this.OPStoredProc = new System.Data.DataColumn();
		this.OPrtpID = new System.Data.DataColumn();
		this.OPReportTypeCode = new System.Data.DataColumn();
		((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtNonOptionalParams)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtOptionalParams)).BeginInit();
		//
		// DS
		//
		this.DS.DataSetName = "NewDataSet";
		this.DS.Tables.AddRange(new System.Data.DataTable[] {
			this.dtNonOptionalParams,
			this.dtOptionalParams
		});
		//
		// dtNonOptionalParams
		//
		this.dtNonOptionalParams.Columns.AddRange(new System.Data.DataColumn[] {
			this.PID,
			this.PParamName,
			this.PPropertyType,
			this.PPropertyValue,
			this.PPropertyEnumID,
			this.PStoredProc,
			this.PReportTypeCode,
			this.PPropertyName
		});
		this.dtNonOptionalParams.TableName = "dtNonOptionalParams";
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
		// PPropertyName
		//
		this.PPropertyName.ColumnName = "PPropertyName";
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
		// PReportTypeCode
		//
		this.PReportTypeCode.ColumnName = "PReportTypeCode";
		this.PReportTypeCode.DataType = typeof(long);
		//
		// dtClient
		//
		//
		// CReportCaption
		//
		this.CReportCaption.ColumnName = "CReportCaption";
		//
		// CReportType
		//
		this.CReportType.ColumnName = "CReportType";
		//
		// dtOptionalParams
		//
		this.dtOptionalParams.Columns.AddRange(new System.Data.DataColumn[] {
			this.OPID,
			this.OPParamName,
			this.OPPropertyType,
			this.OPPropertyValue,
			this.OPPropertyEnumID,
			this.OPStoredProc,
			this.OPrtpID,
			this.OPReportTypeCode,
			this.OPPropertyName
		});
		this.dtOptionalParams.TableName = "dtOptionalParams";
		//
		// OPID
		//
		this.OPID.ColumnName = "OPID";
		this.OPID.DataType = typeof(long);
		//
		// OPParamName
		//
		this.OPParamName.ColumnName = "OPParamName";
		//
		// OPPropertyName
		//
		this.OPPropertyName.ColumnName = "OPPropertyName";
		//
		// OPPropertyType
		//
		this.OPPropertyType.ColumnName = "OPPropertyType";
		//
		// OPPropertyValue
		//
		this.OPPropertyValue.ColumnName = "OPPropertyValue";
		//
		// OPPropertyEnumID
		//
		this.OPPropertyEnumID.ColumnName = "OPPropertyEnumID";
		this.OPPropertyEnumID.DataType = typeof(long);
		//
		// OPStoredProc
		//
		this.OPStoredProc.ColumnName = "OPStoredProc";
		//
		// OPrtpID
		//
		this.OPrtpID.ColumnName = "OPrtpID";
		this.OPrtpID.DataType = typeof(long);
		//
		// OPReportTypeCode
		//
		this.OPReportTypeCode.ColumnName = "OPReportTypeCode";
		this.OPReportTypeCode.DataType = typeof(long);
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtNonOptionalParams)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtOptionalParams)).EndInit();
	}

	#endregion

	protected void dgvNonOptional_RowDataBound(object sender, GridViewRowEventArgs e)
	{
		ShowEditor(e.Row);
	}

	private void ShowEditor(GridViewRow rowView)
	{
		if (rowView.RowType != DataControlRowType.DataRow)
			return;

		var row = (DataRowView)rowView.DataItem;
		var reportProperty = GetReportProperty(row);
		var id = reportProperty.Id;
		var type = reportProperty.PropertyType.PropertyType;
		var value = reportProperty.Value;

		var cell = rowView.Cells[1];
		cell.Controls.Cast<Control>().OfType<WebControl>().Each(c => c.Visible = false);
		((Button)cell.FindControl("btnFind")).CommandArgument = rowView.RowIndex.ToString();
		((Button)cell.FindControl("btnListValue")).CommandArgument = id.ToString();

		if (type == "DATETIME") {
			cell.FindControl("tbDate").Visible = true;
			if (String.Equals(value, "NOW", StringComparison.OrdinalIgnoreCase))
				((TextBox)cell.FindControl("tbDate")).Text = DateTime.Now.ToString("yyyy-MM-dd");
			else
				((TextBox)cell.FindControl("tbDate")).Text = value;
		}
		else if (type == "BOOL") {
			cell.FindControl("chbValue").Visible = true;

			((CheckBox)cell.FindControl("chbValue")).Checked = Convert.ToBoolean(Convert.ToInt32(value));
		}
		else if (type == "ENUM") {
			var ddlValues = ((DropDownList)cell.FindControl("ddlValue"));
			ddlValues.Visible = true;
			if (reportProperty.PropertyType.DisplayName == "Пользователь") {
				var clientProperty = reportProperty.Report.Properties.FirstOrDefault(p => p.PropertyType.DisplayName == "Клиент");
				if (clientProperty == null)
					return;
				if (String.IsNullOrEmpty(clientProperty.Value))
					return;
				FillUserDDL(Convert.ToInt64(clientProperty.Value), ddlValues);
			} else {
				FillDDL(reportProperty.PropertyType.Enum.Id);
				ddlValues.DataSource = dtEnumValues;
				ddlValues.DataTextField = "evName";
				ddlValues.DataValueField = "evValue";
				if (!String.IsNullOrEmpty(value))
					ddlValues.SelectedValue = value;
				ddlValues.DataBind();
			}
		}
		else if (type == "INT") {
			if (String.IsNullOrEmpty(reportProperty.PropertyType.SelectStoredProcedure)) {
				if (row[1].ToString() == "Количество отображаемых цен")
					cell.FindControl("tbValueShort").Visible = true;
				else
					cell.FindControl("tbValue").Visible = true;
			}
			else if (!String.IsNullOrEmpty(value)) {
				cell.FindControl("ddlValue").Visible = true;

				var table = FillDDL(
					reportProperty.PropertyType.SelectStoredProcedure,
					"",
					value);
				ShowSearchedParam(
					((DropDownList)cell.FindControl("ddlValue")),
					((TextBox)cell.FindControl("tbSearch")),
					((Button)cell.FindControl("btnFind")),
					table,
					value);
			}
			else {
				cell.FindControl("tbSearch").Visible = true;
				cell.FindControl("btnFind").Visible = true;
			}
		}
		else if (type == "LIST") {
			cell.FindControl("btnListValue").Visible = true;
		}
		else if (type == "PERCENT") {
			cell.FindControl("percentPanel").Visible = true;
		}
		else {
			cell.FindControl("tbValue").Visible = true;
		}
	}

	private ReportProperty GetReportProperty(DataRowView row)
	{
		string columnName;
		if (row.Row.Table.Columns.Contains(PID.ColumnName))
			columnName = PID.ColumnName;
		else
			columnName = OPID.ColumnName;

		var id = Convert.ToUInt64(row[columnName]);
		var property = ReportProperty.Find(id);
		//тк мы во время одной сессии можем значала загрузить объект
		//потом с помощью dataset его обновить
		//повторного запроса к базе не будет тк объект уже в сессии
		//и мы увидем старые данные
		//что бы этого изюежать явно запрашиваем обновленные данные
		property.Refresh();
		return property;
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
		MyCmd.Parameters.AddWithValue("PEID", PropertyEnumID);
		MyCmd.CommandText = @"
SELECT distinct
	Value as evValue,
	DisplayValue as evName
FROM
	reports.report_type_properties rtp, reports.Property_Enums pe, reports.Enum_Values ev
WHERE
	rtp.PropertyEnumID = pe.ID
AND pe.ID = ev.PropertyEnumID
AND rtp.PropertyEnumID=?PEID
";
		MyDA.Fill(dtEnumValues);

		MyCn.Close();
	}

	private void ApplyNonOptimal(MySqlTransaction trans)
	{
		MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE
	reports.report_properties
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
		if (strUser.StartsWith("ANALIT\\")) {
			strUser = strUser.Substring(7);
		}
		MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

		MyDA.Update(DS, DS.Tables[dtNonOptionalParams.TableName].TableName);
	}

	private void ApplyOptimal(MySqlTransaction trans)
	{
		MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE
	reports.report_properties
SET
	PropertyValue = ?OPPropertyValue
WHERE ID = ?OPID", MyCn, trans);

		UpdCmd.Parameters.Clear();
		UpdCmd.Parameters.Add(new MySqlParameter("OPID", MySqlDbType.Int64));
		UpdCmd.Parameters["OPID"].Direction = ParameterDirection.Input;
		UpdCmd.Parameters["OPID"].SourceColumn = OPID.ColumnName;
		UpdCmd.Parameters["OPID"].SourceVersion = DataRowVersion.Current;
		UpdCmd.Parameters.Add(new MySqlParameter("OPPropertyValue", MySqlDbType.VarString));
		UpdCmd.Parameters["OPPropertyValue"].Direction = ParameterDirection.Input;
		UpdCmd.Parameters["OPPropertyValue"].SourceColumn = OPPropertyValue.ColumnName;
		UpdCmd.Parameters["OPPropertyValue"].SourceVersion = DataRowVersion.Current;

		MySqlCommand InsCmd = new MySqlCommand(@"
INSERT
	reports.report_properties
SET
	ReportCode = ?rp,
	PropertyID = ?OPrtpID,
	PropertyValue = ?OPPropertyValue
", MyCn, trans);

		InsCmd.Parameters.Clear();
		InsCmd.Parameters.Add(new MySqlParameter("OPID", MySqlDbType.Int64));
		InsCmd.Parameters["OPID"].Direction = ParameterDirection.Input;
		InsCmd.Parameters["OPID"].SourceColumn = OPID.ColumnName;
		InsCmd.Parameters["OPID"].SourceVersion = DataRowVersion.Current;
		InsCmd.Parameters.Add(new MySqlParameter("OPPropertyValue", MySqlDbType.VarString));
		InsCmd.Parameters["OPPropertyValue"].Direction = ParameterDirection.Input;
		InsCmd.Parameters["OPPropertyValue"].SourceColumn = OPPropertyValue.ColumnName;
		InsCmd.Parameters["OPPropertyValue"].SourceVersion = DataRowVersion.Current;
		InsCmd.Parameters.Add(new MySqlParameter("OPrtpID", MySqlDbType.Int64));
		InsCmd.Parameters["OPrtpID"].Direction = ParameterDirection.Input;
		InsCmd.Parameters["OPrtpID"].SourceColumn = OPrtpID.ColumnName;
		InsCmd.Parameters["OPrtpID"].SourceVersion = DataRowVersion.Current;
		InsCmd.Parameters.Add(new MySqlParameter("rp", Request["rp"]));

		MySqlCommand DelCmd = new MySqlCommand(@"
DELETE FROM
	reports.report_properties
WHERE ID = ?OPID", MyCn, trans);

		DelCmd.Parameters.Clear();
		DelCmd.Parameters.Add(new MySqlParameter("OPID", MySqlDbType.Int64));
		DelCmd.Parameters["OPID"].Direction = ParameterDirection.Input;
		DelCmd.Parameters["OPID"].SourceColumn = OPID.ColumnName;
		DelCmd.Parameters["OPID"].SourceVersion = DataRowVersion.Original;

		MyDA.UpdateCommand = UpdCmd;
		MyDA.InsertCommand = InsCmd;
		MyDA.DeleteCommand = DelCmd;

		string strHost = HttpContext.Current.Request.UserHostAddress;
		string strUser = HttpContext.Current.User.Identity.Name;
		if (strUser.StartsWith("ANALIT\\")) {
			strUser = strUser.Substring(7);
		}
		MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

		MyDA.Update(DS, DS.Tables[dtOptionalParams.TableName].TableName);
	}

	protected void btnApply_Click(object sender, EventArgs e)
	{
		CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
		CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

		MyCn.Open();

		var drows = DS.Tables[dtOptionalParams.TableName].Rows.Cast<DataRow>().Where(dr => (dr.RowState == DataRowState.Added) && (dr[OPrtpID.ColumnName] is DBNull)).ToArray();
		for (var i = 0; i < drows.Count(); i++)
			DS.Tables[dtOptionalParams.TableName].Rows.Remove(drows[i]);

		foreach (DataRow dr in DS.Tables[dtOptionalParams.TableName].Rows) {
			if (dr.RowState == DataRowState.Added) {
				dr[OPPropertyValue.ColumnName] = MySqlHelper.ExecuteScalar(MyCn, "SELECT DefaultValue FROM reports.report_type_properties WHERE ID=" + dr[OPrtpID.ColumnName]);
			}
		}
		MyCn.Close();

		MyCn.Open();
		var trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
		try {
			var requiredParameters = DS.Tables[dtNonOptionalParams.TableName];

			var baseCostRows = requiredParameters.Select("PPropertyName = 'ByBaseCosts'");
			var weightCostRows = requiredParameters.Select("PPropertyName = 'ByWeightCosts'");
			var clientRows = requiredParameters.Select("PPropertyName = 'ClientCode'");
			if (baseCostRows.Length > 0 && weightCostRows.Length > 0 && clientRows.Length > 0) {
				if (baseCostRows[0]["PPropertyValue"].ToString() == "1"
					|| weightCostRows[0]["PPropertyValue"].ToString() == "1") {
					clientRows[0]["PPropertyValue"] = 1;
				}
			}

			ApplyNonOptimal(trans);
			ApplyOptimal(trans);

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
		if (dgvNonOptional.Rows.Count > 0)
			btnApply.Visible = true;
		else
			btnApply.Visible = false;
	}

	private void CopyChangesToTable(GridView dgv, DataTable dt, string column)
	{
		foreach (GridViewRow dr in dgv.Rows) {
			var row = DS.Tables[dt.TableName].DefaultView[dr.RowIndex];
			var value = row[column];
			if (dr.FindControl("ddlValue").Visible) {
				if (dr.FindControl("ddlValue") != null) {
					if (((DropDownList)dr.FindControl("ddlValue")).SelectedValue != null)
						if (value.ToString() !=
							((DropDownList)dr.FindControl("ddlValue")).SelectedValue)
							row[column] =
								((DropDownList)dr.FindControl("ddlValue")).SelectedValue;
				}
			}
			else if ((dr.FindControl("chbValue")).Visible
				|| (row[1].ToString().Contains("По базовым ценам")
					|| row[1].ToString().Contains("По взвешенным ценам"))) {
				if (value.ToString() !=
					((CheckBox)dr.FindControl("chbValue")).Checked.ToString())
					row[column] =
						Convert.ToInt32(((CheckBox)dr.FindControl("chbValue")).Checked).ToString();
			}
			else if ((dr.FindControl("tbDate")).Visible) {
				if (value.ToString() != ((TextBox)dr.FindControl("tbDate")).Text)
					row[column] = ((TextBox)dr.FindControl("tbDate")).Text;
			}
			else if ((dr.FindControl("tbValue")).Visible) {
				if (value.ToString() !=
					((TextBox)dr.FindControl("tbValue")).Text)
					row[column] = ((TextBox)dr.FindControl("tbValue")).Text;
			}
			else if ((dr.FindControl("percentPanel")).Visible) {
				var valTxt = ((TextBox)dr.FindControl("tbValuePercent")).Text.Replace(",", ".");
				var val = 0m;
				if (value.ToString() != valTxt && decimal.TryParse(valTxt, NumberStyles.Number, CultureInfo.InvariantCulture, out val)) {
					row[column] = valTxt;
				}
			}
			else if ((dr.FindControl("tbValueShort")).Visible) {
				if (value.ToString() !=
					((TextBox)dr.FindControl("tbValueShort")).Text)
					row[column] = ((TextBox)dr.FindControl("tbValueShort")).Text;
			}
			else {
				var allControls = dr.Controls.Cast<Control>().Flat(c => c.Controls.Cast<Control>());
				var file = allControls.OfType<FileUpload>().Where(u => u.Visible).Where(f => f.HasFile).FirstOrDefault();
				if (file != null)
					row[column] = Path.GetFileName(file.FileName);
			}

			if (dgv == dgvOptional) {
				if ((dr.FindControl("ddlName")).Visible) {
					if (((DropDownList)dr.FindControl("ddlName")).SelectedValue != null) {
						if (row[OPrtpID.ColumnName].ToString() !=
							((DropDownList)dr.FindControl("ddlName")).SelectedValue) {
							row[OPrtpID.ColumnName] =
								((DropDownList)dr.FindControl("ddlName")).SelectedValue;
						}
					}
				}
			}
		}
	}

	private DataTable FillDDL(string proc, string filter, string id)
	{
		string db = String.Empty;
		try {
			if (MyCn.State != ConnectionState.Open)
				MyCn.Open();
			db = MyCn.Database;
			MyCn.ChangeDatabase("reports");
			MyCmd.Connection = MyCn;
			MyDA.SelectCommand = MyCmd;
			MyCmd.Parameters.Clear();
			MyCmd.Parameters.AddWithValue("inFilter", filter);
			MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
			if (id == String.Empty)
				MyCmd.Parameters.AddWithValue("inID", DBNull.Value);
			else
				MyCmd.Parameters.AddWithValue("inID", Convert.ToInt64(id));
			MyCmd.Parameters["inID"].Direction = ParameterDirection.Input;
			MyCmd.CommandText = proc;
			MyCmd.CommandType = CommandType.StoredProcedure;
			var table = new DataTable();
			MyDA.Fill(table);
			return table;
		}
		finally {
			if (db != String.Empty)
				MyCn.ChangeDatabase(db);
			MyCmd.CommandType = CommandType.Text;
			MyCn.Close();
		}
	}

	protected void dgvNonOptional_RowCommand(object sender, GridViewCommandEventArgs e)
	{
		if (e.CommandName == "Find") {
			CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
			CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

			var ddlValues = ((DropDownList)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("ddlValue"));
			var tbFind = ((TextBox)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("tbSearch"));
			var btnFind = ((Button)dgvNonOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("btnFind"));

			var table = FillDDL(
				DS.Tables[dtNonOptionalParams.TableName].DefaultView[Convert.ToInt32(e.CommandArgument)][PStoredProc.ColumnName].ToString(),
				tbFind.Text,
				String.Empty);
			ShowSearchedParam(ddlValues, tbFind, btnFind, table);
		}
		else if (e.CommandName == "ShowValues")
			ShowValues(e);
	}

	private void ShowValues(GridViewCommandEventArgs e)
	{
		CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
		CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

		var url = String.Empty;
		var prop = ReportProperty.Find(Convert.ToUInt64(e.CommandArgument));
		propertiesHelper = (PropertiesHelper)Session[PropHelper];
		var result = propertiesHelper.GetRelativeValue(prop);

		if (!String.IsNullOrEmpty(Request["TemporaryId"]))
			url = String.Format("ReportPropertyValues.aspx?TemporaryId={0}&rp={1}&rpv={2}",
				Request["TemporaryId"],
				Request["rp"],
				e.CommandArgument);
		else if (prop.IsSupplierEditor())
			url = String.Format("../ReportsTuning/SelectClients.rails?r={0}&report={1}&rpv={2}&firmType=0",
				Request["r"],
				Request["rp"],
				e.CommandArgument);
		// http://redmine.analit.net/issues/52707
		else if (prop.PropertyType.PropertyName == "FirmCodeEqual2")
		{
			url = String.Format("../ReportsTuning/SelectClients.rails?r={0}&report={1}&rpv={2}&sortOrder=3&firmType=0",
				Request["r"],
				Request["rp"],
				e.CommandArgument);
			var propClients = prop.Report.Properties.SingleOrDefault(x => x.PropertyType.PropertyName == "Clients");
			if (propClients != null)
			{
				var clientId = propClients.Values.FirstOrDefault();
				if (clientId != null)
				{
					var maskRegion = Client.Find(Convert.ToUInt32(clientId.Value)).MaskRegion;
					url += $"&region={maskRegion}";
				}
			}
		}
		else if (prop.IsClientEditor())
			url = String.Format("../ReportsTuning/SelectClients.rails?r={0}&report={1}&rpv={2}&firmType=1",
				Request["r"],
				Request["rp"],
				e.CommandArgument);
		else if (prop.IsAddressesEditor())
			url = String.Format("../ReportsTuning/SelectAddresses.rails?filter.GeneralReport={0}&filter.Report={1}&filter.ReportPropertyValue={2}",
				Request["r"],
				Request["rp"],
				e.CommandArgument);
		else
			url = String.Format("ReportPropertyValues.aspx?r={0}&rp={1}&rpv={2}",
				Request["r"],
				Request["rp"],
				e.CommandArgument);

		if (!String.IsNullOrEmpty(result))
			url = String.Format("{0}&{1}", url, result);

		Response.Redirect(url);
	}

	protected void ddlValue_SelectedIndexChanged(object sender, EventArgs e)
	{
		var dropDown = (DropDownList)sender;
		var property = ElToProperty(dropDown);
		if (property?.PropertyType.PropertyName == "ReportPeriod") {
			var value = (ReportPeriod)Convert.ToInt32(dropDown.SelectedValue);
			SetRowVisibility("Интервал отчета (дни) от текущей даты", value == ReportPeriod.ByInterval);
		}
		if (dropDown.SelectedValue == "-1") {
			dropDown.Visible = false;
			((TextBox)dropDown.Parent.FindControl("tbSearch")).Visible = true;
			((TextBox)dropDown.Parent.FindControl("tbSearch")).Text = string.Empty;
			((Button)dropDown.Parent.FindControl("btnFind")).Visible = true;
		}
		foreach (GridViewRow dr in dgvNonOptional.Rows) {
			if (dr.Cells[0].Text == "Клиент") {
				DropDownList ddl = (DropDownList)dr.Cells[1].FindControl("ddlValue");
				if (ddl.UniqueID == dropDown.UniqueID) {
					foreach (GridViewRow dro in dgvOptional.Rows) {
						if (((Label)dro.Cells[0].FindControl("lblName")).Text == "Пользователь") {
							DropDownList ddlo = (DropDownList)dro.Cells[1].FindControl("ddlValue");
							FillUserDDL(Convert.ToInt64(ddl.SelectedValue), ddlo);
						}
					}
				}
			}
		}
	}

	private ReportProperty ElToProperty(DropDownList el)
	{
		var index = dgvNonOptional.Rows.OfType<GridViewRow>()
			.IndexOf(x => x.Cells[1].FindControl("ddlValue")?.UniqueID == el.UniqueID);
		if (index < 0)
			return null;
		return DbSession.Load<ReportProperty>(Convert.ToUInt64(DS.Tables[dtNonOptionalParams.TableName].DefaultView[index]["PID"]));
	}

	protected object FindCheckBoxByKey(string key)
	{
		foreach (GridViewRow dr in dgvNonOptional.Rows) {
			if (dr.Cells[0].Text == key) {
				return dr.Cells[1].FindControl("chbValue");
			}
		}
		return null;
	}

	protected void chbValue_CheckedChanged(object sender, EventArgs e)
	{
		RecalculateOptionVisibility();
	}

	private void RecalculateOptionVisibility()
	{
		var base_costs = GetValueByLabel(dgvNonOptional.Rows, "По базовым ценам");
		var byPreviousMonth = GetValueByLabel(dgvNonOptional.Rows, "За предыдущий месяц");
		var weight_costs = GetValueByLabel(dgvNonOptional.Rows, "По взвешенным ценам");

		SetRowVisibility("Список значений &quot;Прайс&quot;", base_costs);
		SetRowVisibility("Список значений &quot;Региона&quot;", base_costs || weight_costs);
		SetRowVisibility("Клиент", !base_costs && !weight_costs);
		SetRowEnablity("Готовить по розничному сегменту", !base_costs);
		SetRowVisibility("Интервал отчета (дни) от текущей даты", !byPreviousMonth);
		SetRowVisibility("По взвешенным ценам", !base_costs);
		SetRowVisibility("По базовым ценам", !weight_costs);
		SetRowVisibility("Продукты без учета свойств (все цвета\\вкусы объединены)", !weight_costs);

		dgvOptional.DataSource = DS;
		dgvOptional.DataBind();
		SetOptionalVisibility();
	}

	private void SetOptionalVisibility()
	{
		var base_costs = GetValueByLabel(dgvNonOptional.Rows, "По базовым ценам");
		var weight_costs = GetValueByLabel(dgvNonOptional.Rows, "По взвешенным ценам");
		SetOptionalRowVisibility("Пользователь", !weight_costs && !base_costs);
		SetOptionalRowVisibility("Список доступных клиенту регионов", !weight_costs && !base_costs);

		SetOptionalRowVisibility("Зашумлять все прайс листы всех поставщиков кроме", !weight_costs);
		SetOptionalRowVisibility("Список значений \"Прайс\"", !weight_costs);
		SetOptionalRowVisibility("Список исключений &quot;Прайс&quot;", !weight_costs);
	}

	private void FilterDDLOptional()
	{
		var base_costs = GetValueByLabel(dgvNonOptional.Rows, "По базовым ценам");
		var weight_costs = GetValueByLabel(dgvNonOptional.Rows, "По взвешенным ценам");
		DataRow[] forRemove = new DataRow[0];
		if (base_costs) {
			forRemove = dtDDLOptionalParams.Select(String.Format("opName = '{0}' or opName = '{1}'",
				"Пользователь",
				"Список доступных клиенту регионов"));
		}
		else if (weight_costs) {
			forRemove = dtDDLOptionalParams.Select(String.Format("opName in ('{0}','{1}','{2}','{3}','{4}')",
				"Пользователь",
				"Список доступных клиенту регионов",
				"Зашумлять все прайс листы всех поставщиков кроме",
				"Список значений \"Прайс\"",
				"Список исключений \"Прайс\""));
		}
		else {
			var last = dtDDLOptionalParams.Select(String.Format("opName = 'Список доступных клиенту регионов'"));
			if (last.Length > 0) {
				var newRow = dtDDLOptionalParams.NewRow();
				newRow[0] = last[0][0];
				newRow[1] = last[0][1];
				newRow[2] = last[0][2];
				dtDDLOptionalParams.Rows.Remove(last[0]);
				dtDDLOptionalParams.Rows.Add(newRow);
			}
		}

		foreach (var dataRow in forRemove) {
			dtDDLOptionalParams.Rows.Remove(dataRow);
		}
	}

	private void SetRowVisibility(string label, bool visible)
	{
		foreach (GridViewRow dr in dgvNonOptional.Rows) {
			if (dr.Cells.Count < 1)
				continue;

			var cell = dr.Cells[0];
			if (cell.Text == label)
				dr.Visible = visible;
		}
	}

	private void SetOptionalRowVisibility(string label, bool visible)
	{
		foreach (GridViewRow dr in dgvOptional.Rows) {
			if (((Label)dr.FindControl("lblName")).Text == label)
				dr.Visible = visible;
		}
	}

	private void SetRowEnablity(string label, bool enable)
	{
		foreach (GridViewRow dr in dgvNonOptional.Rows) {
			if (dr.Cells.Count < 1)
				continue;

			var cell = dr.Cells[0];
			if (cell.Text == label)
				dr.Enabled = enable;
		}
	}

	private bool GetValueByLabel(GridViewRowCollection rows, string label)
	{
		bool value = false;
		foreach (GridViewRow dr in dgvNonOptional.Rows) {
			if (dr.Cells.Count > 1 && dr.Cells[0].Text == label) {
				var chk = (CheckBox)dr.Cells[1].FindControl("chbValue");
				value = chk.Checked;
				break;
			}
		}
		return value;
	}

	private void ShowSearchedParam(DropDownList ddl, TextBox tb, Button btn, DataTable data, string value = null)
	{
		if (data.Rows.Count > 0) {
			ddl.Visible = true;
			tb.Visible = false;
			btn.Visible = false;
			ddl.DataSource = data;
			ddl.DataTextField = "DisplayValue";
			ddl.DataValueField = "ID";
			ddl.DataBind();
			var li = new ListItem {
				Text = "<изменить>",
				Value = "-1"
			};
			ddl.Items.Insert(0, li);
			if (String.IsNullOrEmpty(value))
				ddl.SelectedIndex = 1;
			else
				ddl.SelectedValue = value;
		}
		else {
			ddl.Visible = false;
			tb.Visible = true;
			tb.Text = String.Empty;
			btn.Visible = true;
		}
	}

	protected void dgvOptional_RowCommand(object sender, GridViewCommandEventArgs e)
	{
		if (e.CommandName == "Find") {
			CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
			CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

			var ddlValues = ((DropDownList)dgvOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("ddlValue"));
			var tbFind = ((TextBox)dgvOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("tbSearch"));
			var btnFind = ((Button)dgvOptional.Rows[Convert.ToInt32(e.CommandArgument)].FindControl("btnFind"));

			var table = FillDDL(
				DS.Tables[dtOptionalParams.TableName].DefaultView[Convert.ToInt32(e.CommandArgument)][OPStoredProc.ColumnName].ToString(),
				tbFind.Text,
				String.Empty);
			ShowSearchedParam(ddlValues, tbFind, btnFind, table);
		}
		else if (e.CommandName == "ShowValues")
			ShowValues(e);
		else if (e.CommandName == "Add") {
			var addedExist = DS.Tables[dtOptionalParams.TableName].Rows.Cast<DataRow>().Any(dr => dr.RowState == DataRowState.Added);
			if (!addedExist) {
				CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
				CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);

				var dr = DS.Tables[dtOptionalParams.TableName].NewRow();
				DS.Tables[dtOptionalParams.TableName].Rows.Add(dr);

				dgvOptional.DataSource = DS;

				dgvOptional.DataBind();

				btnApply.Visible = true;
			}
		}
		SetOptionalVisibility();
	}

	private void FillUserDDL(long clientID, DropDownList ddl)
	{
		if (clientID < 0)
			clientID = 0;
		var users = ReportTuner.Models.User.Queryable.Where(u => u.Client.Id == clientID).ToList().OrderBy(u => u.ShortNameAndId).ToList();
		ddl.DataSource = users;
		ddl.DataTextField = "ShortNameAndId";
		ddl.DataValueField = "Id";
		ddl.DataBind();
		var reportCode = Convert.ToUInt64(Request["rp"]);
		var property = ReportProperty.Queryable.FirstOrDefault(p =>
			p.Report.Id == reportCode && p.PropertyType.PropertyName == "UserCode");
		if (property == null)
			return;
		if (String.IsNullOrEmpty(property.Value))
			return;

		var user = users.FirstOrDefault(u => u.Id == Convert.ToUInt32(property.Value));
		var index = users.IndexOf(user);
		ddl.SelectedIndex = index;
	}

	private void FillDDLOptimal()
	{
		if (MyCn.State != ConnectionState.Open)
			MyCn.Open();
		MyCmd.Connection = MyCn;

		dtDDLOptionalParams = new DataTable("DDLOptionalParams");
		dtDDLOptionalParams.Columns.Add("opID", typeof(int));
		dtDDLOptionalParams.Columns.Add("opName", typeof(string));
		dtDDLOptionalParams.Columns.Add("opRemove", typeof(byte));

		MyDA.SelectCommand = MyCmd;
		MyCmd.Parameters.Clear();
		MyCmd.Parameters.AddWithValue("?ReportCode", Request["rp"]);
		MyCmd.CommandText = @"
Select
	rtp.ID as opID,
	rtp.DisplayName as opName
FROM
	reports.report_type_properties rtp,
	reports.reports r
WHERE
	rtp.Optional=1
and r.ReportCode = ?ReportCode
and rtp.ReportTypeCode = r.ReportTypeCode";
		MyDA.Fill(dtDDLOptionalParams);

		MyCn.Close();

		foreach (DataRow dr in dtDDLOptionalParams.Rows) {
			DataRow[] dtr = DS.Tables[dtOptionalParams.TableName].Select(OPrtpID.ColumnName + "=" + dr["opID"].ToString());
			if (dtr.Length > 0)
				dr["opRemove"] = 1;
			else
				dr["opRemove"] = 0;
		}

		DataRow[] dtrRemove = dtDDLOptionalParams.Select("opRemove = 1");
		for (int i = 0; i < dtrRemove.Length; i++) {
			if (dtDDLOptionalParams.Select("opID=" + dtrRemove[i]["opID"]).Length > 0) {
				if (dtrRemove[i]["opRemove"].ToString() == "1")
					dtDDLOptionalParams.Rows.Remove(dtrRemove[i]);
			}
		}
		dtDDLOptionalParams.AcceptChanges();
	}

	protected void dgvOptional_RowDataBound(object sender, GridViewRowEventArgs e)
	{
		if (e.Row.RowType != DataControlRowType.DataRow)
			return;

		if (((Label)e.Row.Cells[0].FindControl("lblName")).Text == String.Empty) {
			var ddlName = ((DropDownList)e.Row.Cells[0].FindControl("ddlName"));
			ddlName.Visible = true;
			(e.Row.Cells[0].FindControl("lblName")).Visible = false;
			FillDDLOptimal();
			FilterDDLOptional();
			ddlName.DataSource = dtDDLOptionalParams;
			ddlName.DataTextField = "opName";
			ddlName.DataValueField = "opID";
			ddlName.DataBind();
		}
		else {
			ShowEditor(e.Row);
		}
	}

	protected void dgvOptional_RowDeleting(object sender, GridViewDeleteEventArgs e)
	{
		CopyChangesToTable(dgvNonOptional, dtNonOptionalParams, PPropertyValue.ColumnName);
		CopyChangesToTable(dgvOptional, dtOptionalParams, OPPropertyValue.ColumnName);
		DS.Tables[dtOptionalParams.TableName].DefaultView[e.RowIndex].Delete();
		dgvOptional.DataSource = DS;
		dgvOptional.DataBind();
		SetOptionalVisibility();
	}

	protected void btnBack_Click(object sender, EventArgs e)
	{
		Response.Redirect("TemporaryReport.aspx?TemporaryId=" + Request["TemporaryId"]);
	}

	protected void btnNext_Click(object sender, EventArgs e)
	{
		Response.Redirect("TemporaryReportSchedule.aspx?TemporaryId=" + Request["TemporaryId"]);
	}
}