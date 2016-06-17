using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Common.Web.Ui.Models;
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
		public void Sunday_date()
		{
			var settings = new CostDynamicSettings(1, "");
			settings.Date = new DateTime(2011, 12, 25);
			Assert.That(settings.PrevWeek, Is.EqualTo(new DateTime(2011, 12, 19)));
		}

		[Test]
		public void Format()
		{
			var settings = new CostDynamicSettings(1, "отчет") {
				Regions = new ulong[] { 1 }
			};
			settings.Date = DateTime.Today;

			var report = new CostDynamic();
			var file = "CostDynamic.xls";
			var writer = new CostDynamicWriter();

			var results = report.CreateResultTable(settings.Dates);
			var row = results.NewRow();
			row["Name"] = "Протек";
			row["SomeDateCostIndex"] = 1.45;
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