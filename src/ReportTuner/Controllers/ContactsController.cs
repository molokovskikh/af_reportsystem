using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using Boo.Lang;
using Common.MySql;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Models;
using Common.Web.Ui.NHibernateExtentions;
using NHibernate.Linq;
using ReportTuner.Models;

namespace ReportTuner.Controllers
{
	public class ContactsController : AbstractContactController
	{
		public void Show(ulong reportId, string filterValue = null)
		{
			PropertyBag["searchText"] = PropertyBag["searchText"] ?? "";
			var report = DbSession.Load<GeneralReport>(reportId);
			PropertyBag["currentReport"] = report;
			PropertyBag["reports"] = DbSession.Query<GeneralReport>()
				.Where(r => r.ContactGroup == report.ContactGroup)
				.Select(r => new {r.Id, r.EMailSubject});
			var ownerId = uint.Parse(ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"]);
			if (filterValue == null)
				filterValue = "";
			PropertyBag["filterValue"] = filterValue;
			var owner = DbSession.Load<ContactGroupOwner>(ownerId);
			var groups = DbSession.Query<ContactGroup>()
				.Where(cg => cg.Type == ContactGroupType.Reports && cg.ContactGroupOwner == owner && cg.Name.Contains(filterValue))
				.Select(cg => new { Key = cg.Id, value = cg.Name})
				.OrderBy(c => c.value);
			PropertyBag["Groups"] = groups;

			if (report.PublicSubscriptions != null)
				PropertyBag["PublicGroupContacts"] = report.PublicSubscriptions.Contacts
					.Select(c => new {
						Id = c.Id,
						ContactText = c.ContactText,
						Comment = c.Comment,
						Payer = GetPayerName(c)
					});
		}

		public void EditGroupName(uint contactGroupId, ulong reportId)
		{
			var contactGroup = DbSession.Load<ContactGroup>(contactGroupId);
			PropertyBag["reportId"] = reportId;
			PropertyBag["ContactGroup"] = contactGroup;
			if (IsPost) {
				BindObjectInstance(contactGroup, "ContactGroup");
				if (IsValid(contactGroup)) {
					DbSession.Save(contactGroup);
					RedirectToAction("Show", new { reportId = reportId });
				}
			}
		}

		public void NewGroup(ulong reportId)
		{
			var ownerId = uint.Parse(ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"]);
			var contactGroup = new ContactGroup(ContactGroupType.Reports) {
				ContactGroupOwner = DbSession.Load<ContactGroupOwner>(ownerId)
			};
			PropertyBag["reportId"] = reportId;
			PropertyBag["ContactGroup"] = contactGroup;
			if (IsPost) {
				BindObjectInstance(contactGroup, "ContactGroup");
				if (IsValid(contactGroup)) {
					DbSession.Save(contactGroup);
					var report = DbSession.Load<GeneralReport>(reportId);
					report.ContactGroup = contactGroup;
					DbSession.Save(report);
					RedirectToAction("Show", new { reportId = reportId });
				}
			}
		}

		/// <summary>
		/// Выбор рассылки для отчета. Рассылкой является группа контактов
		/// </summary>
		/// <param name="contactGroupId">Идентификатор группы контактов</param>
		/// <param name="reportId">Идентификатор генерального отчета</param>
		public void SelectGroup(uint contactGroupId, ulong reportId)
		{
			var report = DbSession.Load<GeneralReport>(reportId);
			report.ContactGroup = DbSession.Load<ContactGroup>(contactGroupId);
			DbSession.Save(report);
			RedirectToReferrer();
		}

		public void DeletePublicSubscriptions(uint contactId)
		{
			var contact = DbSession.Load<Contact>(contactId);
			var reportSubscribe = DbSession.Query<ReportSubscriptionContact>()
				.Where(rs => rs.ReportContact == contact).ToArray();
			DbSession.DeleteMany(reportSubscribe);
			DbSession.Delete(contact);
			RedirectToReferrer();
		}

		private string GetPayerName(Contact reportContact)
		{
			var contact = DbSession.Query<ReportSubscriptionContact>()
				.Where(rsc => rsc.ReportContact == reportContact)
				.Select(rsc => rsc.PayerContact)
				.FirstOrDefault();
			if (contact == null)
				return "";
			var payer = DbSession.Query<Payer>()
				.FirstOrDefault(p => p.ContactGroupOwner == ((ContactGroup)contact.ContactOwner).ContactGroupOwner);
			return payer != null ? payer.ShortName : "";
		}

		/// <summary>
		/// Отображение списка отчетов, при поиске отчетов по названию, чтобы скопировать самостоятельные подписки
		/// </summary>
		/// <param name="reportId"></param>
		/// <param name="searchText">Комментарий отчета или его идентификатор</param>
		public void FindReportsByNameOrId(uint reportId, string searchText)
		{
			PropertyBag["searchText"] = searchText;
			uint id;
			uint.TryParse(searchText, out id);
			var reports = DbSession.Query<GeneralReport>().Where(i => i.Comment.Contains(searchText) || i.Id == id).OrderBy(i => i.Comment).ToList();
			PropertyBag["foundedReports"] = reports;
			Show(reportId);
			RenderView("Show");
		}

		/// <summary>
		/// Копирование самостоятельных подписчиков из одного отчета в другой.
		/// Копирование является расширяющим. То есть отчету добавляются подписки, но не удаляются старые.
		/// </summary>
		/// <param name="reportId">Идентификатор отчета, куда надо добавить подписки</param>
		/// <param name="donorReportId">Идентификатор отчета, откуда надо взять подписки</param>
		public void CopyOwnContactsFromReport(ulong reportId, ulong donorReportId)
		{
			var report = DbSession.Load<GeneralReport>(reportId);
			var donor = DbSession.Load<GeneralReport>(donorReportId);

			//Если у отчета еще нет группы контактов, то ее необходимо создать
			if (report.PublicSubscriptions == null) {
				var ownerId = uint.Parse(ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"]);
				var contactGroup = new ContactGroup(ContactGroupType.Reports);
				var owner = DbSession.Load<ContactGroupOwner>(ownerId);
				contactGroup.ContactGroupOwner = owner;
				report.PublicSubscriptions = contactGroup;
				DbSession.Save(contactGroup);
				DbSession.Save(report);
			}

			var errorFlag = donor.PublicSubscriptions == null || donor.PublicSubscriptions.Contacts.Count == 0;
			if (!errorFlag) {
				//Если все в порядке, импортируем подписчиков
				foreach (var contact in donor.PublicSubscriptions.Contacts)
					report.PublicSubscriptions.AddContact(contact.Type, contact.ContactText);
				DbSession.Save(report);
			}
			else
				Error("У указанного отчета нет публичных подписчиков");
			RedirectToAction("Show", new {reportId});
		}
	}
}