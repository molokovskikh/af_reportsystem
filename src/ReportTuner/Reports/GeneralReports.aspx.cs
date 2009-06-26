using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using MySql.Data;
using MySql.Data.MySqlClient;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;
using ReportTuner.Models;

public partial class Reports_GeneralReports : System.Web.UI.Page
{
	public enum GeneralReportFields : int
	{
		Code = 0,
		Payer = 1,
		Delivery = 5,
        Reports = 6,
		Schedule = 7
	}

	private string SetFilterCaption = "Фильтровать";
	private string ClearFilterCaption = "Сбросить";

	private MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
	private MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataSet DS;
    private DataTable dtGeneralReports;
    private DataColumn GeneralReportCode;
	private DataColumn FirmCode;
	private DataColumn Comment;
    private DataColumn Allow;
    private DataTable dtPayers;
    private DataColumn PayerShortName;
    private DataColumn PPayerID;
    private DataColumn GRPayerShortName;
	private DataColumn GRPayerID;



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
    gr.GeneralReportCode,
    gr.PayerID,
    p.ShortName as PayerShortName,
    min(cd.FirmCode) as FirmCode,
    gr.Allow,
    gr.Comment,
    gr.EMailSubject,
    gr.ReportFileName,
    gr.ReportArchName
FROM
    reports.general_reports gr,
    billing.payers p,
    usersettings.clientsdata cd
WHERE
    p.PayerId = gr.PayerId
and cd.BillingCode = gr.PayerId
and gr.GeneralReportCode <> ?TemplateReportId
and gr.Temporary = 0
group by gr.GeneralReportCode
Order by gr.GeneralReportCode
";
		MyCmd.Parameters.AddWithValue("?TemplateReportId", ConfigurationManager.AppSettings["TemplateReportId"]);
        MyDA.Fill(DS, dtGeneralReports.TableName);
        MyCn.Close();

        Session.Add(DSReports, DS);

		if (String.IsNullOrEmpty(SortField))
		{
			SortField = "GeneralReportCode";
		}

		ClearFilter();

		DS.Tables[dtGeneralReports.TableName].DefaultView.Sort = SortField + " " + getSortDirection(); 
        dgvReports.DataSource = DS.Tables[dtGeneralReports.TableName].DefaultView;
        dgvReports.DataBind();
    }

    private void InitializeComponent()
    {
		this.DS = new System.Data.DataSet();
		this.dtGeneralReports = new System.Data.DataTable();
		this.GeneralReportCode = new System.Data.DataColumn();
		this.FirmCode = new System.Data.DataColumn();
		this.Comment = new System.Data.DataColumn();
		this.Allow = new System.Data.DataColumn();
		this.GRPayerShortName = new System.Data.DataColumn();
		this.GRPayerID = new System.Data.DataColumn();
		this.dtPayers = new System.Data.DataTable();
		this.PayerShortName = new System.Data.DataColumn();
		this.PPayerID = new System.Data.DataColumn();
		((System.ComponentModel.ISupportInitialize)(this.DS)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(this.dtPayers)).BeginInit();
		// 
		// DS
		// 
		this.DS.DataSetName = "NewDataSet";
		this.DS.Tables.AddRange(new System.Data.DataTable[] {
            this.dtGeneralReports,
            this.dtPayers});
		// 
		// dtGeneralReports
		// 
		this.dtGeneralReports.Columns.AddRange(new System.Data.DataColumn[] {
            this.GeneralReportCode,
            this.FirmCode,
            this.Comment,
            this.Allow,
            this.GRPayerShortName,
            this.GRPayerID});
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
            this.PPayerID});
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
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtPayers)).EndInit();

    }

    protected void dgvReports_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Add")
        {
            CopyChangesToTable();

			ClearFilter();

            DataRow dr = DS.Tables[dtGeneralReports.TableName].NewRow();
            dr[Allow.ColumnName] = 0;
            DS.Tables[dtGeneralReports.TableName].Rows.Add(dr);

            dgvReports.DataSource = DS.Tables[dtGeneralReports.TableName].DefaultView;

            dgvReports.DataBind();

            btnApply.Visible = true;
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
        foreach (GridViewRow dr in dgvReports.Rows)
        {
            if (((DropDownList)dr.FindControl("ddlNames")).SelectedValue != String.Empty)
            {
                if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRPayerID.ColumnName].ToString() != ((DropDownList)dr.FindControl("ddlNames")).SelectedValue)
					DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRPayerID.ColumnName] = ((DropDownList)dr.FindControl("ddlNames")).SelectedValue;
            }

            if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][Allow.ColumnName].ToString() != Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked).ToString())
                DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][Allow.ColumnName] = Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked);

			if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][Comment.ColumnName].ToString() != ((TextBox)dr.FindControl("tbComment")).Text)
				DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][Comment.ColumnName] = ((TextBox)dr.FindControl("tbComment")).Text;

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
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            if (((Label)e.Row.FindControl("lblFirmName")).Text != "")
            {
                ((TextBox)e.Row.FindControl("tbSearch")).Visible = false;
                ((Button)e.Row.FindControl("btnSearch")).Visible = false;
                ((DropDownList)e.Row.FindControl("ddlNames")).Visible = false;
                ((Label)e.Row.FindControl("lblFirmName")).Visible = true;
				e.Row.Cells[(int)GeneralReportFields.Delivery].Enabled = true;
            }
            else
            {
                ((TextBox)e.Row.FindControl("tbSearch")).Visible = true;
				((TextBox)e.Row.FindControl("tbSearch")).Focus();
                ((Button)e.Row.FindControl("btnSearch")).Visible = true;


                DropDownList ddlReports = (DropDownList)e.Row.FindControl("ddlNames");
                ddlReports.Visible = true;
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

		List<ulong> _deletedReports = new List<ulong>();
		List<ulong> _updatedReports = new List<ulong>();

        MySqlTransaction trans;
        MyCn.Open();
        trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE 
    reports.general_reports 
SET 
    Allow = ?Allow,
    Comment = ?Comment
WHERE GeneralReportCode = ?GeneralReportCode", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("Allow", MySqlDbType.Byte));
            UpdCmd.Parameters["Allow"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["Allow"].SourceColumn = Allow.ColumnName;
            UpdCmd.Parameters["Allow"].SourceVersion = DataRowVersion.Current;
			UpdCmd.Parameters.Add(new MySqlParameter("Comment", MySqlDbType.VarString));
			UpdCmd.Parameters["Comment"].Direction = ParameterDirection.Input;
			UpdCmd.Parameters["Comment"].SourceColumn = Comment.ColumnName;
			UpdCmd.Parameters["Comment"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("GeneralReportCode", MySqlDbType.Int64));
            UpdCmd.Parameters["GeneralReportCode"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GeneralReportCode"].SourceColumn = GeneralReportCode.ColumnName;
            UpdCmd.Parameters["GeneralReportCode"].SourceVersion = DataRowVersion.Current;

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
SET 
    PayerId = ?PayerId,
    Allow = ?Allow,
    Comment = ?Comment;
select last_insert_id() as GRLastInsertID;
", MyCn, trans);

            InsCmd.Parameters.Clear();
            InsCmd.Parameters.Add(new MySqlParameter("Allow", MySqlDbType.Byte));
            InsCmd.Parameters["Allow"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["Allow"].SourceColumn = Allow.ColumnName;
            InsCmd.Parameters["Allow"].SourceVersion = DataRowVersion.Current;
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
            if (strUser.StartsWith("ANALIT\\"))
            {
                strUser = strUser.Substring(7);
            }
            MySqlHelper.ExecuteNonQuery(trans.Connection, "set @INHost = ?Host; set @INUser = ?User", new MySqlParameter[] { new MySqlParameter("Host", strHost), new MySqlParameter("User", strUser) });

			DataTable dtDeleted = DS.Tables[dtGeneralReports.TableName].GetChanges(DataRowState.Deleted);
			if (dtDeleted != null)
			{
				foreach (DataRow drDeleted in dtDeleted.Rows)
					_deletedReports.Add(Convert.ToUInt64(drDeleted[GeneralReportCode.ColumnName, DataRowVersion.Original]));
				MyDA.Update(dtDeleted);
			}

			DataTable dtInserted = DS.Tables[dtGeneralReports.TableName].GetChanges(DataRowState.Added);
			if (dtInserted != null)
			{
				MyDA.Update(dtInserted);
				foreach (DataRow drInsert in dtInserted.Rows)
					_updatedReports.Add(Convert.ToUInt64(drInsert["GRLastInsertID"]));
			}

			DataTable dtUpdated = DS.Tables[dtGeneralReports.TableName].GetChanges(DataRowState.Modified);
			if (dtUpdated != null)
			{
				foreach (DataRow drUpdate in dtUpdated.Rows)
					if (drUpdate["Comment", DataRowVersion.Original] != drUpdate["Comment", DataRowVersion.Current])
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
        finally
        {
            MyCn.Close();
        }

		//Удаляем задания для отчетов и обновляем комментарии в заданиях (или создаем эти задания)
		if ((_deletedReports.Count > 0) || (_updatedReports.Count > 0))
		{
			using (TaskService taskService = ScheduleHelper.GetService())
			using (TaskFolder reportsFolder = ScheduleHelper.GetReportsFolder(taskService))
			{
				foreach (ulong _updatedReportId in _updatedReports)
				{
					GeneralReport _report = GeneralReport.Find(_updatedReportId);
					ScheduleHelper.GetTask(taskService, reportsFolder, _updatedReportId, _report.Comment);
				}

				foreach (ulong _deletedReportId in _deletedReports)
					ScheduleHelper.DeleteTask(reportsFolder, _deletedReportId);
			}
		}

		PostData();

		if (dgvReports.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

	public string SortField
	{
		get
		{
			object o = ViewState["SortField"];
			if (o == null)
			{
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
			if (o == null)
			{
				return true;
			}
			return (bool)o;
		}
		set
		{
			ViewState["SortAscending"] = value;
		}
	}

	private string getSortDirection()
	{
		return SortAscending ? "asc" : "desc";
	}

	protected void dgvReports_Sorting(object sender, GridViewSortEventArgs e)
	{
		CopyChangesToTable();

		if (e.SortExpression != SortField)
		{
			SortField = e.SortExpression;
			SortAscending = true;
		}
		else
		{
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
		if (e.Row.RowType == DataControlRowType.Header)
		{
			// Call the GetSortColumnIndex helper method to determine
			// the index of the column being sorted.
			int sortColumnIndex = GetSortColumnIndex();

			if (sortColumnIndex != -1)
			{
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
		foreach (DataControlField field in dgvReports.Columns)
		{
			if (field.SortExpression == SortField)
			{
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
		if (SortAscending)
		{
			sortImage.ImageUrl = "~/Images/Ascending.gif";
			sortImage.AlternateText = "По возрастанию";
		}
		else
		{
			sortImage.ImageUrl = "~/Images/Descending.gif";
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

	private void SetFilter()
	{
		List<string> filter = new List<string>();
		int testInt;
		if (int.TryParse(tbFilter.Text, out testInt))
		{
			filter.Add(String.Format("(GeneralReportCode = {0})", testInt));
			filter.Add(String.Format("(PayerID = {0})", testInt));
		}

		filter.Add(String.Format("(PayerShortName like '%{0}%')", tbFilter.Text));
		filter.Add(String.Format("(Comment like '%{0}%')", tbFilter.Text));
		filter.Add(String.Format("(EMailSubject like '%{0}%')", tbFilter.Text));
		filter.Add(String.Format("(ReportFileName like '%{0}%')", tbFilter.Text));
		filter.Add(String.Format("(ReportArchName like '%{0}%')", tbFilter.Text));

		DS.Tables[dtGeneralReports.TableName].DefaultView.RowFilter = String.Join(" or ", filter.ToArray());
	}

	protected void btnFilter_Click(object sender, EventArgs e)
	{
		CopyChangesToTable();

		if (((sender == btnFilter) && (btnFilter.Text == ClearFilterCaption)))
		{
			ClearFilter();
		}
		else
			if (String.IsNullOrEmpty(tbFilter.Text))
				ClearFilter();
			else
				SetFilter();

		dgvReports.DataSource = DS.Tables[dtGeneralReports.TableName].DefaultView;
		dgvReports.DataBind();
	}

}
