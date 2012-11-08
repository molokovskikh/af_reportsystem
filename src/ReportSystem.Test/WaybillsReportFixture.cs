﻿using System;
using System.Collections.Generic;
using System.IO;
using Castle.ActiveRecord;
using Common.Web.Ui.ActiveRecordExtentions;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOffers;
using Inforoom.ReportSystem.ByOrders;
using Inforoom.ReportSystem.Model;
using NHibernate;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class WaybillsReportFixture : BaseProfileFixture
	{
		[Test]
		public void Build()
		{
			int orgId = 0;
			Init(session => {
				var client = TestClient.CreateNaked();
				var address = client.Addresses[0];
				var supplier = TestSupplier.CreateNaked();

				orgId = (int)address.LegalEntity.Id;
				var waybill = new TestWaybill(new TestDocumentLog(supplier, address));
				waybill.Lines.Add(new TestWaybillLine(waybill) {
					Product = "Аксетин",
					Quantity = 10,
					SerialNumber = "4563",
					EAN13 = "5290931004832",
					ProducerCost = 56,
					SupplierCostWithoutNDS = 70,
				});
				waybill.Lines.Add(new TestWaybillLine(waybill) {
					Product = "Аксетин",
					Quantity = 10,
					SerialNumber = "4563",
					EAN13 = "5290931004832",
					ProducerCost = 56,
					SupplierCostWithoutNDS = 100,
				});
				session.Save(waybill);
				session.CreateSQLQuery("update Customers.Suppliers set VendorId = 15 where id = :id")
					.SetParameter("id", supplier.Id)
					.ExecuteUpdate();
			});

			Property("ByPreviousMonth", false);
			Property("OrgId", orgId);
			report = new WaybillsReport(1, "test", Conn, ReportFormats.CSV, properties);
			BuildOrderReport("test");
			var result = File.ReadAllText("test.csv");
			Assert.That(result, Is.EqualTo("DrugID;Segment;Year;Month;Series;TotDrugQn;MnfPrice;PrcPrice;RtlPrice;Funds;VendorID;Remark;SrcOrg\r\n34413;1;2012;11;\"4563\";10.00;56.00;70.00;79.24;0.00;15;;\r\n"));
		}

		[Test]
		public void Calculate_retail_cost()
		{
			var cost = Markup.CalculateRetailCost(50, 40, 10, 15);
			Assert.That(cost, Is.EqualTo(56.6));
		}

		[Test]
		public void Max_cost()
		{
			var markups = new[] { new Markup(MarkupType.Supplier, 20), new Markup(MarkupType.Drugstore, 20) };
			Assert.That(Markup.MaxCost(50, 10, markups), Is.EqualTo(71));
		}

		[Test]
		public void Correct_retails_markup()
		{
			var markups = new[] { new Markup(MarkupType.Supplier, 20), new Markup(MarkupType.Drugstore, 20) };
			Assert.That(Markup.RetailCost(70, 50, 10, markups), Is.EqualTo(0));
			Assert.That(Markup.RetailCost(65, 50, 10, markups), Is.EqualTo(71));
		}

		private void Init(Action<ISession> action)
		{
			using (new SessionScope()) {
				ArHelper.WithSession(action);
			}
		}
	}
}