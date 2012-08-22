using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ReportTuner.Models;
using Castle.ActiveRecord;
using NHibernate.Criterion;
using System.Configuration;
using ReportTuner.Helpers;

namespace ReportTuner.Reports
{
	public partial class TemporaryReport : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty(Request["TemporaryId"])) {
				GeneralReport _generalReport = new GeneralReport() { Allow = true, Temporary = true, TemporaryCreationDate = DateTime.Now, Format = "Excel" };
				//Выставляем плательщика 921 (Офис123)
				//todo: Возможно потом это надо удалить
				_generalReport.Payer = Payer.Find((uint)921);
				using (new TransactionScope()) {
					_generalReport.Save();
				}
				Response.Redirect("TemporaryReport.aspx?TemporaryId=" + _generalReport.Id);
			}

			if (!this.IsPostBack) {
				ReportType[] _reportTypes = ReportType.FindAll(Order.Asc("ReportTypeName"));
				ddlReportTypes.DataSource = _reportTypes;
				ddlReportTypes.DataTextField = "ReportTypeName";
				ddlReportTypes.DataValueField = "Id";
				ddlReportTypes.DataBind();

				tbReportName.Text = _reportTypes[0].AlternateSubject;

				BindTemplateReports(_reportTypes[0]);
			}
		}

		private void BindTemplateReports(ReportType selectedReportType)
		{
			Report[] _templateReports = Report.FindAll(
				Order.Asc("ReportCaption"),
				Expression.Eq("GeneralReport", GeneralReport.Find(Convert.ToUInt64(ConfigurationManager.AppSettings["TemplateReportId"]))),
				Expression.Eq("ReportType", selectedReportType));
			if (_templateReports.Length > 0) {
				ddlTemplates.Visible = true;
				ddlTemplates.DataSource = _templateReports;
				ddlTemplates.DataTextField = "ReportCaption";
				ddlTemplates.DataValueField = "Id";
				ddlTemplates.DataBind();
				ddlTemplates.Items.Insert(0, new ListItem("не установлен", String.Empty));
			}
			else
				ddlTemplates.Visible = false;
		}

		protected void ddlReportTypes_SelectedIndexChanged(object sender, EventArgs e)
		{
			ReportType _selectedReporType = ReportType.Find(Convert.ToUInt64(ddlReportTypes.SelectedValue));
			tbReportName.Text = _selectedReporType.AlternateSubject;

			BindTemplateReports(_selectedReporType);
		}

		protected void btnNext_Click(object sender, EventArgs e)
		{
			if (this.IsValid) {
				GeneralReport _generalReport = GeneralReport.Find(Convert.ToUInt64(Request["TemporaryId"]));
				ReportType _reportType = ReportType.Find(Convert.ToUInt64(ddlReportTypes.SelectedValue));
				Report _newReport = new Report() {
					GeneralReport = _generalReport,
					ReportType = _reportType,
					Enabled = true,
					ReportCaption = tbReportName.Text
				};

				Report[] _oldReports = Report.FindAll(Expression.Eq("GeneralReport", _generalReport));

				using (new TransactionScope()) {
					foreach (Report _deletedReport in _oldReports)
						_deletedReport.Delete();
					_newReport.Save();
				}

				if (ddlTemplates.Visible && (ddlTemplates.SelectedIndex > 0)) {
					ulong _sourceTemplateReport = Convert.ToUInt64(ddlTemplates.SelectedValue);
					ReportHelper.CopyReportProperties(_sourceTemplateReport, _newReport.Id);
				}

				Response.Redirect(String.Format(
					"ReportProperties.aspx?TemporaryId={0}&rp={1}", Request["TemporaryId"], _newReport.Id));
			}
		}
	}
}