using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models;
using NHibernate.Linq;
using ReportTuner.Helpers;
using ReportTuner.Models;
using System.Linq;

namespace ReportTuner.Controllers
{
	public class ContactController : AbstractContactController
	{
		public override void AddPerson(uint contactGroupId,
			[DataBind("CurrentPerson")] Person person,
			[DataBind("Contacts")] Contact[] contacts)
		{
			base.AddPerson(contactGroupId, person, contacts);
			if (Response.StatusCode == 302)
				RedirectToAction("CloseWindow");
		}

		public override void UpdateContactGroup(uint contactGroupId,
			[DataBind("Contacts")] Contact[] contacts)
		{
			var actualContactId = contacts.Select(c => c.Id).ToList();
			var oldContacts = DbSession.Query<Contact>().Where(c => actualContactId.Contains(c.Id)).ToList().ToDictionary(c => c.Id);
			var mails = contacts.Where(c => c.Id == 0 || oldContacts[c.Id].ContactText != c.ContactText).Select(c => c.ContactText).Distinct().ToList();
			var payerContacts = DbSession.Query<PayerOwnerContact>().Where(c => mails.Contains(c.Contact.ContactText)).ToList();
			if (payerContacts.Count > 0) {
				var errorBuilder = new StringBuilder();
				foreach (var email in mails) {
					var payers = payerContacts.Where(p => p.Contact.ContactText == email).GroupBy(g => g.Payer).ToList();
					foreach (var payer in payers) {
						errorBuilder.AppendLine(string.Format("E-mail <b>{0}</b> уже зарегистрирован для плательщика <b>{1}</b>", email, payer.Key.ShortName));
					}
				}
				if (errorBuilder.Length > 0) {
					errorBuilder.AppendLine();
					errorBuilder.AppendLine("Для добавления данных E-mail в список рассылки отчетов воспользуйтесь Л.К. любого Поставщика или Аптеки для этого Плательщика");
					PropertyBag["Message"] = Message.Error(errorBuilder.ToString().Replace("\r\n", "<br/>"));
				}
				var contactGroup = ContactGroup.Find(contactGroupId);
				RenderInvalidGroup(contacts, contactGroup);
			}
			else {
				base.UpdateContactGroup(contactGroupId, contacts);
			}
			if (Response.StatusCode == 302)
				RedirectToAction("CloseWindow");
		}

		public override void UpdatePerson([DataBind("CurrentPerson")] Person person,
			[DataBind("Contacts")] Contact[] contacts)
		{
			base.UpdatePerson(person, contacts);
			if (Response.StatusCode == 302)
				RedirectToAction("CloseWindow");
		}

		public override void EditContactGroup(uint contactGroupId)
		{
			var contactGroup = ContactGroup.Find(contactGroupId);
			var payerOwners = DbSession.Query<PayerOwnerContact>()
				.Where(p => contactGroup.Contacts.Contains(p.Contact))
				.Fetch(p => p.Payer)
				.ToDictionary(k => k.Contact);
			foreach (var contact in contactGroup.Contacts) {
				if (payerOwners.Keys.Contains(contact)) {
					var payer = payerOwners[contact].Payer;
					contact.PayerOwnerName = string.Format("[{0}] - \"{1}\"", payer.Id, payer.ShortName);
				}
			}
			PropertyBag["CurrentContactGroup"] = contactGroup;
		}

		public void CloseWindow()
		{
			RenderView(@"..\Common\CloseWindow");
		}
	}
}