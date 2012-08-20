using Castle.ActiveRecord;
using Castle.MonoRail.TestSupport;
using Common.Web.Ui.Models;
using NUnit.Framework;
using ReportTuner.Controllers;
using ReportTuner.Models;
using Test.Support.log4net;

namespace ReportTuner.Test.Integration
{
	[TestFixture]
	public class ReportControllerFixture : BaseControllerTest
	{
		[Test]
		public void Delete_report()
		{
			ulong reportId;
			uint groupId;
			using (new SessionScope()) {
				var report = new GeneralReport();
				report.ContactGroup = new ContactGroup(ContactGroupType.Reports);
				report.ContactGroup.ContactGroupOwner = new ContactGroupOwner();
				report.ContactGroup.ContactGroupOwner.Save();
				report.ContactGroup.Save();
				report.SaveAndFlush();

				reportId = report.Id;
				groupId = report.ContactGroup.Id;

				var controller = new ReportsController();
				PrepareController(controller);
				controller.Delete(new[] { report.Id });
			}

			using (new SessionScope()) {
				Assert.That(reportId, Is.Not.EqualTo(0ul));
				Assert.That(groupId, Is.Not.EqualTo(0u));
				Assert.That(GeneralReport.TryFind(reportId), Is.Null);
				Assert.That(ActiveRecordMediator<ContactGroup>.FindByPrimaryKey(groupId, false), Is.Null);
			}
		}
	}
}