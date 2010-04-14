using System;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class CombineProfileFixture : BaseProfileFixture
	{
		[Test]
		public void Combine()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Combine);
			var report = new CombReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Combine);
		}

		[Test]
		public void CombineCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineCount);
			var report = new CombReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineCount);
		}

		[Test]
		public void CombineCountAndProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineCountAndProducer);
			var report = new CombReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineCountAndProducer);
		}

		[Test]
		public void CombineProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineProducer);
			var report = new CombReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineProducer);
		}

		[Test]
		public void Test()
		{
			using (var connection = new MySqlConnection("user=Kvasov;password=ghjgtkkth;host=sql.analit.net;database=usersettings"))
			{
				connection.Open();
				var prop = new ReportPropertiesLoader();
				var data = prop.LoadProperties(connection, Convert.ToUInt64(944));
				var report = new ContactsReport(944, "Automate Created Report", connection, false, ReportFormats.Excel, data);
				report.ProcessReport();
				report.ReportToFile(@"C:\Temp\Combine.xls");
			}
		}
	}
}
