using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Common.MySql;
using Common.Schedule;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Inforoom.ReportSystem;
using Microsoft.Win32.TaskScheduler;
using MySql.Data.MySqlClient;
using ReportTuner.Helpers;
using GeneralReport = ReportTuner.Models.GeneralReport;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;
using Page = System.Web.UI.Page;

public partial class Reports_GeneralReports : BasePage
{
	public bool UnderTest;
	public List<MailMessage> Messages = new List<MailMessage>();

	public static void Redirect(Page CurrentPage)
	{
		CurrentPage.Session["redirected"] = true;
		CurrentPage.Response.Redirect("GeneralReports.aspx");
	}

	public enum GeneralReportFields : int
	{
		Code = 0,
		Payer = 1,
		Delivery = 6,
		Reports = 7,
		Schedule = 8
	}

	private string SetFilterCaption = "Фильтровать";

	private MySqlConnection MyCn = new MySqlConnection(ConnectionHelper.GetConnectionString());
	private MySqlCommand MyCmd = new MySqlCommand();
	private MySqlDataAdapter MyDA = new MySqlDataAdapter();
	private DataSet DS;
	private DataTable dtGeneralReports;
	private DataColumn GeneralReportCode;
	private DataColumn FirmCode;
	private DataColumn Comment;
	private DataColumn Allow;
	private DataColumn Public;
	private DataTable dtPayers;
	private DataColumn PayerShortName;
	private DataColumn PPayerID;
	private DataColumn GRPayerShortName;
	private DataColumn GRPayerID;
	private DataColumn dataColumn1;

	private const string DSReports = "Inforoom.Reports.GeneralReports.DSReports";

	protected void Page_Init(object sender, System.EventArgs e)
	{
		InitializeComponent();
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		if (!IsPostBack) {
			PostData();
		}
		else {
			DS = ((DataSet)Session[DSReports]);
			if (DS == null) // вероятно, сессия завершилась и все ее данные утеряны
				Redirect(this);
		}

		btnApply.Visible = dgvReports.Rows.Count > 0;

		if (Session["redirected"] != null && Convert.ToBoolean(Session["redirected"])) {
			lblMessage.Text = "Вследствие закрытия сессии, Вы были переведены на главную страницу. Повторите запрос.";
			Session["redirected"] = null;
		}
		else {
			lblMessage.Text = "";
		}
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
	gr.GeneralReportCode,
	gr.PayerID,
	p.ShortName as PayerShortName,
	gr.FirmCode,
	gr.Allow,
	gr.Public,
	gr.Comment,
	gr.EMailSubject,
	gr.ReportFileName,
	gr.ReportArchName,
	EXISTS(select 1 from Customers.Clients where Id = gr.FirmCode) IsNewClient
FROM
	reports.general_reports gr,
	billing.payers p
WHERE
	p.PayerId = gr.PayerId
and gr.GeneralReportCode <> ?TemplateReportId
and gr.Temporary = 0
Order by gr.GeneralReportCode
";
		MyCmd.Parameters.AddWithValue("?TemplateReportId", ConfigurationManager.AppSettings["TemplateReportId"]);
		MyDA.Fill(DS, dtGeneralReports.TableName);
		MyCn.Close();

		Session.Add(DSReports, DS);

		if (String.IsNullOrEmpty(SortField)) {
			SortField = "GeneralReportCode";
		}

		ClearFilter();

		DS.Tables[dtGeneralReports.TableName].DefaultView.Sort = SortField + " " + getSortDirection();
		dgvReports.DataSource = DS.Tables[dtGeneralReports.TableName].DefaultView;
		dgvReports.DataBind();
	}
	#region Component Designer generated code
	private void InitializeComponent()
	{
		this.DS = new System.Data.DataSet();
		this.dtGeneralReports = new System.Data.DataTable();
		this.GeneralReportCode = new System.Data.DataColumn();
		this.FirmCode = new System.Data.DataColumn();
		this.Comment = new System.Data.DataColumn();
		this.Allow = new System.Data.DataColumn();
		this.Public = new System.Data.DataColumn();
		this.GRPayerShortName = new System.Data.DataColumn();
		this.GRPayerID = new System.Data.DataColumn();
		this.dtPayers = new System.Data.DataTable();
		this.PayerShortName = new System.Data.DataColumn();
		this.PPayerID = new System.Data.DataColumn();
		this.dataColumn1 = new System.Data.DataColumn();
		((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtPayers)).BeginInit();
		//
		// DS
		//
		this.DS.DataSetName = "NewDataSet";
		this.DS.Tables.AddRange(new System.Data.DataTable[] {
			this.dtGeneralReports,
			this.dtPayers
		});
		//
		// dtGeneralReports
		//
		this.dtGeneralReports.Columns.AddRange(new System.Data.DataColumn[] {
			this.GeneralReportCode,
			this.FirmCode,
			this.Comment,
			this.Allow,
			this.Public,
			this.GRPayerShortName,
			this.GRPayerID,
			this.dataColumn1
		});
		this.dtGeneralReports.TableName = "dtGeneralReports";
		//
		// GeneralReportCode
		//
		this.GeneralReportCode.ColumnName = "GeneralReportCode";
		this.GeneralReportCode.DataType = typeof(long);
		//
		// FirmCode
		//
		this.FirmCode.ColumnName = "FirmCode";
		this.FirmCode.DataType = typeof(long);
		//
		// Comment
		//
		this.Comment.ColumnName = "Comment";
		//
		// Allow
		//
		this.Allow.ColumnName = "Allow";
		this.Allow.DataType = typeof(byte);
		//
		// Public
		//
		this.Public.ColumnName = "Public";
		this.Public.DataType = typeof(byte);
		//
		// GRPayerShortName
		//
		this.GRPayerShortName.ColumnName = "PayerShortName";
		//
		// GRPayerID
		//
		this.GRPayerID.ColumnName = "PayerID";
		this.GRPayerID.DataType = typeof(long);
		//
		// dtPayers
		//
		this.dtPayers.Columns.AddRange(new System.Data.DataColumn[] {
			this.PayerShortName,
			this.PPayerID
		});
		this.dtPayers.TableName = "dtPayers";
		//
		// PayerShortName
		//
		this.PayerShortName.ColumnName = "PayerShortName";
		//
		// PPayerID
		//
		this.PPayerID.ColumnName = "PayerID";
		this.PPayerID.DataType = typeof(long);
		//
		// dataColumn1
		//
		this.dataColumn1.ColumnName = "IsNewClient";
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtPayers)).EndInit();
	}
	#endregion
	protected void dgvReports_RowCommand(object sender, GridViewCommandEventArgs e)
	{
		if (e.CommandName == "Add") {
			//Если нет добавленных записей, то позволяем добавить запись
			if (DS.Tables[dtGeneralReports.TableName].GetChanges(DataRowState.Added) == null) {
				CopyChangesToTable();

				ClearFilter();

				DataRow dr = DS.Tables[dtGeneralReports.TableName].NewRow();
				dr[Allow.ColumnName] = 0;
				dr[Public.ColumnName] = 0;
				DS.Tables[dtGeneralReports.TableName].Rows.Add(dr);

				dgvReports.DataSource = DS.Tables[dtGeneralReports.TableName].DefaultView;

				dgvReports.DataBind();

				btnApply.Visible = true;
			}
			else {
				//Ищем добавленную запись и позиционируемся на нее
				foreach (GridViewRow row in dgvReports.Rows)
					if (String.IsNullOrEmpty(row.Cells[(int)GeneralReportFields.Code].Text)) {
						dgvReports.SelectedIndex = row.RowIndex;
						break;
					}
			}
		}
		else if (e.CommandName == "editPayer") {
			DataControlFieldCell cell = (DataControlFieldCell)((Control)e.CommandSource).Parent;
			((TextBox)cell.FindControl("tbSearch")).Visible = true;
			((TextBox)cell.FindControl("tbSearch")).Focus();
			((Button)cell.FindControl("btApplyCopy")).Visible = true;
			((LinkButton)cell.FindControl("linkEdit")).Visible = false;

			FillDDL(((Label)cell.FindControl("lblFirmName")).Text);
		}
	}

	protected void dgvReports_RowDeleting(object sender, GridViewDeleteEventArgs e)
	{
		CopyChangesToTable();
		DS.Tables[dtGeneralReports.TableName].DefaultView[e.RowIndex].Delete();
		dgvReports.DataSource = DS.Tables[dtGeneralReports.TableName].DefaultView;
		dgvReports.DataBind();
	}

	private void FillDDL(string Name)
	{
		if (MyCn.State != ConnectionState.Open)
			MyCn.Open();
		MyCmd.Connection = MyCn;
		MyDA.SelectCommand = MyCmd;
		MyCmd.Parameters.Clear();
		MyCmd.Parameters.AddWithValue("Name", "%" + Name + "%");
		DS.Tables[dtPayers.TableName].Clear();
		MyCmd.CommandText = @"
SELECT
	p.PayerID,
	convert(concat(p.PayerID, ' - ', p.ShortName) using cp1251) as PayerShortName
FROM
	 billing.payers p
 WHERE
  p.ShortName like ?Name
Order by p.ShortName
";
		MyDA.Fill(DS, DS.Tables[dtPayers.TableName].TableName);
		MyCn.Close();
		Session.Add(DSReports, DS);
	}

	private void CopyChangesToTable()
	{
		foreach (GridViewRow dr in dgvReports.Rows) {
			DataRow changedRow = null;


			if (Convert.IsDBNull(dgvReports.DataKeys[dr.RowIndex].Value)) {
				//добавленная запись
				DataRow[] drs = DS.Tables[dtGeneralReports.TableName].Select("GeneralReportCode is null");
				if (drs.Length == 1) {
					changedRow = drs[0];
					/*if (!String.IsNullOrEmpty(((DropDownList)dr.FindControl("ddlNames")).SelectedValue))
						changedRow[GRPayerID.ColumnName] = Convert.ToInt64(((DropDownList)dr.FindControl("ddlNames")).SelectedValue);*/
				}
			}
			else {
				//измененная запись
				DataRow[] drs = DS.Tables[dtGeneralReports.TableName].Select("GeneralReportCode = " + dgvReports.DataKeys[dr.RowIndex].Value);
				if (drs.Length == 1)
					changedRow = drs[0];
			}

			if (changedRow != null) {
				if (!changedRow[Allow.ColumnName].Equals(Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked)))
					changedRow[Allow.ColumnName] = Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked);

				if (!changedRow[Public.ColumnName].Equals(Convert.ToByte(((CheckBox)dr.FindControl("chbPublic")).Checked)))
					changedRow[Public.ColumnName] = Convert.ToByte(((CheckBox)dr.FindControl("chbPublic")).Checked);

				if (!changedRow[Comment.ColumnName].Equals(((TextBox)dr.FindControl("tbComment")).Text))
					changedRow[Comment.ColumnName] = ((TextBox)dr.FindControl("tbComment")).Text;

				var names = (HiddenField)dr.FindControl("ddlNames");
				if (!String.IsNullOrEmpty(names.Value))
					changedRow[GRPayerID.ColumnName] = Convert.ToInt64(names.Value);
			}
		}
	}

	protected void btnSearch_Click(object sender, EventArgs e)
	{
		FillDDL(((TextBox)(((Button)sender).Parent).FindControl("tbSearch")).Text);
		DropDownList ddlNames = (DropDownList)(((Button)sender).Parent).FindControl("ddlNames");
		ddlNames.DataSource = DS.Tables[dtPayers.TableName];
		ddlNames.DataTextField = "PayerShortName";
		ddlNames.DataValueField = "PayerID";
		ddlNames.DataBind();
		ddlNames.Focus();
	}

	protected void dgvReports_RowDataBound(object sender, GridViewRowEventArgs e)
	{
		if (e.Row.RowType == DataControlRowType.DataRow) {
			//"Рассылки"
			e.Row.Cells[(int)GeneralReportFields.Delivery].ToolTip = "Рассылки";
			//"Отчеты"
			e.Row.Cells[(int)GeneralReportFields.Reports].ToolTip = "Отчеты";
			//"Расписание"
			e.Row.Cells[(int)GeneralReportFields.Schedule].ToolTip = "Расписание";

			var btnDelete = e.Row.FindControl("btnDelete") as Button;
			if (btnDelete != null)
			{
				var code = e.Row.Cells[0].Text;
				var comment = ((TextBox)e.Row.FindControl("tbComment")).Text;
				btnDelete.OnClientClick = $"return confirm('Вы действительно хотите удалить отчет №{code} {comment}?');";
			}

			if (((Label)e.Row.FindControl("lblFirmName")).Text != "") {
				((TextBox)e.Row.FindControl("tbSearch")).Visible = false;
				((Button)e.Row.FindControl("btApplyCopy")).Visible = false;
				((Label)e.Row.FindControl("lblFirmName")).Visible = true;
				((LinkButton)e.Row.FindControl("linkEdit")).Visible = true;
				e.Row.Cells[(int)GeneralReportFields.Delivery].Enabled = true;
			}
			else {
				((TextBox)e.Row.FindControl("tbSearch")).Visible = true;
				((TextBox)e.Row.FindControl("tbSearch")).Focus();
				((Button)e.Row.FindControl("btApplyCopy")).Visible = true;
				((LinkButton)e.Row.FindControl("linkEdit")).Visible = false;


				//Делаем недоступными столбцы
				//"Рассылки"
				e.Row.Cells[(int)GeneralReportFields.Delivery].Enabled = false;
				//"Отчеты"
				e.Row.Cells[(int)GeneralReportFields.Reports].Enabled = false;
				//"Расписание"
				e.Row.Cells[(int)GeneralReportFields.Schedule].Enabled = false;
				((Label)e.Row.FindControl("lblFirmName")).Visible = false;
			}
		}
	}

	protected void btnApply_Click(object sender, EventArgs e)
	{
		CopyChangesToTable();

		var _deletedReports = new List<ulong>();
		var _updatedReports = new List<ulong>();
		DataTable dtInserted;

		MyCn.Open();
		var trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
		try {
			var UpdCmd = new MySqlCommand(@"
UPDATE
	reports.general_reports
SET
	Allow = ?Allow,
	Public = ?Public,
	Comment = ?Comment,
	FirmCode = if(PayerID = ?payerID, FirmCode,
			(select min(Id)
			   from
			   (select s.Id
				from Customers.Suppliers s
				where s.Payer = ?payerID) tbl)),
	PayerID = ?payerID
WHERE GeneralReportCode = ?GeneralReportCode", MyCn, trans);

			UpdCmd.Parameters.Clear();
			UpdCmd.Parameters.Add(new MySqlParameter("Allow", MySqlDbType.Byte));
			UpdCmd.Parameters["Allow"].Direction = ParameterDirection.Input;
			UpdCmd.Parameters["Allow"].SourceColumn = Allow.ColumnName;
			UpdCmd.Parameters["Allow"].SourceVersion = DataRowVersion.Current;
			UpdCmd.Parameters.Add(new MySqlParameter("Public", MySqlDbType.Byte));
			UpdCmd.Parameters["Public"].Direction = ParameterDirection.Input;
			UpdCmd.Parameters["Public"].SourceColumn = Public.ColumnName;
			UpdCmd.Parameters["Public"].SourceVersion = DataRowVersion.Current;
			UpdCmd.Parameters.Add(new MySqlParameter("Comment", MySqlDbType.VarString));
			UpdCmd.Parameters["Comment"].Direction = ParameterDirection.Input;
			UpdCmd.Parameters["Comment"].SourceColumn = Comment.ColumnName;
			UpdCmd.Parameters["Comment"].SourceVersion = DataRowVersion.Current;
			UpdCmd.Parameters.Add(new MySqlParameter("GeneralReportCode", MySqlDbType.Int64));
			UpdCmd.Parameters["GeneralReportCode"].Direction = ParameterDirection.Input;
			UpdCmd.Parameters["GeneralReportCode"].SourceColumn = GeneralReportCode.ColumnName;
			UpdCmd.Parameters["GeneralReportCode"].SourceVersion = DataRowVersion.Current;
			UpdCmd.Parameters.Add("?payerID", MySqlDbType.Int64).SourceColumn = GRPayerID.ColumnName;

			MySqlCommand DelCmd = new MySqlCommand(@"
DELETE from reports.general_reports
WHERE GeneralReportCode = ?GRDelCode", MyCn, trans);

			DelCmd.Parameters.Clear();
			DelCmd.Parameters.Add(new MySqlParameter("GRDelCode", MySqlDbType.Int64));
			DelCmd.Parameters["GRDelCode"].Direction = ParameterDirection.Input;
			DelCmd.Parameters["GRDelCode"].SourceColumn = GeneralReportCode.ColumnName;
			DelCmd.Parameters["GRDelCode"].SourceVersion = DataRowVersion.Original;

			MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO
	reports.general_reports
(PayerId, Allow, Public, Comment, FirmCode)
select
  ?PayerId,
  ?Allow,
  ?Public,
  ?Comment,
  min(Id)
from
(
select s.Id
from Customers.Suppliers s
where s.Payer = ?payerID
) tbl;
select last_insert_id() as GRLastInsertID;
", MyCn, trans);

			InsCmd.Parameters.Clear();
			InsCmd.Parameters.Add(new MySqlParameter("Allow", MySqlDbType.Byte));
			InsCmd.Parameters["Allow"].Direction = ParameterDirection.Input;
			InsCmd.Parameters["Allow"].SourceColumn = Allow.ColumnName;
			InsCmd.Parameters["Allow"].SourceVersion = DataRowVersion.Current;
			InsCmd.Parameters.Add(new MySqlParameter("Public", MySqlDbType.Byte));
			InsCmd.Parameters["Public"].Direction = ParameterDirection.Input;
			InsCmd.Parameters["Public"].SourceColumn = Public.ColumnName;
			InsCmd.Parameters["Public"].SourceVersion = DataRowVersion.Current;
			InsCmd.Parameters.Add(new MySqlParameter("PayerId", MySqlDbType.Int64));
			InsCmd.Parameters["PayerId"].Direction = ParameterDirection.Input;
			InsCmd.Parameters["PayerId"].SourceColumn = GRPayerID.ColumnName;
			InsCmd.Parameters["PayerId"].SourceVersion = DataRowVersion.Current;
			InsCmd.Parameters.Add(new MySqlParameter("Comment", MySqlDbType.VarString));
			InsCmd.Parameters["Comment"].Direction = ParameterDirection.Input;
			InsCmd.Parameters["Comment"].SourceColumn = Comment.ColumnName;
			InsCmd.Parameters["Comment"].SourceVersion = DataRowVersion.Current;

			MyDA.UpdateCommand = UpdCmd;
			MyDA.DeleteCommand = DelCmd;
			MyDA.InsertCommand = InsCmd;

			string strHost = HttpContext.Current.Request.UserHostAddress;
			string strUser = HttpContext.Current.User.Identity.Name;
			if (strUser.StartsWith("ANALIT\\")) {
				strUser = strUser.Substring(7);
			}
			MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

			DataTable dtDeleted = DS.Tables[dtGeneralReports.TableName].GetChanges(DataRowState.Deleted);
			if (dtDeleted != null) {
				foreach (DataRow drDeleted in dtDeleted.Rows)
				{
					var code = Convert.ToUInt64(drDeleted[GeneralReportCode.ColumnName, DataRowVersion.Original]);
					var comment = drDeleted[Comment.ColumnName, DataRowVersion.Original].ToString();
					SendDeleteAlert(code, comment, strUser, strHost);
					_deletedReports.Add(code);
				}
				MyDA.Update(dtDeleted);
			}

			dtInserted = DS.Tables[dtGeneralReports.TableName].GetChanges(DataRowState.Added);
			if (dtInserted != null)
				foreach (DataRow drInsert in dtInserted.Rows)
					if (!Convert.IsDBNull(drInsert[GRPayerID.ColumnName]) && (drInsert[GRPayerID.ColumnName] is long)) {
						MyDA.Update(new DataRow[] { drInsert });
						_updatedReports.Add(Convert.ToUInt64(drInsert["GRLastInsertID"]));
					}

			DataTable dtUpdated = DS.Tables[dtGeneralReports.TableName].GetChanges(DataRowState.Modified);
			if (dtUpdated != null) {
				foreach (DataRow drUpdate in dtUpdated.Rows)
					if (drUpdate["Comment", DataRowVersion.Original] != drUpdate["Comment", DataRowVersion.Current] ||
						drUpdate["Public", DataRowVersion.Original] != drUpdate["Public", DataRowVersion.Current] ||
						drUpdate["Allow", DataRowVersion.Original] != drUpdate["Allow", DataRowVersion.Current])
						_updatedReports.Add(Convert.ToUInt64(drUpdate["GeneralReportCode"]));
				MyDA.Update(dtUpdated);
			}

			trans.Commit();
		}
		catch
		{
			trans.Rollback();
			throw;
		}
		finally {
			MyCn.Close();
		}

		//Удаляем задания для отчетов и обновляем комментарии в заданиях (или создаем эти задания)
		// А также включаем/выключаем задание при изменении галки "Включен"
		UpdateTasksForGeneralReports(_deletedReports, _updatedReports);

		PostData();

		if (dgvReports.Rows.Count > 0)
			btnApply.Visible = true;
		else
			btnApply.Visible = false;

		if (dtInserted != null) {
			if (!Request.Url.OriginalString.Contains("#"))
				Response.Redirect(Request.Url.OriginalString + "#addedPage");
		}
	}

	public void SendDeleteAlert(ulong code, string comment, string user, string host)
	{
		var reportChangeAlertMailTo = ConfigurationManager.AppSettings["ReportChangeAlertMailTo"];
		var body = $"Пользователь {user} в {DateTime.Now} с IP {host} удалил отчет код {code} {comment}";
		var message = new MailMessage("service@analit.net", reportChangeAlertMailTo)
		{
			Subject = "Удаление отчета",
			Body = body,
			IsBodyHtml = false,
			BodyEncoding = Encoding.UTF8
		};
		SendMessage(message);
	}

	private void SendMessage(MailMessage message)
	{
		var client = new SmtpClient();
		if (UnderTest)
			Messages.Add(message);
		else
			client.Send(message);
	}

	public void UpdateTasksForGeneralReports(List<ulong> deletedReports,
		List<ulong> updatedReports)
	{
		if ((deletedReports.Count > 0)
			|| (updatedReports.Count > 0)) {
			using (var helper = new ScheduleHelper()) {
				foreach (var id in updatedReports) {
					var report = DbSession.Get<GeneralReport>(id);
					helper.GetTaskOrCreate(id, report.Comment);
					ScheduleHelper.SetTaskComment(id, report.Comment, "GR");
					ScheduleHelper.SetTaskEnableStatus(id, report.Allow, "GR");
				}
				foreach (var id in deletedReports)
					helper.DeleteReportTask(id);
			}
		}
	}

	public string SortField
	{
		get
		{
			object o = ViewState["SortField"];
			if (o == null) {
				return String.Empty;
			}
			return (string)o;
		}
		set
		{
			/*
			if (value == SortField)
			{
				//if ascending change to descending or vice versa.
				SortAscending = !SortAscending;
			}
		 */
			ViewState["SortField"] = value;
		}
	}

	// using ViewState for SortAscending property
	public bool SortAscending
	{
		get
		{
			object o = ViewState["SortAscending"];
			if (o == null) {
				return true;
			}
			return (bool)o;
		}
		set { ViewState["SortAscending"] = value; }
	}

	private string getSortDirection()
	{
		return SortAscending ? "asc" : "desc";
	}

	protected void dgvReports_Sorting(object sender, GridViewSortEventArgs e)
	{
		CopyChangesToTable();

		if (e.SortExpression != SortField) {
			SortField = e.SortExpression;
			SortAscending = true;
		}
		else {
			SortAscending = !SortAscending;
		}

		DS.Tables[dtGeneralReports.TableName].DefaultView.Sort = SortField + " " + getSortDirection();
		dgvReports.DataSource = DS.Tables[dtGeneralReports.TableName].DefaultView;
		dgvReports.DataBind();
	}

	protected void dgvReports_RowCreated(object sender, GridViewRowEventArgs e)
	{
		// Use the RowType property to determine whether the
		// row being created is the header row.
		if (e.Row.RowType == DataControlRowType.Header) {
			// Call the GetSortColumnIndex helper method to determine
			// the index of the column being sorted.
			int sortColumnIndex = GetSortColumnIndex();

			if (sortColumnIndex != -1) {
				// Call the AddSortImage helper method to add
				// a sort direction image to the appropriate
				// column header.
				AddSortImage(sortColumnIndex, e.Row);
			}
		}
	}

	// This is a helper method used to determine the index of the
	// column being sorted. If no column is being sorted, -1 is returned.
	private int GetSortColumnIndex()
	{
		// Iterate through the Columns collection to determine the index
		// of the column being sorted.
		foreach (DataControlField field in dgvReports.Columns) {
			if (field.SortExpression == SortField) {
				return dgvReports.Columns.IndexOf(field);
			}
		}

		return -1;
	}

	// This is a helper method used to add a sort direction
	// image to the header of the column being sorted.
	private void AddSortImage(int columnIndex, GridViewRow headerRow)
	{
		// Create the sorting image based on the sort direction.
		Image sortImage = new Image();
		if (SortAscending) {
			sortImage.ImageUrl = "~/Assets/Images/Ascending.gif";
			sortImage.AlternateText = "По возрастанию";
		}
		else {
			sortImage.ImageUrl = "~/Assets/Images/Descending.gif";
			sortImage.AlternateText = "По убыванию";
		}

		// Add the image to the appropriate header cell.
		headerRow.Cells[columnIndex].Controls.Add(sortImage);
	}

	private void ClearFilter()
	{
		tbFilter.Text = String.Empty;
		btnFilter.Text = SetFilterCaption;
		DS.Tables[dtGeneralReports.TableName].DefaultView.RowFilter = String.Empty;
	}

	protected IList<uint> GetReportCodesByEmails(IList<string> emails)
	{
		var codes = new List<uint>();
		var condition = new List<string>();
		for (int i = 0; i < emails.Count; i++) {
			condition.Add(string.Format("(c.ContactText like ?email_{0})", i));
		}
		try {
			if (MyCn.State != ConnectionState.Open)
				MyCn.Open();
			MyCmd.Connection = MyCn;
			MyDA.SelectCommand = MyCmd;
			MyCmd.CommandText = String.Format(@"
select distinct gr.GeneralReportCode from reports.general_reports gr
inner join contacts.contacts c on gr.ContactGroupId = c.ContactOwnerId and c.Type = 0
and ( {0} )
union
select distinct gr.GeneralReportCode from reports.general_reports gr
inner join contacts.contacts c on gr.PublicSubscriptionsId = c.ContactOwnerId and c.Type = 0
and ( {0} );
", String.Join(" or ", condition.ToArray()));
			var count = 0;
			foreach (var email in emails) {
				var param = string.Format("email_{0}", count);
				if (!MyCmd.Parameters.Contains(param))
					MyCmd.Parameters.AddWithValue(param, "%" + email.Trim() + "%");
				count++;
			}
			using (var reader = MyCmd.ExecuteReader()) {
				while (reader.Read()) {
					codes.Add(Convert.ToUInt32(reader[0]));
				}
			}
		}
		finally {
			MyCn.Close();
		}
		return codes;
	}

	private void SetFilter()
	{
		List<string> filter = new List<string>();
		IList<string> emails = tbFilter.Text.Split(',').ToList();

		int testInt;
		if (int.TryParse(tbFilter.Text, out testInt)) {
			filter.Add(String.Format("(GeneralReportCode = {0})", testInt));
			filter.Add(String.Format("(PayerID = {0})", testInt));
		}
		else {
			var codes = GetReportCodesByEmails(emails);
			filter.Add(codes.Count > 0
				? String.Format("(GeneralReportCode in ({0}))", codes.Implode(","))
				: "(GeneralReportCode is null)");
		}

		var filterText = SecurityElement.Escape(tbFilter.Text);

		filter.Add(String.Format("(PayerShortName like '%{0}%')", filterText));
		filter.Add(String.Format("(Comment like '%{0}%')", filterText));
		filter.Add(String.Format("(EMailSubject like '%{0}%')", filterText));
		filter.Add(String.Format("(ReportFileName like '%{0}%')", filterText));
		filter.Add(String.Format("(ReportArchName like '%{0}%')", filterText));
		DS.Tables[dtGeneralReports.TableName].DefaultView.RowFilter = String.Join(" or ", filter.ToArray());
	}

	protected void btnFilter_Click(object sender, EventArgs e)
	{
		CopyChangesToTable();

		if (String.IsNullOrEmpty(tbFilter.Text))
			ClearFilter();
		else
			SetFilter();

		dgvReports.DataSource = DS.Tables[dtGeneralReports.TableName].DefaultView;
		dgvReports.DataBind();
	}
}