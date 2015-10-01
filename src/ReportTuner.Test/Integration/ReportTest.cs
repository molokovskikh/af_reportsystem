using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.MonoRail.TestSupport;
using NHibernate.Linq;
using NUnit.Framework;
using ReportTuner.Controllers;
using ReportTuner.Helpers;
using Test.Support;
using ReportTuner.Models;
using System;
using System.Data;
using Common.MySql;
using Common.Web.Ui.Test.Controllers;
using MySql.Data.MySqlClient;
using NHibernate;
using Test.Support.Suppliers;
using Test.Support.log4net;

namespace ReportTuner.Test.Integration
{
	[TestFixture]
	internal class ReportTest : ControllerFixture
	{
		private DataTable FillClients(string proc, string filter, string id)
		{
			return ((MySqlConnection)session.Connection).Fill($"call Reports.{proc}(?, ?)", new {
				inFilter = filter,
				inID = id == String.Empty ? null : id,
			});
		}

		[Test]
		public void TestRecipientsList()
		{
			var payer = new TestPayer();
			session.Save(payer);

			var contactGroupOwner = new TestContactGroupOwner();
			contactGroupOwner.SaveAndFlush();

			var client1 = TestClient.CreateNaked(session);
			var client2 = TestClient.CreateNaked(session);

			client1.Payers.Add(payer);
			client2.Payers.Add(payer);

			session.CreateSQLQuery(@"INSERT INTO Billing.PayerClients(ClientId, PayerId) VALUES(:clientid1, :payerid);
										INSERT INTO Billing.PayerClients(ClientId, PayerId) VALUES(:clientid2, :payerid);")
				.SetParameter("clientid1", client1.Id).SetParameter("clientid2", client2.Id).SetParameter("payerid", payer.Id).ExecuteUpdate();

			var repPayer = Payer.Find(payer.Id);

			var new_report = new GeneralReport() { Format = "Excel", Payer = repPayer, Comment = "Тестовый отчет" };
			new_report.SaveAndFlush();
			var reportId = new_report.Id;
			var report = GeneralReport.Find(Convert.ToUInt64(reportId));
			Assert.That(report.Payer.AllClients.Count, Is.EqualTo(2));
			Assert.That(report.Payer.Clients[0].ShortName, Is.EqualTo(client1.Name));
			Assert.That(report.Payer.Clients[1].ShortName, Is.EqualTo(client2.Name));
		}

		[Test]
		public void TestClientsListInCombineReport()
		{
			var dt = DateTime.Now.ToString();

			var payer = new TestPayer();
			session.Save(payer);

			var contactGroupOwner = new TestContactGroupOwner();
			contactGroupOwner.SaveAndFlush();

			var supplier = new TestSupplier {
				Disabled = false,
				Type = ServiceType.Drugstore,
				Name = "тестовый поставщик" + dt,
				FullName = "тестовый поставщик" + dt,
				Payer = payer,
				ContactGroupOwner = contactGroupOwner
			};
			supplier.SaveAndFlush();

			var client = new TestClient {
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

			var result1 = FillClients("GetClientCodeWithNewUsers", "", client.Id.ToString());
			var result2 = FillClients("GetClientCodeWithNewUsers", "", supplier.Id.ToString());

			var row = result1.Rows[0];

			var id = Convert.ToUInt32(row[0]);
			var name = Convert.ToString(row[1]);

			Assert.That(result1.Rows.Count, Is.EqualTo(1));
			Assert.That(result2.Rows.Count, Is.EqualTo(0));
			Assert.That(id, Is.EqualTo(client.Id));
			Assert.That(name, Is.EqualTo(client.Name));
		}

		[Test]
		public void Region_mask_for_PharmacyMixedReport()
		{
			var reports = Report.Queryable.Where(r => r.ReportType.ReportClassName.Contains("PharmacyMixedReport") && r.Enabled).ToList();
			var report = reports.Select(r => {
				var properties = ReportProperty.Queryable.Where(p => p.Report == r).ToList();
				var prop = properties.FirstOrDefault(p => p.PropertyType.PropertyName == "RegionEqual");
				if (prop != null)
					return r;
				return null;
			}).FirstOrDefault(r => r != null);
			var reportProperties = report.Properties;
			var clientProperty = reportProperties.FirstOrDefault(p => p.PropertyType.PropertyName == "SourceFirmCode");
			var regionProperty = reportProperties.FirstOrDefault(p => p.PropertyType.PropertyName == "RegionEqual");
			var clientid = Convert.ToUInt32(clientProperty.Value);
			var client = Client.TryFind(clientid);
			if (client == null) {
				var tc = TestClient.Create();
				clientProperty.Value = tc.Id.ToString();
				clientProperty.Save();
				client = Client.TryFind(tc.Id);
			}
			var clientMask = client.MaskRegion;
			var regMask = regionProperty.Values.Where(v => {
				var reg = Convert.ToUInt32(v.Value);
				if ((reg & clientMask) > 0)
					return false;
				return true;
			}).Sum(v => Convert.ToUInt32(v.Value));
			var mask = clientMask + regMask;

			var dtNonOptionalParams = new DataTable();
			dtNonOptionalParams.Columns.AddRange(new[] {
				new DataColumn() { ColumnName = "PID", DataType = typeof(long) },
				new DataColumn() { ColumnName = "PPropertyName", DataType = typeof(string) },
				new DataColumn() { ColumnName = "PPropertyValue", DataType = typeof(string) }
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

		[Test]
		public void test_userId_SpecReport()
		{
			var reports = Report.Queryable.Where(r => r.ReportType.ReportClassName.Contains("SpecReport") && r.Enabled).ToList();
			var report = reports.Select(r => {
				var properties = r.Properties;
				var clientCode = properties.FirstOrDefault(p => p.PropertyType.PropertyName == "ClientCode");
				var clientProp = Client.TryFind(Convert.ToUInt32(clientCode.Value));
				var prop = properties.FirstOrDefault(p => p.PropertyType.PropertyName == "FirmCodeEqual");
				if (prop != null && clientProp != null)
					return r;
				return null;
			}).FirstOrDefault(r => r != null);
			var reportProperties = report.Properties;
			var clientProperty = reportProperties.FirstOrDefault(p => p.PropertyType.PropertyName == "ClientCode");
			var firmCodeProperty = reportProperties.FirstOrDefault(p => p.PropertyType.PropertyName == "FirmCodeEqual");
			var clientid = Convert.ToUInt32(clientProperty.Value);
			var client = Client.TryFind(clientid);
			var user = client.Users.FirstOrDefault();

			var dtNonOptionalParams = new DataTable();
			dtNonOptionalParams.Columns.AddRange(new[] {
				new DataColumn() { ColumnName = "PID", DataType = typeof(long) },
				new DataColumn() { ColumnName = "PPropertyName", DataType = typeof(string) },
				new DataColumn() { ColumnName = "PPropertyValue", DataType = typeof(string) }
			});
			DataRow dr = dtNonOptionalParams.NewRow();
			dr["PID"] = clientProperty.Id;
			dr["PPropertyName"] = "ClientCode";
			dr["PPropertyValue"] = client.Id;
			dtNonOptionalParams.Rows.Add(dr);

			var dtOptionalParams = new DataTable();
			dtOptionalParams.Columns.AddRange(new[] {
				new DataColumn() { ColumnName = "OPID", DataType = typeof(long) },
				new DataColumn() { ColumnName = "OPPropertyName", DataType = typeof(string) },
				new DataColumn() { ColumnName = "OPPropertyValue", DataType = typeof(string) }
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

		[Test]
		public void AddressTest()
		{
			var controller = new ReportsTuningController();
			Prepare(controller);
			controller.DbSession = session;
			var reportType = session.Query<ReportType>().First(rt => rt.ReportTypeFilePrefix == "Mixed");
			var report = session.Query<Report>().First(r => r.ReportType == reportType);
			var propertyType = session.Query<ReportTypeProperty>().First(rpt => rpt.ReportType == reportType && rpt.PropertyName == "AddressesEqual");
			var reportProperty = new ReportProperty {
				Value = "1",
				Report = report,
				PropertyType = propertyType
			};
			reportProperty.Save();
			var value = new ReportPropertyValue {
				ReportPropertyId = reportProperty.Id,
				Value = "0"
			};
			value.Save();
			reportProperty.Values = new List<ReportPropertyValue> { value };
			reportProperty.Save();
			var filter = new AddressesFilter {
				Report = report.Id,
				GeneralReport = 1u,
				ReportPropertyValue = reportProperty.Id
			};
			controller.SelectAddresses(filter);
			Assert.IsNotNull(controller.PropertyBag["addresses"]);
		}
	}
}