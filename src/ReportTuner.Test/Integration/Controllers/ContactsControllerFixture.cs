using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.Core.Internal;
using Common.Web.Ui.Models;
using Common.Web.Ui.NHibernateExtentions;
using Common.Web.Ui.Test.Controllers;
using NHibernate.Linq;
using NUnit.Framework;
using ReportTuner.Controllers;
using ReportTuner.Models;

namespace ReportTuner.Test.Integration.Controllers
{
	public class ContactsControllerFixture : ControllerFixture
	{
		private ContactsController _controller;
		private GeneralReport _report;
		private ContactGroupOwner _contactGroupOwner;

		[SetUp]
		public void Setup()
		{
			_controller = new ContactsController();
			Prepare(_controller);
			_report = new GeneralReport() {
				EMailSubject = "Тестовый отчет"
			};
			var ownerId = uint.Parse(ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"]);
			_contactGroupOwner = session.Get<ContactGroupOwner>(ownerId);
			session.Save(_report);
		}

		[Test]
		public void FilterTest()
		{
			var privateGroup = new ContactGroup(_contactGroupOwner, ContactGroupType.Reports) {
				Name = "Фильтр 1"
			};
			session.Save(privateGroup);
			var privateGroup2 = new ContactGroup(_contactGroupOwner, ContactGroupType.Reports) {
				Name = "Фильтр 2"
			};
			session.Save(privateGroup2);
			_report.ContactGroup = privateGroup;
			session.Save(_report);
			var startCount = session.Query<ContactGroup>()
				.Where(g => g.ContactGroupOwner == _contactGroupOwner).Count();

			_controller.Show(_report.Id, "ильтр 1");

			var groupsCount = ((IEnumerable<object>)_controller.PropertyBag["Groups"]).Count();
			Assert.That(groupsCount < startCount);
		}
	}
}