using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test.Contacts
{
	[TestFixture]
	public class ContactsProfileFixture : BaseProfileFixture
	{
		[Test]
		public void ContactsOld()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Combine);
			var report = new ContactsReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReportWithOutDeletion(report, ReportsTypes.Combine);
		}

		[Test]
		public void ContactsNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineNew);
			var report = new ContactsReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReportWithOutDeletion(report, ReportsTypes.CombineNew);
		}
	}
}