using System;
using System.Collections.Generic;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test
{
	[TestFixture]
	public class PharmacyMixedProfileFixture : BaseProfileFixture
	{
		[Test]
		public void PharmacyMixedName()
		{
			var type = ReportsTypes.PharmacyMixedName;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void PharmacyMixedFullName()
		{
			var type = ReportsTypes.PharmacyMixedFullName;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void PharmacyMixedNameProducer()
		{
			var type = ReportsTypes.PharmacyMixedNameProducer;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void PharmacyMixedNameProducerSupplierList()
		{
			var type = ReportsTypes.PharmacyMixedNameProducerSupplierList;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void PharmacyMixedNameOld()
		{
			var type = ReportsTypes.PharmacyMixedNameOld;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void Filter_concurent_by_address()
		{
			Property("ByPreviousMonth", false);
			Property("SourceFirmCode", 3110);
			Property("BusinessRivals", new List<ulong> { 465, 10415 });
			Property("AddressRivals", new List<ulong> { 465, 11279 });
			Property("ClientCodeEqual", new List<ulong> { 3110, 465, 11279 });
			Property("ProductNamePosition", 0);
			var file = "Filter_concurent_by_address.xls";
			report = new PharmacyMixedReport(1, file, Conn, ReportFormats.Excel, properties);
			BuildOrderReport(file);
		}
	}
}