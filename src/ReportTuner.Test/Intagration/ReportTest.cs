using System.Collections.Generic;
using Castle.ActiveRecord;
using NUnit.Framework;
using Test.Support;
using ReportTuner.Models;
using System;
using System.Data;
using System.Configuration;
using MySql.Data.MySqlClient;
using Test.Support.Suppliers;


namespace ReportTuner.Test.Intagration
{
	[TestFixture]
	class ReportTest
	{
		private MySqlConnection MyCn;
		private MySqlCommand MyCmd;
		private MySqlDataAdapter MyDA;

		[SetUp]
		public void Setup()
		{
			MyCn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
			MyCmd = new MySqlCommand();
			MyDA = new MySqlDataAdapter();
		}

		DataTable FillClients(string proc, string filter, string id)
		{
			DataTable dtProcResult = new DataTable();
			string db = String.Empty;
			try
			{
				if (MyCn.State != ConnectionState.Open)
					MyCn.Open();				
				db = MyCn.Database;
				MyCn.ChangeDatabase("reports");
				MyCmd.Connection = MyCn;
				MyDA.SelectCommand = MyCmd;
				MyCmd.Parameters.Clear();
				MyCmd.Parameters.AddWithValue("inFilter", filter);
				MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
				if (id == String.Empty)
					MyCmd.Parameters.AddWithValue("inID", DBNull.Value);
				else
					MyCmd.Parameters.AddWithValue("inID", Convert.ToInt64(id));
				MyCmd.Parameters["inID"].Direction = ParameterDirection.Input;
				MyCmd.CommandText = proc;
				MyCmd.CommandType = CommandType.StoredProcedure;
				MyDA.Fill(dtProcResult);
			}
			finally
			{
				if (db != String.Empty)
					MyCn.ChangeDatabase(db);
				MyCmd.CommandType = CommandType.Text;
				MyCn.Close();
			}
			return dtProcResult;
		}

		[Test]
		public void TestRecipientsList()
		{
			TestPayer payer;
			TestClient client1;
			TestClient client2;
			ulong reportId;

			var dt = DateTime.Now.ToString();

			using (new SessionScope())
			{
				payer = new TestPayer() {};
				payer.SaveAndFlush();
			
				var contactGroupOwner = new TestContactGroupOwner();
				contactGroupOwner.SaveAndFlush();

			    client1 = TestClient.Create();
			    client2 = TestClient.Create();

                client1.Payers.Add(payer);
                client2.Payers.Add(payer);

				var session = ActiveRecordMediator.GetSessionFactoryHolder().CreateSession(typeof(ActiveRecordBase));
				try
				{
                    session.CreateSQLQuery(@"INSERT INTO Billing.PayerClients(ClientId, PayerId) VALUES(:clientid1, :payerid);
                                             INSERT INTO Billing.PayerClients(ClientId, PayerId) VALUES(:clientid2, :payerid);")
                        .SetParameter("clientid1", client1.Id).SetParameter("clientid2", client2.Id).SetParameter("payerid", payer.Id).ExecuteUpdate();
				}
				finally
				{
					ActiveRecordMediator.GetSessionFactoryHolder().ReleaseSession(session);
				}

				var repPayer = Payer.Find(payer.Id);

				var new_report = new GeneralReport() {Format = "Excel", Payer = repPayer, Comment = "Тестовый отчет"};
				new_report.SaveAndFlush();
				reportId = new_report.Id;
			}
			using (new SessionScope())
			{
				var report = GeneralReport.Find(Convert.ToUInt64(reportId));				
				Assert.That(report.Payer.AllClients.Count, Is.EqualTo(2));				
                Assert.That(report.Payer.FutureClients[0].ShortName, Is.EqualTo(client1.Name));
				Assert.That(report.Payer.FutureClients[1].ShortName, Is.EqualTo(client2.Name));
			}
		}
		
		[Test]
		public void TestClientsListInCombineReport()
		{
			TestPayer payer;			
		    TestSupplier supplier;
			TestClient client;
			ulong reportId;
			var dt = DateTime.Now.ToString();

			using (new SessionScope())
			{
				payer = new TestPayer() {};
				payer.SaveAndFlush();

				var contactGroupOwner = new TestContactGroupOwner();
				contactGroupOwner.SaveAndFlush();

				supplier = new TestSupplier()
				           	{
				           		Segment = Segment.Wholesale,
				           		Disabled = false,
				           		Type = ServiceType.Drugstore,
				           		Name = "тестовый поставщик" + dt,
				           		FullName = "тестовый поставщик" + dt,
				           		Payer = payer,
				           		ContactGroupOwner = contactGroupOwner
				           	};
				supplier.SaveAndFlush();

				client = new TestClient()
				         	{
				         		Segment = Segment.Wholesale,
				         		Status = ClientStatus.On,
				         		Type = ServiceType.Drugstore,
				         		Name = "тестовый клиент" + dt,
				         		FullName = "тестовый клиент" + dt,
				         		RegionCode = 1UL,
				         		MaskRegion = 1UL,
				         		ContactGroupOwner = contactGroupOwner,
				         		Users = new List<TestUser>()
				         	};
				client.SaveAndFlush();
			}

			DataTable result1 = FillClients("GetClientCodeWithNewUsers", "", client.Id.ToString());
			DataTable result2 = FillClients("GetClientCodeWithNewUsers", "", supplier.Id.ToString());

			DataRow row = result1.Rows[0];

			uint id = Convert.ToUInt32(row[0]);						
			string name = Convert.ToString(row[1]);

			Assert.That(result1.Rows.Count, Is.EqualTo(1));
			Assert.That(result2.Rows.Count, Is.EqualTo(0));
			Assert.That(id, Is.EqualTo(client.Id));
			Assert.That(name,Is.EqualTo(client.Name));
		}
	}
}
