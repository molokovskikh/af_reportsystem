using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Castle.MonoRail.TestSupport;
using Common.Web.Ui.Models;
using Common.Web.Ui.Test.Controllers;
using NUnit.Framework;
using ReportTuner.Controllers;
using ReportTuner.Models;
using Test.Support.log4net;

namespace ReportTuner.Test.Integration
{
	[TestFixture]
	public class ReportControllerFixture : ControllerFixture
	{
		[Test]
		public void Delete_report()
		{
			var report = new GeneralReport();
			report.ContactGroup = new ContactGroup(ContactGroupType.Reports);
			report.ContactGroup.ContactGroupOwner = new ContactGroupOwner();
			session.Save(report.ContactGroup.ContactGroupOwner);
			session.Save(report.ContactGroup);
			session.Save(report);

			var controller = new ReportsController();
			Prepare(controller);
			controller.Delete(new[] { report.Id });

			session.Flush();
			session.Clear();
			Assert.IsNull(session.Get<GeneralReport>(report.Id));
			Assert.IsNull(session.Get<ContactGroup>(report.ContactGroup.Id));
		}
	}
}