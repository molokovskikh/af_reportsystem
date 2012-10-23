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
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineNew);
		}

		[Test]
		public void CombineWithOutSuppliersListDbf()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineNew);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.DBF, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineNew);
		}

		[Test]
		public void CombineWithSuppliersList()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineNewWithSuppliers);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineNewWithSuppliers);
		}

		[Test]
		public void CombineNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineNewDifficult);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineNewDifficult);
		}

		[Test]
		public void CombineByBaseCostNew()
		{
			var fileName = "CombineByBaseCostNew.xls";
			Property("ReportType", 4);
			Property("RegionEqual", new List<ulong> {
				70368744177664
			});

			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", false);
			Property("CalculateByCatalog", false);
			Property("ByBaseCosts", true);
			report = new CombReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}