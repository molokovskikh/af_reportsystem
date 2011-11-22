using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Diagnostics;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.Tools;
using ExecuteTemplate;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Model;
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
			/*e.DataAdapter.SelectCommand.CommandText = "select * from future.Clients where Id = " + _clientCode;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			IsNewClient = reader.Read();
			reader.Close();*/

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
			/*e.DataAdapter.SelectCommand.CommandText = "select * from future.Clients where Id = " + _clientCode;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			IsNewClient = reader.Read();
			reader.Close();*/

			//var offers = GetOffers(4);
			//Assert.That(offers.Count, Is.GreaterThan(0));
			//Console.WriteLine("{0} Offers count: {1}", DateTime.Now, offers.Count);
			//var group = offers.GroupBy(item => item.CatalogId);
			//Console.WriteLine("{0} group by {1}", DateTime.Now, group.Count());
		}
	}

	public class TestClientNamesBaseReport : Inforoom.ReportSystem.ProviderReport
	{
		public TestClientNamesBaseReport(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties) : base(reportCode, reportCaption, connection, temporary, format, dsProperties)
		{
		}

		public string PublicGetClientsNamesFromSQL(List<ulong> equalValues)
		{
			return GetClientsNamesFromSQL(equalValues);
		}

		public override void GenerateReport(ExecuteArgs e)
		{
		}
	}

	public class SpecShortReportFake : SpecShortReport
	{
		public List<SpecShortReportData> ReportData { get { return _reportData; } }

		//public SpecShortReportFake(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
//			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		public SpecShortReportFake()
		{
			_reportData = new List<SpecShortReportData>();
			_hash = new Hashtable();
		}

		public override void ReadReportParams()
		{
			_reportType = 3; // с учетом производителя и без кол-ва
			_showCodeCr = true;
			_codesWithoutProducer = true;
		}

		public override List<Offer> GetOffers(int clientId, uint sourcePriceCode, uint? noiseSupplierId, bool allAssortment, bool byCatalog, bool withProducers)
		{
			var result = new List<Offer>();
			result.Add(new Offer {ProductId = 1, ProducerId = 1, Cost = 10, AssortmentCoreId = 1, AssortmentCode = "4", AssortmentCodeCr = "1"});
			result.Add(new Offer {ProductId = 1, ProducerId = 1, Cost = 3, AssortmentCoreId = 1, AssortmentCode = "2", AssortmentCodeCr = "3"});
			result.Add(new Offer {ProductId = 1, ProducerId = 2, Cost = 1, AssortmentCoreId = 2, AssortmentCode = "5", AssortmentCodeCr = "1"});
			result.Add(new Offer {ProductId = 2, ProducerId = 2, Cost = 5, AssortmentCoreId = 3, AssortmentCode = "7", AssortmentCodeCr = "4"});
			result.Add(new Offer {ProductId = 3, ProducerId = 2, Cost = 8, AssortmentCoreId = 4, AssortmentCode = "15", AssortmentCodeCr = "4"});
			result.Add(new Offer {ProductId = 3, ProducerId = 8, Cost = 5, AssortmentCoreId = 5, AssortmentCode = "11", AssortmentCodeCr = "4"});

			result.Add(new Offer {ProductId = 1, ProducerId = 1, Cost = 5, AssortmentCode = null, AssortmentCodeCr = null});
			result.Add(new Offer {ProductId = 1, ProducerId = 1, Cost = 2, AssortmentCode = null, AssortmentCodeCr = null});
			result.Add(new Offer {ProductId = 1, ProducerId = 2, Cost = 5, AssortmentCode = null, AssortmentCodeCr = null});
			result.Add(new Offer {ProductId = 2, ProducerId = 6, Cost = 0, AssortmentCode = null, AssortmentCodeCr = null});
			result.Add(new Offer {ProductId = 3, ProducerId = 6, Cost = 0, AssortmentCode = null, AssortmentCodeCr = null});
			result.Add(new Offer {ProductId = 5, ProducerId = 6, Cost = 0, AssortmentCode = null, AssortmentCodeCr = null});

			return result;
		}

		public void GetOffersByClient(int clientId)
		{
			base.GetOffersByClient(clientId);
		}
	}

	[TestFixture]
	public class ProviderReportFixture : BaseProfileFixture
	{
		[Test]
		public void GetOffersByClientIfCodesWithoutProducerTest()
		{
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordStarter.Initialize( typeof(Client).Assembly, ActiveRecordSectionHandler.Instance);
			var report = new SpecShortReportFake();
			report.ReadReportParams();
			using(new SessionScope()) {
				var client = Client.Queryable.FirstOrDefault();
				report.GetOffersByClient((int) client.Id);
			}
			Assert.That(report.ReportData.Count, Is.EqualTo(10));
			
			Assert.That(report.ReportData[0].Code, Is.EqualTo("2"));
			Assert.That(report.ReportData[0].CodeWithoutProducer, Is.EqualTo("2"));
			Assert.That(report.ReportData[1].Code, Is.EqualTo("5"));
			Assert.That(report.ReportData[1].CodeWithoutProducer, Is.EqualTo("2"));
			Assert.That(report.ReportData[2].Code, Is.EqualTo("7"));
			Assert.That(report.ReportData[2].CodeWithoutProducer, Is.EqualTo("7"));
			Assert.That(report.ReportData[3].Code, Is.EqualTo("15"));
			Assert.That(report.ReportData[3].CodeWithoutProducer, Is.EqualTo("11"));
			Assert.That(report.ReportData[4].Code, Is.EqualTo("11"));
			Assert.That(report.ReportData[4].CodeWithoutProducer, Is.EqualTo("11"));

			Assert.That(report.ReportData[5].Code, Is.Null);
			Assert.That(report.ReportData[5].CodeWithoutProducer, Is.EqualTo("2"));
			Assert.That(report.ReportData[6].Code, Is.Null);
			Assert.That(report.ReportData[6].CodeWithoutProducer, Is.EqualTo("2"));
			Assert.That(report.ReportData[7].Code, Is.Null);
			Assert.That(report.ReportData[7].CodeWithoutProducer, Is.EqualTo("7"));
			Assert.That(report.ReportData[8].Code, Is.Null);
			Assert.That(report.ReportData[8].CodeWithoutProducer, Is.EqualTo("11"));
		}

		[Test, Ignore("Это временный тест для проверки скорости выборки предложений")]
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

		private DataSet GetClients(string sql, int rowCount)
		{
			var dsClients = MySqlHelper.ExecuteDataset(
				Conn,
				sql);
			Assert.That(dsClients.Tables.Count, Is.EqualTo(1), "Не выбрали клиентов, удовлетворяющих условию теста");
			Assert.That(dsClients.Tables[0].Rows.Count, Is.EqualTo(rowCount), "Не выбрали клиентов, удовлетворяющих условию теста");

			return dsClients;
		}

		private void CheckClientsName(DataTable clients)
		{
			var query =
					from client in clients.AsEnumerable()
					select new
					{
						Id = Convert.ToUInt64(client["Id"]),
						Name = client["Name"].ToString()
					};
			var list = query.OrderBy(c => c.Name);

			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			var report = new TestClientNamesBaseReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);

			report.ProcessReport();

			var names = report.PublicGetClientsNamesFromSQL(list.Select(c => c.Id).ToList());

			Assert.That(names, Is.EqualTo(list.Select(c => c.Name).Implode()));
		}

		[Test(Description = "Проверяем работу метода с новыми клиентами")]
		public void CheckClientNamesWithNewClients()
		{
			var dsClients = GetClients(
				@"
select 
	c.Id,
	c.Name 
from 
	future.Clients c
	left join usersettings.ClientsData cd on cd.FirmCode = c.Id and cd.FirmType = 1
where
  cd.FirmCode is null
limit 1"
				,
				1);

			CheckClientsName(dsClients.Tables[0]);
		}

		[Test(Description = "Проверяем работу метода со старыми клиентами")]
        [Ignore("Все клиенты перенесены в future.clients")]
		public void CheckClientNamesWithOldClients()
		{
			var dsClients = GetClients(
				@"
select 
	cd.FirmCode as Id,
	cd.ShortName as Name
from 
	usersettings.ClientsData cd
	left join future.Clients c on cd.FirmCode = c.Id
where
	cd.FirmType = 1
and c.Id is null
limit 1"
				,
				1);

			CheckClientsName(dsClients.Tables[0]);
		}

		[Test(Description = "Проверяем работу метода с новыми клиентами, для которых существуют старые клиенты с другим именем")]
		public void CheckClientNamesWithNewAndOldClients()
		{
			var dsClients = GetClients(
				@"
select 
	c.Id,
	c.Name 
from 
	future.Clients c
	left join usersettings.ClientsData cd on cd.FirmCode = c.Id and cd.FirmType = 1 and cd.ShortName <> c.Name
where
  cd.FirmCode is not null
limit 1"
				,
				1);

			CheckClientsName(dsClients.Tables[0]);
		}

		[Test(Description = "Проверяем работу метода с различными типами клиентов")]
        [Ignore("Все клиенты перенесены в future.clients")]
		public void CheckClientNamesWithDifferentClients()
		{
			var dsClients = GetClients(
				@"
select
*
from
(
select 
	c.Id,
	c.Name 
from 
	future.Clients c
	left join usersettings.ClientsData cd on cd.FirmCode = c.Id and cd.FirmType = 1 and cd.ShortName <> c.Name
where
  cd.FirmCode is not null
limit 1
) cl1
union
select
*
from
(
select 
	cd.FirmCode as Id,
	cd.ShortName as Name
from 
	usersettings.ClientsData cd
	left join future.Clients c on cd.FirmCode = c.Id
where
	cd.FirmType = 1
and c.Id is null
limit 1
) cl2
"
				,
				2);

			CheckClientsName(dsClients.Tables[0]);
		}

	}
}