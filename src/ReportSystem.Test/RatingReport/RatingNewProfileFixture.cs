using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
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

		[Test, Description("Тестирует построение отчета по служебному прайс-листу - в итоговых данных должно быть пусто")]
		public void RatingWithLocalPrice()
		{
			ulong price = 216;
			var fileName = "RatingWithLocalPrice.xls";
			Property("JunkState", 0);
			Property("ReportInterval", 10);
			Property("ByPreviousMonth", false);
			Property("PriceCodeEqual", new List<ulong> {
				price
			});

			// Устанавливаем единственный прайс, по которому делаем отчет в служебный
			MySqlCommand cmd = new MySqlCommand(
				String.Format("update usersettings.pricesdata set IsLocal=1 where pricecode={0}", price), Conn);
			cmd.ExecuteNonQuery();
			report = new RatingReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);

			// возвращаем настроки прайса как было
			cmd = new MySqlCommand(
				String.Format("update usersettings.pricesdata set IsLocal=0 where pricecode={0}", price), Conn);
			cmd.ExecuteNonQuery();

			Assert.That(((RatingReport)report).ResultTable.Select("F1 is not null").Length, Is.EqualTo(0));
		}
	}
}