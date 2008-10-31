using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ReportTuner.Models;
using TaskScheduler;
using System.Configuration;
using Common.Web.Ui.Models;
using NHibernate.Criterion;
using Castle.ActiveRecord;
using System.Threading;

namespace ReportTuner.Reports
{
	public partial class TemporaryReportSchedule : System.Web.UI.Page
	{
		private GeneralReport _generalReport;
		private Task _currentTask;
		private ScheduledTasks _scheduledTasks;

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
				_scheduledTasks = new ScheduledTasks(ConfigurationManager.AppSettings["asComp"]);
				_currentTask = FindTask(_scheduledTasks, _generalReport.Id);

				btnRun.Enabled = _currentTask.Status != TaskStatus.Running;
				btnRun.Text = (btnRun.Enabled) ? StatusRunning : StatusNotRunning;

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

		private Task FindTask(ScheduledTasks scheduledTasks, ulong temporaryId)
		{
			string _findTaskName = "GR" + temporaryId + ".job";
			Task _findedTask = null;
			string[] taskNames = scheduledTasks.GetTaskNames();
			bool find = false;
			foreach (string name in taskNames)
			{
				if (name.Equals(_findTaskName))
				{
					find = true;
					_findedTask = scheduledTasks.OpenTask(name);
					break;
				}
			}
			if (!find)
			{
				_findedTask = CreateNewTask(scheduledTasks, _findTaskName);
				_findedTask = scheduledTasks.OpenTask(_findTaskName);
			}
			return _findedTask;
		}

		private Task CreateNewTask(ScheduledTasks scheduledTasks, string creationTaskName)
		{
			Task _createdTask = scheduledTasks.CreateTask(creationTaskName);

			_createdTask.ApplicationName = ConfigurationManager.AppSettings["asApp"];
			_createdTask.Parameters = "/gr:" + Request["TemporaryId"];
			if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["asScheduleUserName"]))
				_createdTask.SetAccountInformation(String.Empty, null);
			else
				_createdTask.SetAccountInformation(
					ConfigurationManager.AppSettings["asScheduleUserName"], 
					ConfigurationManager.AppSettings["asSchedulePassword"]);
			_createdTask.WorkingDirectory = ConfigurationManager.AppSettings["asWorkDir"];
			_createdTask.Comment = "Временный отчет, созданный " + _generalReport.TemporaryCreationDate.Value.ToString();
			_createdTask.Save();
			_createdTask.Close();
			return _createdTask;
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
			_currentTask.Close();
			//Удаляем задачу
			_scheduledTasks.DeleteTask("GR" + _generalReport.Id + ".job");
			//Закончили работу с задачами
			_scheduledTasks.Dispose();

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
			if (this.IsValid && (_currentTask.Status != TaskStatus.Running) && (_generalReport.ContactGroup != null))
			{
				_currentTask.Run();
				Thread.Sleep(500);
				_runed = true;
			}
			_currentTask.Close();
			//Закончили работу с задачами
			_scheduledTasks.Dispose();
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

			_currentTask.Close();
			//Закончили работу с задачами
			_scheduledTasks.Dispose();
		}

		protected void btnCancelContactGroup_Click(object sender, EventArgs e)
		{
			ClearSearch();

			_currentTask.Close();
			//Закончили работу с задачами
			_scheduledTasks.Dispose();
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
				btnRun.Enabled = _currentTask.Status != TaskStatus.Running;
			}

			ClearSearch();

			_currentTask.Close();
			//Закончили работу с задачами
			_scheduledTasks.Dispose();
		}

		protected void btnBack_Click(object sender, EventArgs e)
		{
			_currentTask.Close();
			//Закончили работу с задачами
			_scheduledTasks.Dispose();
			Report _temporaryReport = Report.FindFirst(
				Expression.Eq("GeneralReport", _generalReport)
			);

			Response.Redirect(String.Format("ReportProperties.aspx?TemporaryId={0}&rp={1}", _generalReport.Id, _temporaryReport.Id));

		}
	}
}
