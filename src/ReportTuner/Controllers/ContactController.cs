using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Models;

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

		public void CloseWindow()
		{
			RenderView(@"..\Common\CloseWindow");
		}
	}
}