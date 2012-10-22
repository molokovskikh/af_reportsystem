using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Common.MySql;
using ReportTuner.Models;
using Common.Web.Ui.Models;
using Castle.ActiveRecord;
using NHibernate.Criterion;
using System.Data;
using MySql.Data.MySqlClient;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;


namespace ReportTuner
{
	public partial class Contacts : System.Web.UI.Page
	{
		//Текущий редактируемый отчет
		private GeneralReport _currentReport;
		//Владелец всех контактных групп для отчета, прописан в Web.Config
		private ContactGroupOwner _reportsContactGroupOwner;
		//Текущая редактируемая контактная группа
		private ContactGroup _currentContactGroup;
		//все отчеты, которые используют ту же контактную группу
		private GeneralReport[] _relatedReportsByContactGroup;

		//Имя переменной в сессии, говорящей о том, кто вызвал отображение контролов
		//для изменения имени группы: кнопка "Изменить наименование" или кнопка "Создать группу"
		private string _changeSenderSessionName = "ReportTuner.Contacts.ChangeSender";

		protected void Page_Load(object sender, EventArgs e)
		{
			ulong _generalReportCode;
			if (ulong.TryParse(Request["GeneralReport"], out _generalReportCode)) {
				uint _ContactOwnerId;
				if (uint.TryParse(System.Configuration.ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"], out _ContactOwnerId)) {
					try {
						_reportsContactGroupOwner = ContactGroupOwner.Find(_ContactOwnerId);
					}
					catch (NotFoundException exp) {
						throw new ReportTunerException("В файле Web.Config параметр ReportsContactGroupOwnerId указывает на несуществующую запись.", exp);
					}
				}
				else
					throw new ReportTunerException("В файле Web.Config параметр ReportsContactGroupOwnerId не существует или настроен некорректно.");

				//текущий отчет
				_currentReport = GeneralReport.Find(_generalReportCode);
				//текущая контактная группа для данного отчета
				_currentContactGroup = _currentReport.ContactGroup;

				BindRelatedReports();

				if (!this.IsPostBack) {
					lReportName.Text = _currentReport.EMailSubject;

					if (_currentContactGroup != null) {
						hlEditGroup.Text = _currentContactGroup.Name;
						hlEditGroup.NavigateUrl = "~/Contact/EditContactGroup.rails?contactGroupId=" + _currentContactGroup.Id;
						BindEmailList();
					}

					ClearSearch();

					ClearChangeName();
				}
			}
			else
				Response.Redirect("GeneralReports.aspx");
		}

		protected void BindRelatedReports()
		{
			//все отчеты, которые связаны на ту же контактную группу
			_relatedReportsByContactGroup = GeneralReport.FindAll(Order.Asc("Id"), Expression.Eq("ContactGroup", _currentContactGroup));
			int _currentIndex = Array.IndexOf<GeneralReport>(_relatedReportsByContactGroup, _currentReport);
			gvRelatedReports.DataSource = _relatedReportsByContactGroup;
			gvRelatedReports.SelectedIndex = _currentIndex;
			gvRelatedReports.DataBind();
		}

		protected void BindEmailList()
		{
			DataSet dsContacts = MySqlHelper.ExecuteDataset(ConnectionHelper.GetConnectionString(), @"
select lower(c.contactText) as ContactText
from
  contacts.contact_groups cg
  join contacts.contacts c on cg.Id = c.ContactOwnerId
where
    cg.Id = ?ContactGroupId
and cg.Type = ?ContactGroupType
and c.Type = ?ContactType
union
select lower(c.contactText) as ContactText
from
  contacts.contact_groups cg
  join contacts.persons p on cg.id = p.ContactGroupId
  join contacts.contacts c on p.Id = c.ContactOwnerId
where
    cg.Id = ?ContactGroupId
and cg.Type = ?ContactGroupType
and c.Type = ?ContactType
order by 1",
				new MySqlParameter("?ContactGroupId", _currentContactGroup.Id),
				new MySqlParameter("?ContactGroupType", 6),
				new MySqlParameter("?ContactType", MySqlDbType.Byte) { Value = 0 });

			gvEmails.DataSource = dsContacts.Tables[0];
			gvEmails.Width = Unit.Pixel(250);
			gvEmails.DataBind();
		}

		protected void ClearSearch()
		{
			tbContactFind.Visible = true;
			btnFind.Visible = true;
			ContactGroups.Visible = false;
			btnSaveContactGropup.Visible = false;
			btnCancelContactGroup.Visible = false;
		}

		protected void ClearChangeName()
		{
			hlEditGroup.Visible = true;
			gvEmails.Visible = true;
			btnChangeGroupName.Visible = (_currentContactGroup != null);
			btnCreate.Visible = (_currentContactGroup == null) || ((_relatedReportsByContactGroup != null) && (_relatedReportsByContactGroup.Length > 1));
			tbContactGroupName.Visible = false;
			btnSaveChangedGroupName.Visible = false;
			btnCancelChangeGroupName.Visible = false;
		}

		protected void cvOnLikeName_ServerValidate(object source, ServerValidateEventArgs args)
		{
			//Пытаемся найти в существующих группах с таким же названием, если находим, то не даем создавать и изменять
			args.IsValid = !ActiveRecordBase<ContactGroup>.Exists(
				Expression.Eq("ContactGroupOwner", _reportsContactGroupOwner),
				Expression.Eq("Type", ContactGroupType.Reports),
				Expression.Eq("Name", args.Value));
		}

		protected void btnCreate_Click(object sender, EventArgs e)
		{
			ClearSearch();
			hlEditGroup.Visible = false;
			gvEmails.Visible = false;
			btnChangeGroupName.Visible = false;
			btnCreate.Visible = false;
			tbContactGroupName.Text = _currentReport.EMailSubject;
			tbContactGroupName.Visible = true;
			btnSaveChangedGroupName.Visible = true;
			btnCancelChangeGroupName.Visible = true;
			Session[_changeSenderSessionName] = "Create";
		}


		protected void btnChangeGroupName_Click(object sender, EventArgs e)
		{
			ClearSearch();
			hlEditGroup.Visible = false;
			gvEmails.Visible = false;
			btnChangeGroupName.Visible = false;
			btnCreate.Visible = false;
			tbContactGroupName.Text = _currentContactGroup.Name;
			tbContactGroupName.Visible = true;
			btnSaveChangedGroupName.Visible = true;
			btnCancelChangeGroupName.Visible = true;
			Session[_changeSenderSessionName] = "Change";
		}

		protected void btnCancelChangeGroupName_Click(object sender, EventArgs e)
		{
			ClearChangeName();
		}

		protected void btnSaveChangedGroupName_Click(object sender, EventArgs e)
		{
			if (!this.IsValid)
				return;

			string _senderName = (string)Session[_changeSenderSessionName];
			if (String.IsNullOrEmpty(_senderName))
				Response.Redirect("Contacts.aspx?GeneralReport=" + _currentReport.Id);

			if (_senderName.Equals("Change", StringComparison.OrdinalIgnoreCase) || _senderName.Equals("Create", StringComparison.OrdinalIgnoreCase)) {
				if (_senderName.Equals("Change", StringComparison.OrdinalIgnoreCase) && (_currentContactGroup != null)) {
					_currentContactGroup.Name = tbContactGroupName.Text;
					using (new TransactionScope()) {
						_currentContactGroup.Save();
					}
					hlEditGroup.Text = tbContactGroupName.Text;
				}
				else {
					_currentContactGroup = new ContactGroup {
						Name = tbContactGroupName.Text,
						Type = ContactGroupType.Reports
					};
					using (new TransactionScope()) {
						_currentContactGroup.ContactGroupOwner = _reportsContactGroupOwner;
						_currentReport.ContactGroup = _currentContactGroup;
						_currentContactGroup.Save();
						_currentReport.Save();
					}

					hlEditGroup.Text = tbContactGroupName.Text;
					hlEditGroup.NavigateUrl = "~/Contact/EditContactGroup.rails?contactGroupId=" + _currentContactGroup.Id;

					BindRelatedReports();

					BindEmailList();
				}

				ClearSearch();

				ClearChangeName();
			}
			else
				Response.Redirect("Contacts.aspx?GeneralReport=" + _currentReport.Id);
		}

		protected void btnFind_Click(object sender, EventArgs e)
		{
			ClearChangeName();

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
		}

		protected void btnCancelContactGroup_Click(object sender, EventArgs e)
		{
			ClearSearch();
		}

		protected void btnSaveContactGropup_Click(object sender, EventArgs e)
		{
			uint _newGroupId;
			//попытка преобразовать выбранное значение в Id группы, если это получилось сделать, то установливаем новое значение
			if (uint.TryParse(ContactGroups.SelectedValue, out _newGroupId)) {
				ContactGroup _newGroup = ContactGroup.Find(_newGroupId);
				using (new TransactionScope()) {
					_currentReport.ContactGroup = _newGroup;
					_currentReport.Save();
				}
				_currentContactGroup = _newGroup;

				hlEditGroup.Text = _currentContactGroup.Name;
				hlEditGroup.NavigateUrl = "~/Contact/EditContactGroup.rails?contactGroupId=" + _currentContactGroup.Id;
				tbContactFind.Text = String.Empty;
				btnChangeGroupName.Visible = true;

				BindRelatedReports();

				BindEmailList();
			}

			ClearSearch();
			Response.Redirect("Contacts.aspx?GeneralReport=" + _currentReport.Id);
		}
	}
}