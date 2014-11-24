using System;
using System.Collections.Generic;
using System.Configuration;
using Common.Web.Ui.Models;
using NHibernate.Util;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support.Web;
using WatiN.Core;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class ContactsFixture : WatinFixture2
	{
		private GeneralReport _report;
		private ContactGroupOwner _contactGroupOwner;

		[SetUp]
		public void SetUp()
		{
			_report = new GeneralReport() {
				EMailSubject = "Тестовый отчет"
			};
			var ownerId = uint.Parse(ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"]);
			_contactGroupOwner = session.Get<ContactGroupOwner>(ownerId);
			session.Save(_report);
		}

		[Test]
		public void Show()
		{
			var privateGroup = new ContactGroup(_contactGroupOwner, ContactGroupType.Reports) {
				Name = "Приватная рассылка"
			};
			session.Save(privateGroup);
			var publicGroup = new ContactGroup(_contactGroupOwner, ContactGroupType.ReportSubscribers) {
				Name = "Публичная рассылка"
			};
			session.Save(publicGroup);
			_report.ContactGroup = privateGroup;
			_report.PublicSubscriptions = publicGroup;
			session.Save(_report);
			var privateContact = new Contact(ContactType.Email, "qwe@qwe.ru") {
				ContactOwner = privateGroup
			};
			session.Save(privateContact);
			var publicContact = new Contact(ContactType.Email, "ewq@eqw.com") {
				ContactOwner = publicGroup
			};
			session.Save(publicContact);
			var contactGroupOwner = new ContactGroupOwner();
			session.Save(contactGroupOwner);
			var payerContactOwner = new ContactGroup(contactGroupOwner, ContactGroupType.ReportSubscribers);
			session.Save(payerContactOwner);
			var payer = new Payer("Тестовый плательщик") {
				ContactGroupOwner = contactGroupOwner
			};
			session.Save(payer);
			var payercontact = new Contact(payerContactOwner) {
				ContactText = "ewq@eqw.com"
			};
			session.Save(payercontact);
			var reportSub = new ReportSubscriptionContact(payercontact, publicContact);
			session.Save(reportSub);

			Open(String.Format("/Contacts/Show?reportId={0}", _report.Id));

			AssertText("qwe@qwe.ru");
			AssertText("ewq@eqw.com");
			AssertText("Тестовый плательщик");
			AssertText("Приватная рассылка");
		}

		[Test]
		public void EditGroupName()
		{
			var privateGroup = new ContactGroup(_contactGroupOwner, ContactGroupType.Reports) {
				Name = "Приватная рассылка"
			};
			session.Save(privateGroup);
			_report.ContactGroup = privateGroup;
			session.Save(_report);
			var privateContact = new Contact(ContactType.Email, "qwe@qwe.ru") {
				ContactOwner = privateGroup
			};
			session.Save(privateContact);

			Open(String.Format("/Contacts/Show?reportId={0}", _report.Id));

			ClickLink("Редактировать имя");

			browser.TextField(Find.ByValue(privateGroup.Name)).Value = "Рассылка приватная";

			ClickButton("Сохранить");

			AssertText("Рассылка приватная");
			AssertText("qwe@qwe.ru");
		}

		[Test]
		public void NewGroup()
		{
			Open(String.Format("/Contacts/Show?reportId={0}", _report.Id));

			ClickLink("Создать новую рассылку");

			browser.TextField(Find.ByName("ContactGroup.Name")).Value = "Рассылка приватная";

			ClickButton("Сохранить");

			AssertText("Рассылка приватная");
		}

		[Test]
		public void DeletePublicSubscriptions()
		{
			var privateGroup = new ContactGroup(_contactGroupOwner, ContactGroupType.Reports) {
				Name = "Приватная рассылка"
			};
			session.Save(privateGroup);
			var publicGroup = new ContactGroup(_contactGroupOwner, ContactGroupType.ReportSubscribers) {
				Name = "Публичная рассылка"
			};
			session.Save(publicGroup);
			_report.ContactGroup = privateGroup;
			_report.PublicSubscriptions = publicGroup;
			session.Save(_report);
			var privateContact = new Contact(ContactType.Email, "qwe@qwe.ru") {
				ContactOwner = privateGroup
			};
			session.Save(privateContact);
			var publicContact = new Contact(ContactType.Email, "ewq@eqw.com") {
				ContactOwner = publicGroup
			};
			session.Save(publicContact);

			Open(String.Format("/Contacts/Show?reportId={0}", _report.Id));
			Click("Отписать");

			AssertNoText("qwe@qwe.ru");
			AssertText("ewq@eqw.com");
			AssertText("Приватная рассылка");
		}
	}
}