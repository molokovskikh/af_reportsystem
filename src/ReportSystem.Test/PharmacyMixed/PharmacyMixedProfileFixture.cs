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
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void PharmacyMixedFullName()
		{
			var type = ReportsTypes.PharmacyMixedFullName;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void PharmacyMixedNameProducer()
		{
			var type = ReportsTypes.PharmacyMixedNameProducer;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void PharmacyMixedNameProducerSupplierList()
		{
			var type = ReportsTypes.PharmacyMixedNameProducerSupplierList;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}

		[Test]
		public void PharmacyMixedNameOld()
		{
			var type = ReportsTypes.PharmacyMixedNameOld;
			var props = TestHelper.LoadProperties(type);
			var report = new PharmacyMixedReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, type);
		}
	}
}
