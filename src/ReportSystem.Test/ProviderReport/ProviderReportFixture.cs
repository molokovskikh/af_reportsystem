using System;
using System.Data;
using System.Linq;
using System.Diagnostics;
using ExecuteTemplate;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace ReportSystem.Test.ProviderReport
{

	public class SpecReportOldLoad : Inforoom.ReportSystem.SpecShortReport
	{
		public SpecReportOldLoad(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, temporary, format, dsProperties)
		{ 
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = "select * from future.Clients where Id = " + _clientCode;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			IsNewClient = reader.Read();
			reader.Close();

			GetOffers();
			var dataSet = MySqlHelper.ExecuteDataset(
				e.DataAdapter.SelectCommand.Connection,
				@"
select 
	* 
from
	Core
	inner join farm.Core0 c on c.Id = Core.Id
	inner join catalogs.Products p on p.Id = c.ProductId
	left join farm.Synonym s on s.SynonymCode = c.SynonymCode
	left join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c.SynonymFirmCrCode");
			Assert.That(dataSet.Tables.Count, Is.GreaterThan(0));
			Assert.That(dataSet.Tables[0].Rows.Count, Is.GreaterThan(0));
			Console.WriteLine("{0} Offers count: {1}", DateTime.Now, dataSet.Tables[0].Rows.Count);

			var res = (from r in dataSet.Tables[0].AsEnumerable()
			        group r by r["CatalogId"]);
			Console.WriteLine("{0} group by {1}", DateTime.Now, res.Count());

		}
	}

	public class SpecReportNewLoad : Inforoom.ReportSystem.SpecShortReport
	{
		public SpecReportNewLoad(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, temporary, format, dsProperties)
		{
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = "select * from future.Clients where Id = " + _clientCode;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			IsNewClient = reader.Read();
			reader.Close();

			//var offers = GetOffers(4);
			//Assert.That(offers.Count, Is.GreaterThan(0));
			//Console.WriteLine("{0} Offers count: {1}", DateTime.Now, offers.Count);
			//var group = offers.GroupBy(item => item.CatalogId);
			//Console.WriteLine("{0} group by {1}", DateTime.Now, group.Count());
		}
	}

	[TestFixture]
	public class ProviderReportFixture : BaseProfileFixture
	{
		[Test]
		public void CheckSpeedLoad()
		{
			// Create new stopwatch
			Stopwatch stopwatch = new Stopwatch();

			// Begin timing
			stopwatch.Start();
			try
			{
				CheckOldLoad();
			}
			finally
			{
				// Stop timing
				stopwatch.Stop();
				// Write result
				Console.WriteLine("Old Load time elapsed: {0}", stopwatch.Elapsed);
			}

			// Begin timing
			stopwatch.Reset();
			stopwatch.Start();
			try
			{
				CheckNewLoad();
			}
			finally
			{
				// Stop timing
				stopwatch.Stop();
				// Write result
				Console.WriteLine("New Load time elapsed: {0}", stopwatch.Elapsed);
			}
		}

		private void CheckNewLoad()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			var report = new SpecReportNewLoad(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);

			report.ReadReportParams();
			report.ProcessReport();
		}

		private void CheckOldLoad()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			var report = new SpecReportOldLoad(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);

			report.ReadReportParams();
			report.ProcessReport();
		}
	}
}