using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using ReportSystem.Profiling;

namespace ReportSystem.Test
{
	[Ignore]
	[TestFixture]
	public class IndividualProfileFixture : BaseProfileFixture
	{
		[Test]		
		public void Individual()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Individual);
			var report = new CombToPlainReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Individual);
		}	
	}
}
