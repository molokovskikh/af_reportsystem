using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using NUnit.Framework;
using ReportTuner.Helpers;
using Test.Support;
using ReportTuner.Models;
using System;
using System.Data;
using MySql.Data.MySqlClient;
using Test.Support.Suppliers;

namespace ReportTuner.Test.Integration
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
			MyCn = new MySqlConnection(FixtureSetup.ConnectionString);
			MyCmd = new MySqlCommand();
			MyDA = new MySqlDataAdapter();
		}

		DataTable FillClients(string proc, string filter, string id)
		{
			var dtProcResult = new DataTable();
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
			TestClient client1;
			TestClient client2;
			ulong reportId;

			using (new SessionScope())
			{
				var payer = new TestPayer();
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
			TestSupplier supplier;
			TestClient client;
			var dt = DateTime.Now.ToString();

			using (new SessionScope())
			{
				var payer = new TestPayer();
				payer.SaveAndFlush();

				var contactGroupOwner = new TestContactGroupOwner();
				contactGroupOwner.SaveAndFlush();

				supplier = new TestSupplier {
					Disabled = false,
					Type = ServiceType.Drugstore,
					Name = "тестовый поставщик" + dt,
					FullName = "тестовый поставщик" + dt,
					Payer = payer,
					ContactGroupOwner = contactGroupOwner
				};
				supplier.SaveAndFlush();

				client = new TestClient {
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

			var result1 = FillClients("GetClientCodeWithNewUsers", "", client.Id.ToString());
			var result2 = FillClients("GetClientCodeWithNewUsers", "", supplier.Id.ToString());

			var row = result1.Rows[0];

			var id = Convert.ToUInt32(row[0]);						
			var name = Convert.ToString(row[1]);

			Assert.That(result1.Rows.Count, Is.EqualTo(1));
			Assert.That(result2.Rows.Count, Is.EqualTo(0));
			Assert.That(id, Is.EqualTo(client.Id));
			Assert.That(name,Is.EqualTo(client.Name));
		}

		public class FakeReports_GeneralReports : Reports_GeneralReports
		{
			public static IList<string> GetMailAddresses(string inStr)
			{
				return Reports_GeneralReports.GetMailAddresses(inStr);
			}
		}

		[Test]
		public void GetMailAddressesTest()
		{
			var mails = FakeReports_GeneralReports.GetMailAddresses("a.tyutin@analit.net, _test@mail.ru, 123_@qwe.ertert.net, .incorrect@mail.ru");
			Assert.That(mails.Count, Is.EqualTo(3));
		}

		[Test]
		public void Region_mask_for_PharmacyMixedReport()
		{
			using (new SessionScope())
			{				
				var reports = Report.Queryable.Where(r => r.ReportType.ReportClassName.Contains("PharmacyMixedReport") && r.Enabled).ToList();
				var report = reports.Select(r => {
					var properties = ReportProperty.Queryable.Where(p => p.Report == r).ToList();
					var prop = properties.FirstOrDefault(p => p.PropertyType.PropertyName == "RegionEqual");
					if (prop != null) return r;
					return null;
				}).FirstOrDefault(r => r != null);
				var reportProperties = report.Properties;
				var clientProperty = reportProperties.FirstOrDefault(p => p.PropertyType.PropertyName == "SourceFirmCode");
				var regionProperty = reportProperties.FirstOrDefault(p => p.PropertyType.PropertyName == "RegionEqual");
				var clientid = Convert.ToUInt32(clientProperty.Value);
				var client = FutureClient.TryFind(clientid);
				if(client == null)
				{
					var tc = TestClient.Create();
					clientProperty.Value = tc.Id.ToString();
					clientProperty.Save();
					client = FutureClient.TryFind(tc.Id);
				}
				var clientMask = client.MaskRegion;
				var regMask = regionProperty.Values.Where(v => {
					var reg = Convert.ToUInt32(v.Value);
					if ((reg & clientMask) > 0) return false;
					return true;
				}).Sum(v => Convert.ToUInt32(v.Value));
				var mask = clientMask + regMask;

				var dtNonOptionalParams = new DataTable();
				dtNonOptionalParams.Columns.AddRange(new[]
				{
					new DataColumn() {ColumnName = "PID", DataType = typeof (long)},
					new DataColumn() {ColumnName = "PPropertyName", DataType = typeof (string)},
					new DataColumn() {ColumnName = "PPropertyValue", DataType = typeof (string)}
				});
				DataRow dr = dtNonOptionalParams.NewRow();
				dr["PID"] = clientProperty.Id;
				dr["PPropertyName"] = "SourceFirmCode";
				dr["PPropertyValue"] = client.Id;
				dtNonOptionalParams.Rows.Add(dr);

				var propertyHelper = new PropertiesHelper(report.Id, dtNonOptionalParams, null);
				var res = propertyHelper.GetRelativeValue(regionProperty);

				Assert.That(res, Is.Not.Null);
				Assert.That(res.Length, Is.GreaterThan(0));
				Assert.That(res, Is.EqualTo(String.Format("inID={0}", mask)));
			}
		}

		[Test]
		public void test_userId_SpecReport()
		{
			using (new SessionScope())
			{
				var reports = Report.Queryable.Where(r => r.ReportType.ReportClassName.Contains("SpecReport") && r.Enabled).ToList();
				var report = reports.Select(r => {
					var properties = r.Properties;
					var prop = properties.FirstOrDefault(p => p.PropertyType.PropertyName == "FirmCodeEqual");
					if (prop != null) return r;
					return null;
				}).FirstOrDefault(r => r != null);
				var reportProperties = report.Properties;
				var clientProperty = reportProperties.FirstOrDefault(p => p.PropertyType.PropertyName == "ClientCode");
				var firmCodeProperty = reportProperties.FirstOrDefault(p => p.PropertyType.PropertyName == "FirmCodeEqual");
				var clientid = Convert.ToUInt32(clientProperty.Value);
				var client = FutureClient.TryFind(clientid);
				var user = client.Users.FirstOrDefault();

				var dtNonOptionalParams = new DataTable();
				dtNonOptionalParams.Columns.AddRange(new[]
				{
					new DataColumn() {ColumnName = "PID", DataType = typeof (long)},
					new DataColumn() {ColumnName = "PPropertyName", DataType = typeof (string)},
					new DataColumn() {ColumnName = "PPropertyValue", DataType = typeof (string)}
				});
				DataRow dr = dtNonOptionalParams.NewRow();
				dr["PID"] = clientProperty.Id;
				dr["PPropertyName"] = "ClientCode";
				dr["PPropertyValue"] = client.Id;
				dtNonOptionalParams.Rows.Add(dr);

				var dtOptionalParams = new DataTable();
				dtOptionalParams.Columns.AddRange(new[]
				{
					new DataColumn() {ColumnName = "OPID", DataType = typeof (long)},
					new DataColumn() {ColumnName = "OPPropertyName", DataType = typeof (string)},
					new DataColumn() {ColumnName = "OPPropertyValue", DataType = typeof (string)}
				});
				dr = dtOptionalParams.NewRow();
				dr["OPID"] = firmCodeProperty.Id;
				dr["OPPropertyName"] = "FirmCodeEqual";
				dr["OPPropertyValue"] = firmCodeProperty.Value;
				dtOptionalParams.Rows.Add(dr);

				var propertyHelper = new PropertiesHelper(report.Id, dtNonOptionalParams, dtOptionalParams);
				var res = propertyHelper.GetRelativeValue(firmCodeProperty);

				Assert.That(res, Is.Not.Null);
				Assert.That(res.Length, Is.GreaterThan(0));
				Assert.That(res, Is.EqualTo(String.Format("userId={0}", user.Id)));
			}
		}
	}
}
