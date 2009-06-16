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

public partial class Reports_GeneralReports : System.Web.UI.Page
{
	public enum GeneralReportFields : int
	{
		Code = 0,
		Payer = 1,
		Delivery = 3,
        Reports = 7,
		Schedule = 8
	}

	private MySqlConnection MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
	private MySqlCommand MyCmd = new MySqlCommand();
    private MySqlDataAdapter MyDA = new MySqlDataAdapter();
    private DataSet DS;
    private DataTable dtGeneralReports;
    private DataColumn GRCode;
	private DataColumn GRFirmCode;
	private DataColumn GRComment;
    private DataColumn GRAllow;
    private DataTable dtPayers;
    private DataColumn PayerShortName;
    private DataColumn PPayerID;
    private DataColumn GRPayerShortName;
	private DataColumn PayerID;

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
    gr.PayerID,
    p.ShortName as GRPayerShortName,
    min(cd.FirmCode) as GRFirmCode,
    Allow as GRAllow,
    gr.Comment as GRComment
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
		this.GRComment = new System.Data.DataColumn();
		this.GRAllow = new System.Data.DataColumn();
		this.GRPayerShortName = new System.Data.DataColumn();
		this.dtPayers = new System.Data.DataTable();
		this.PayerShortName = new System.Data.DataColumn();
		this.PPayerID = new System.Data.DataColumn();
		this.PayerID = new System.Data.DataColumn();
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
            this.GRCode,
            this.GRFirmCode,
            this.GRComment,
            this.GRAllow,
            this.GRPayerShortName,
            this.PayerID});
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
		// GRComment
		// 
		this.GRComment.ColumnName = "GRComment";
		// 
		// GRAllow
		// 
		this.GRAllow.ColumnName = "GRAllow";
		this.GRAllow.DataType = typeof(byte);
		// 
		// GRPayerShortName
		// 
		this.GRPayerShortName.ColumnName = "GRPayerShortName";
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
		// 
		// PayerID
		// 
		this.PayerID.ColumnName = "PayerID";
		this.PayerID.DataType = typeof(long);
		((System.ComponentModel.ISupportInitialize)(this.DS)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtGeneralReports)).EndInit();
		((System.ComponentModel.ISupportInitialize)(this.dtPayers)).EndInit();

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
                if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][PayerID.ColumnName].ToString() != ((DropDownList)dr.FindControl("ddlNames")).SelectedValue)
					DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][PayerID.ColumnName] = ((DropDownList)dr.FindControl("ddlNames")).SelectedValue;
            }

            if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRAllow.ColumnName].ToString() != Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked).ToString())
                DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRAllow.ColumnName] = Convert.ToByte(((CheckBox)dr.FindControl("chbAllow")).Checked);

			if (DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRComment.ColumnName].ToString() != ((TextBox)dr.FindControl("tbComment")).Text)
				DS.Tables[dtGeneralReports.TableName].DefaultView[dr.RowIndex][GRComment.ColumnName] = ((TextBox)dr.FindControl("tbComment")).Text;

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

        MySqlTransaction trans;
        MyCn.Open();
        trans = MyCn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            MySqlCommand UpdCmd = new MySqlCommand(@"
UPDATE 
    reports.general_reports 
SET 
    Allow = ?GRAllow,
    Comment = ?GRComment
WHERE GeneralReportCode = ?GRCode", MyCn, trans);

            UpdCmd.Parameters.Clear();
            UpdCmd.Parameters.Add(new MySqlParameter("GRAllow", MySqlDbType.Byte));
            UpdCmd.Parameters["GRAllow"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRAllow"].SourceColumn = GRAllow.ColumnName;
            UpdCmd.Parameters["GRAllow"].SourceVersion = DataRowVersion.Current;
			UpdCmd.Parameters.Add(new MySqlParameter("GRComment", MySqlDbType.VarString));
			UpdCmd.Parameters["GRComment"].Direction = ParameterDirection.Input;
			UpdCmd.Parameters["GRComment"].SourceColumn = GRComment.ColumnName;
			UpdCmd.Parameters["GRComment"].SourceVersion = DataRowVersion.Current;
            UpdCmd.Parameters.Add(new MySqlParameter("GRCode", MySqlDbType.Int64));
            UpdCmd.Parameters["GRCode"].Direction = ParameterDirection.Input;
            UpdCmd.Parameters["GRCode"].SourceColumn = GRCode.ColumnName;
            UpdCmd.Parameters["GRCode"].SourceVersion = DataRowVersion.Current;

            MySqlCommand DelCmd = new MySqlCommand(@"
DELETE from reports.general_reports 
WHERE GeneralReportCode = ?GRDelCode", MyCn, trans);

            DelCmd.Parameters.Clear();
            DelCmd.Parameters.Add(new MySqlParameter("GRDelCode", MySqlDbType.Int64));
            DelCmd.Parameters["GRDelCode"].Direction = ParameterDirection.Input;
            DelCmd.Parameters["GRDelCode"].SourceColumn = GRCode.ColumnName;
            DelCmd.Parameters["GRDelCode"].SourceVersion = DataRowVersion.Original;

            MySqlCommand InsCmd = new MySqlCommand(@"
INSERT INTO 
    reports.general_reports 
SET 
    PayerId = ?PayerId,
    Allow = ?GRAllow,
    Comment = ?GRComment
", MyCn, trans);

            InsCmd.Parameters.Clear();
            InsCmd.Parameters.Add(new MySqlParameter("GRAllow", MySqlDbType.Byte));
            InsCmd.Parameters["GRAllow"].Direction = ParameterDirection.Input;
            InsCmd.Parameters["GRAllow"].SourceColumn = GRAllow.ColumnName;
            InsCmd.Parameters["GRAllow"].SourceVersion = DataRowVersion.Current;
			InsCmd.Parameters.Add(new MySqlParameter("PayerId", MySqlDbType.Int64));
			InsCmd.Parameters["PayerId"].Direction = ParameterDirection.Input;
			InsCmd.Parameters["PayerId"].SourceColumn = PayerID.ColumnName;
			InsCmd.Parameters["PayerId"].SourceVersion = DataRowVersion.Current;
            InsCmd.Parameters.Add(new MySqlParameter("GRComment", MySqlDbType.VarString));
			InsCmd.Parameters["GRComment"].Direction = ParameterDirection.Input;
			InsCmd.Parameters["GRComment"].SourceColumn = GRComment.ColumnName;
			InsCmd.Parameters["GRComment"].SourceVersion = DataRowVersion.Current;

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
				foreach (DataRow drDeleted in dtDeleted.Rows)
					_deletedReports.Add(Convert.ToUInt64(drDeleted[GRCode.ColumnName, DataRowVersion.Original]));

            MyDA.Update(DS, DS.Tables[dtGeneralReports.TableName].TableName);

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

		//Удаляем задания для отчетов
		if (_deletedReports.Count > 0)
		{
			using (TaskService taskService = new TaskService(
				ConfigurationManager.AppSettings["asComp"],
				ConfigurationManager.AppSettings["asScheduleUserName"],
				ConfigurationManager.AppSettings["asScheduleDomainName"],
				ConfigurationManager.AppSettings["asSchedulePassword"]))
			using (TaskFolder reportsFolder = taskService.GetFolder(ConfigurationManager.AppSettings["asReportsFolderName"]))
			{
				foreach (ulong _deletedReportId in _deletedReports)
					try
					{
						reportsFolder.DeleteTask("GR" + _deletedReportId + ".job");
					}
					catch (System.IO.FileNotFoundException)
					{
						//"Гасим" это исключение при попытке удалить задание, которого не существует
					}
			}
		}

		PostData();

		if (dgvReports.Rows.Count > 0)
            btnApply.Visible = true;
        else
            btnApply.Visible = false;
    }

	protected void dgvReports_Sorting(object sender, GridViewSortEventArgs e)
	{

	}
}
