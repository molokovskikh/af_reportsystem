using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SuppliersRatinglProfileFixture : BaseProfileFixture
	{
		[Test]
		public void SuppliersRating()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SuppliersRating);
			var report = new ProviderRatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SuppliersRating);
		}

		[Test]
		public void SuppliersRatingNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SuppliersRatingNew);
			var report = new ProviderRatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SuppliersRatingNew);
		}

		[Test]
		public void SuppliersRatingNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SuppliersRatingNewDifficult);
			var report = new ProviderRatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SuppliersRatingNewDifficult);
		}

		[Test]
		public void SuppliersRatingNewWithClientCodeNonEqual()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SuppliersRatingNewWithClientCodeNonEqual);
			var report = new ProviderRatingReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SuppliersRatingNewWithClientCodeNonEqual);
		}
	}
}
