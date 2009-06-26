using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ReportTuner.Models;
using System.Configuration;
using Common.Web.Ui.Models;
using NHibernate.Criterion;
using Castle.ActiveRecord;
using System.Threading;
using Microsoft.Win32.TaskScheduler;
using ReportTuner.Helpers;

namespace ReportTuner.Reports
{
	public partial class TemporaryReportSchedule : System.Web.UI.Page
	{
		private GeneralReport _generalReport;
		private Task _currentTask;
		private TaskService _taskService;

		//Владелец всех контактных групп для отчета, прописан в Web.Config
		ContactGroupOwner _reportsContactGroupOwner;

		private const string StatusRunning = "Выполнить задание";
		private const string StatusNotRunning = "Выполняется...";

		protected void Page_Load(object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty(Request["TemporaryId"]))
				Response.Redirect("base.aspx");
			else
			{
				uint _ContactOwnerId;
				if (uint.TryParse(ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"], out _ContactOwnerId))
				{
					try
					{
						_reportsContactGroupOwner = ContactGroupOwner.Find(_ContactOwnerId);
					}
					catch (NotFoundException exp)
					{
						throw new Exception("В файле Web.Config параметр ReportsContactGroupOwnerId указывает на несуществующую запись.", exp);
					}
				}
				else
					throw new Exception("В файле Web.Config параметр ReportsContactGroupOwnerId не существует или настроен некорректно.");

				_generalReport = GeneralReport.Find(Convert.ToUInt64(Request["TemporaryId"]));
				_taskService = ScheduleHelper.GetService();
				_currentTask = ScheduleHelper.GetTask(
					_taskService, 
					ScheduleHelper.GetReportsFolder(_taskService), 
					_generalReport.Id,
					"Временный отчет, созданный " + _generalReport.TemporaryCreationDate.Value.ToString());

				btnRun.Enabled = _currentTask.Enabled && (_currentTask.State != TaskState.Running);
				btnRun.Text = (_currentTask.State == TaskState.Running) ? StatusNotRunning : StatusRunning;

				if ((_generalReport.ContactGroup == null) && btnRun.Enabled)
					btnRun.Enabled = false;

				if (!this.IsPostBack)
				{
					ClearSearch();
					if (_generalReport.ContactGroup != null)
						lContactGroupName.Text = _generalReport.ContactGroup.Name;
				}

			}

		}

		/// <summary>
		/// Закончили работу с TaskService
		/// </summary>
		private void CloseTaskService()
		{
			if (_currentTask != null)
			{
				_currentTask.Dispose();
				_currentTask = null;
			}
			if (_taskService != null)
			{
				_taskService.Dispose();
				_taskService = null;
			}
		}



		protected void ClearSearch()
		{
			tbContactFind.Visible = true;
			btnFind.Visible = true;
			ContactGroups.Visible = false;
			btnSaveContactGropup.Visible = false;
			btnCancelContactGroup.Visible = false;
		}

		protected void btnFinish_Click(object sender, EventArgs e)
		{
			if (_currentTask != null)
			{
				_currentTask.Dispose();
				_currentTask = null;
			}

			ScheduleHelper.DeleteTask(ScheduleHelper.GetReportsFolder(_taskService), _generalReport.Id);

			//Закончили работу с задачами
			if (_taskService != null)
			{
				_taskService.Dispose();
				_taskService = null;
			}

			//Удаляем отчет
			using (new TransactionScope())
			{
				_generalReport.Delete();
			}

			Response.Redirect("base.aspx");
		}

		protected void btnRun_Click(object sender, EventArgs e)
		{
			bool _runed = false;
			if (this.IsValid && (_currentTask.State != TaskState.Running) && (_generalReport.ContactGroup != null))
			{
				_currentTask.Run();
				Thread.Sleep(500);
				btnRun.Enabled = false;
				btnRun.Text = StatusNotRunning;
				_runed = true;
			}

			CloseTaskService();
			Thread.Sleep(500);
			if (_runed)
				Response.Redirect("TemporaryReportSchedule.aspx?TemporaryId=" + Request["TemporaryId"]);
		}

		protected void btnFind_Click(object sender, EventArgs e)
		{
			ContactGroup[] _findedContactGroups = ActiveRecordBase<ContactGroup>.FindAll(
				Order.Asc("Name"),
				Expression.Eq("ContactGroupOwner", _reportsContactGroupOwner),
				Expression.Eq("Type", ContactGroupType.Reports),
				Expression.Like("Name", "%" + tbContactFind.Text + "%"));

			ContactGroups.DataSource = _findedContactGroups;
			ContactGroups.DataTextField = "Name";
			ContactGroups.DataValueField = "Id";
			ContactGroups.DataBind();

			tbContactFind.Visible = false;
			btnFind.Visible = false;
			ContactGroups.Visible = true;
			if (_findedContactGroups.Length > 0)
				btnSaveContactGropup.Visible = true;
			btnCancelContactGroup.Visible = true;

			CloseTaskService();
		}

		protected void btnCancelContactGroup_Click(object sender, EventArgs e)
		{
			ClearSearch();

			CloseTaskService();
		}

		protected void btnSaveContactGropup_Click(object sender, EventArgs e)
		{
			uint _newGroupId;
			//попытка преобразовать выбранное значение в Id группы, если это получилось сделать, то установливаем новое значение
			if (uint.TryParse(ContactGroups.SelectedValue, out _newGroupId))
			{
				ContactGroup _newGroup = ContactGroup.Find(_newGroupId);
				using (new TransactionScope())
				{
					_generalReport.ContactGroup = _newGroup;
					_generalReport.Save();
				}

				lContactGroupName.Text = _generalReport.ContactGroup.Name;
				tbContactFind.Text = String.Empty;
				btnRun.Enabled = _currentTask.Enabled && (_currentTask.State != TaskState.Running);
			}

			ClearSearch();

			CloseTaskService();
		}

		protected void btnBack_Click(object sender, EventArgs e)
		{
			CloseTaskService();
			Report _temporaryReport = Report.FindFirst(
				Expression.Eq("GeneralReport", _generalReport)
			);

			Response.Redirect(String.Format("ReportProperties.aspx?TemporaryId={0}&rp={1}", _generalReport.Id, _temporaryReport.Id));
		}
	}
}
