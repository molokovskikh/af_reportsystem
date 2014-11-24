using System.Configuration;
using System.Linq;
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
			var report = DbSession.Load<GeneralReport>(reportId);
			PropertyBag["currentReport"] = report;
			PropertyBag["reports"] = DbSession.Query<GeneralReport>()
				.Where(r => r.ContactGroup == report.ContactGroup)
				.Select(r => new {r.Id, r.EMailSubject});
			var ownerId = uint.Parse(ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"]);
			if (filterValue == null)
				filterValue = "";
			PropertyBag["filterValue"] = filterValue;
			PropertyBag["Groups"] = DbSession.Query<ContactGroup>().Where(cg => cg.Type == ContactGroupType.Reports
				&& cg.ContactGroupOwner == DbSession.Load<ContactGroupOwner>(ownerId)
				&& cg.Name.Contains(filterValue)).Select(cg => new { Key = cg.Id, value = cg.Name})
				.OrderBy(c => c.value);
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
	}
}