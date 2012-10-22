using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test
{
	[TestFixture]
	public class RatingNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void RatingNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingNew);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingNew);
		}

		[Test]
		public void RatingNewWithPayerList()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.RatingWithPayersList);
			var report = new RatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.RatingWithPayersList);
		}

		[Test]
		public void RatingWithoutGroup()
		{
			var fileName = "RatingWithoutGroup.xls";
			Property("JunkState", 0);
			Property("ReportInterval", 10);
			Property("ByPreviousMonth", false);
			Property("PayerEqual", new List<ulong> {
				3450,
				3733,
				3677
			});

			report = new RatingReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}
	}
}