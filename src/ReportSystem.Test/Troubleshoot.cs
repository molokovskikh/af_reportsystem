using Inforoom.ReportSystem;
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
			using(var connection = new MySqlConnection("server=sql.analit.net;user=;password=; default command timeout=0;database=usersettings"))
			{
				connection.Open();
				var loader = new ReportPropertiesLoader();
				var prop = loader.LoadProperties(connection, 1026);
				var report = new LeakOffersReport(1026, "test", connection, true, ReportFormats.Excel, prop);
				report.ProcessReport();
				report.ReportToFile("test.xls");
			}
		}
	}
}