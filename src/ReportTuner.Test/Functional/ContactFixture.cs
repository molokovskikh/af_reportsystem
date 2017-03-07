using System;
using Common.Web.Ui.Models;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support.Selenium;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class ContactFixture : SeleniumFixture
	{
		private ContactGroup _contactGroup;
		[SetUp]
		public void SetUp()
		{
			var contactGroupOwner = new ContactGroupOwner();
			session.Save(contactGroupOwner);
			_contactGroup = new ContactGroup(ContactGroupType.Reports, "testGroup") { ContactGroupOwner = contactGroupOwner };
			session.Save(_contactGroup);
			var contact = new Contact(ContactType.Email, "test@test.net") { ContactOwner = _contactGroup };
			_contactGroup.Contacts.Add(contact);
			var payer = new Payer("testPayer");
			session.Save(payer);
			var contactPayer = new Contact(ContactType.Email, "ContactPayer@analit.net") { ContactOwner = _contactGroup };
			_contactGroup.Contacts.Add(contactPayer);
			session.Save(_contactGroup);
			session.Save(payer);
			var payerOwner = new PayerOwnerContact { Payer = payer, Contact = contactPayer };
			session.Save(payerOwner);
			Close();
		}

		[Test]
		public void Payer_comment_contact_test()
		{
			Open(string.Format("Contact/EditContactGroup.rails?contactGroupId={0}", _contactGroup.Id));
			AssertText("ContactPayer@analit.net");
			AssertText("testPayer");
		}

		[Test(Description = "Проверяет корректность перехода по кнопке добавления контактного лица")]
		public void AddNewPersonButtonClick()
		{
			Open(string.Format("Contact/EditContactGroup.rails?contactGroupId={0}", _contactGroup.Id));
			Click("Добавить контактное лицо");
			AssertText("Редактирование контактного лица");
		}
	}
}
