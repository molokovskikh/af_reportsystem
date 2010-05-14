using System.IO;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.FastReports;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture, Ignore("Что бы подебажить отчет")]
	public class Troubleshoot
	{
		[Test]
		public void shoot_it()
		{
			uint reportcode = 1036;
			using(var connection = new MySqlConnection("server=sql.analit.net;user=;password=; default command timeout=0;database=usersettings"))
			{
				connection.Open();
				var loader = new ReportPropertiesLoader();
				var prop = loader.LoadProperties(connection, reportcode);
				var report = new PharmacyOffersReport(reportcode, "test", connection, true, ReportFormats.Excel, prop);
				report.ProcessReport();
				report.ReportToFile(Path.GetFullPath("test.xls"));
			}
		}
	}
}