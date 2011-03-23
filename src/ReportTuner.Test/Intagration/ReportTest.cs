using System.Collections.Generic;
using Castle.ActiveRecord;
using NUnit.Framework;
using Test.Support;
using ReportTuner.Models;
using System;


namespace ReportTuner.Test.Intagration
{
	[TestFixture]
	class ReportTest
	{		
		[Test]
		public void TestRecipientsList()
		{
			TestPayer payer;
			TestOldClient supplier;
			TestClient client;
			ulong reportId;

			using (new SessionScope())
			{
				payer = new TestPayer() {};
				payer.SaveAndFlush();
			
				var contactGroupOwner = new TestContactGroupOwner();
				contactGroupOwner.SaveAndFlush();

				supplier = new TestOldClient()
				               	{
				               		Segment = Segment.Wholesale,
				               		Status = ClientStatus.On,
				               		Type = ClientType.Drugstore,
				               		ShortName = "тестовый поставщик",
				               		FullName = "тестовый поставщик",
				               		RegionCode = 1UL,
				               		MaskRegion = 1UL,
				               		Payer = payer,
				               		ContactGroupOwner = contactGroupOwner
				               	};
				supplier.SaveAndFlush();

				client = new TestClient()
				             	{
				             		Segment = Segment.Wholesale,
				             		Status = ClientStatus.On,
				             		Type = ClientType.Drugstore,
				             		Name = "тестовый клиент",
				             		FullName = "тестовый клиент",
				             		RegionCode = 1UL,
				             		MaskRegion = 1UL,
				             		Payer = payer,
				             		ContactGroupOwner = contactGroupOwner,
				             		Users = new List<TestUser>()
				             	};
				client.SaveAndFlush();

				var session = ActiveRecordMediator.GetSessionFactoryHolder().CreateSession(typeof(ActiveRecordBase));
				try
				{
					session.CreateSQLQuery(@"INSERT INTO Billing.PayerClients(ClientId, PayerId) VALUES(:clientid, :payerid)")
						.SetParameter("clientid", client.Id).SetParameter("payerid", payer.Id).ExecuteUpdate();						
				}
				finally
				{
					ActiveRecordMediator.GetSessionFactoryHolder().ReleaseSession(session);
				}

				var repPayer = new Payer();
				repPayer.Id = payer.Id;

				var new_report = new GeneralReport() {Format = "Excel", Payer = repPayer, Comment = "Тестовый отчет"};
				new_report.SaveAndFlush();
				reportId = new_report.Id;

			}

			using (new SessionScope())
			{
				var report = GeneralReport.Find(Convert.ToUInt64(reportId));				
				Assert.That(report.Payer.AllClients.Count, Is.EqualTo(2));
				Assert.That(report.Payer.Clients[0].ShortName, Is.EqualTo("тестовый поставщик"));
				Assert.That(report.Payer.FutureClients[0].ShortName, Is.EqualTo("тестовый клиент"));
			}
		}
	}
}
