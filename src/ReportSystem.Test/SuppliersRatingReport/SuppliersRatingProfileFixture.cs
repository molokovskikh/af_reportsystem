using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using ReportSystem.Profiling;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SuppliersRatinglProfileFixture : BaseProfileFixture
	{
		[Test]
		public void SuppliersRating()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SuppliersRating);
			var report = new ProviderRatingReport(0, "Automate Created Report", Conn, false, props);
			TestHelper.ProcessReport(report, ReportsTypes.SuppliersRating);
		}	
	}
}
