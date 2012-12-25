using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Web.Ui.Models;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support.Web;
using WatiN.Core.Native.Windows;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class ContactFixture : WatinFixture2
	{
		[Test]
		public void Payer_comment_contact_test()
		{
			var contactGroupOwner = new ContactGroupOwner();
			session.Save(contactGroupOwner);
			var contactGroup = new ContactGroup(ContactGroupType.Reports, "testGroup") { ContactGroupOwner = contactGroupOwner };
			session.Save(contactGroup);
			var contact = new Contact(ContactType.Email, "test@test.net") { ContactOwner = contactGroup };
			contactGroup.Contacts.Add(contact);
			var payer = new Payer("testPayer");
			session.Save(payer);
			var contactPayer = new Contact(ContactType.Email, "ContactPayer@analit.net") { ContactOwner = contactGroup };
			contactGroup.Contacts.Add(contactPayer);
			session.Save(contactGroup);
			session.Save(payer);
			var payerOwner = new PayerOwnerContact { Payer = payer, Contact = contactPayer };
			session.Save(payerOwner);
			Close();
			Open(string.Format("Contact/EditContactGroup.rails?contactGroupId={0}", contactGroup.Id));
			Assert.That(browser.Html, Is.StringContaining("ContactPayer@analit.net"));
			AssertText("testPayer");
		}
	}
}
