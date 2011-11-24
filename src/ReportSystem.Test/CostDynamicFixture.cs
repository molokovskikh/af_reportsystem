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
		public void Export()
		{
			var results = new DataTable("Results");

/*			var adapter = new MySqlDataAdapter("", "Database=usersettings;Data Source=testsql.analit.net;User Id=system;Password=newpass;pooling=false; default command timeout=0;Allow user variables=true;convert zero datetime=yes;");
			adapter.SelectCommand.CommandText = String.Format(@"select Id, Name
from Future.Suppliers
where id in ({0})", new List<ulong> {5, 7, 14}.Implode());
			adapter.Fill(results);*/

			results.Columns.Add("t1");
			results.Rows.Add(results.NewRow());
			//results.Columns.Add("MarketShare", typeof (decimal));
			//results.Columns.Add("MarketShareDiff", typeof (decimal));
			//results.Columns.Add("CostDiff", typeof (int));

/*
			results.Columns.Add("PrevMonthMarketShareDiff", typeof (decimal));
			results.Columns.Add("PrevMonthCostDiff", typeof (decimal));

			results.Columns.Add("PrevWeekMarketShareDiff", typeof (decimal));
			results.Columns.Add("PrevWeekCostDiff", typeof (decimal));

			results.Columns.Add("PrevDayMarketShareDiff", typeof (decimal));
			results.Columns.Add("PrevDayCostDiff", typeof (decimal));
*/
			//results.Rows[0]["CostDiff"] = 1;
			results.Rows[0]["t1"] = "123";
			var writer = new BaseExcelWriter();
			var file = "test12213.xls";

			if (File.Exists(file))
				File.Delete(file);

			writer.DataTableToExcel(results, file, 1);
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

			results.Columns.Remove("Id");
			writer.WriteReportToFile(data, file, settings);
		}
	}
}