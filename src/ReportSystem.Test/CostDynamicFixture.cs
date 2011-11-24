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
			Property("suppliers", new List<ulong> {5, 7, 14});
			Property("regions", new List<ulong> {1});
			report = new CostDynamic(1, "CostDynamic.xls", Conn, false, ReportFormats.Excel, properties);
			BuildReport("CostDynamic.xls");
		}

		[Test]
		public void Format()
		{
			var report = new CostDynamic();
			var file = "CostDynamic.xls";
			var writer = new CostDynamicWriter();

			var results = report.CreateResultTable();
			var row = results.NewRow();
			row["Name"] = "Протек";
			row["CostDiff"] = 1.45;
			results.Rows.Add(row);
			var data = new DataSet();
			data.Tables.Add(results);

			var settings = new CostDynamicSettings(1, "") {
				Regions = new ulong[] {1}
			};
			settings.Date = DateTime.Today;
			settings.Filters.Add(String.Format("Динамика уровня цен и доли рынка на {0}", settings.Date.ToShortDateString()));
			settings.Filters.Add(String.Format("Регион {0}", settings.Regions.Select(r => Region.Find(r).Name).Implode()));

			if (File.Exists(file))
				File.Delete(file);

			writer.WriteReportToFile(data, file, settings);
		}
	}
}