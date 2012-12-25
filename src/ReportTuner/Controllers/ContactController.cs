using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Models;
using NHibernate.Linq;
using ReportTuner.Models;
using System.Linq;

namespace ReportTuner.Controllers
{
	[Layout("contact")]
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
			base.UpdateContactGroup(contactGroupId, contacts);
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