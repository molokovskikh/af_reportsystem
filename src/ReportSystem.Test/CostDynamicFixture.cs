using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOffers;
using Inforoom.ReportSystem.Model;
using Inforoom.ReportSystem.Writers;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Common.Tools;

namespace ReportSystem.Test
{
	[TestFixture]
	public class CostDynamicFixture : BaseProfileFixture
	{
		[Test]
		public void Builder_report()
		{
			var date = DateTime.Today.AddDays(-7);
			var someDate = date.AddDays(-7);
			Property("date", date);
			Property("someDate", someDate);
			Property("suppliers", new List<ulong> {5, 7, 14});
			Property("regions", new List<ulong> {1});
			report = new CostDynamic(1, "CostDynamic.xls", Conn, false, ReportFormats.Excel, properties);
			BuildReport("CostDynamic.xls");
		}

		[Test]
		public void Builder_report_for_all()
		{
			Property("date", new DateTime(2011, 11, 26));
			Property("someDate", new DateTime(2011, 11, 25));
			Property("suppliers", new List<ulong>());
			Property("regions", new List<ulong>());
			report = new CostDynamic(1, "CostDynamic.xls", Conn, false, ReportFormats.Excel, properties);
			BuildReport("CostDynamic.xls");
		}

		[Test]
		public void Settings_fixture()
		{
			var settings = new CostDynamicSettings(1, "");
			settings.Date = new DateTime(2011, 12, 20);
			Assert.That(settings.PrevWeek, Is.EqualTo(new DateTime(2011, 12, 19)));
			Assert.That(settings.PrevMonth, Is.EqualTo(new DateTime(2011, 12, 1)));

			settings.Date = new DateTime(2011, 12, 19);
			Assert.That(settings.PrevWeek, Is.EqualTo(new DateTime(2011, 12, 12)));

			settings.Date = new DateTime(2011, 12, 1);
			Assert.That(settings.PrevMonth, Is.EqualTo(new DateTime(2011, 11, 1)));
		}

		[Test]
		public void Format()
		{
			var settings = new CostDynamicSettings(1, "отчет") {
				Regions = new ulong[] {1}
			};
			settings.Date = DateTime.Today;

			var report = new CostDynamic();
			var file = "CostDynamic.xls";
			var writer = new CostDynamicWriter();

			var results = report.CreateResultTable(settings.Dates);
			var row = results.NewRow();
			row["Name"] = "Протек";
			row["CostDiff"] = 1.45;
			results.Rows.Add(row);
			var data = new DataSet();
			data.Tables.Add(results);

			settings.Filters.Add(String.Format("Динамика уровня цен и доли рынка на {0}", settings.Date.ToShortDateString()));
			settings.Filters.Add(String.Format("Регион {0}", settings.Regions.Select(r => Region.Find(r).Name).Implode()));

			if (File.Exists(file))
				File.Delete(file);

			writer.WriteReportToFile(data, file, settings);
		}
	}
}