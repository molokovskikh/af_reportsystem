using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test
{
	[TestFixture]
	public class CombineNewProfilleFixture : BaseProfileFixture
	{
		[Test]
		public void CombineWithOutSuppliersList()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineNew);
			var report = new CombReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineNew);
		}

		[Test]
		public void CombineWithOutSuppliersListDbf()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineNew);
			var report = new CombReport(0, "Automate Created Report", Conn, false, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineNew);
		}

		[Test]
		public void CombineWithSuppliersList()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineNewWithSuppliers);
			var report = new CombReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineNewWithSuppliers);
		}

		[Test]
		public void CombineNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineNewDifficult);
			var report = new CombReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineNewDifficult);
		}
	}
}
