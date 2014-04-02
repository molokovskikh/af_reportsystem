﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.ReportSystem;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;
using Test.Support.Suppliers;

namespace ReportSystem.Test.DefectureReport
{
	public class DefectureByWeightCostsFixture : BaseProfileFixture2
	{
		[Test, Ignore("Готовит пустой набор данных")]
		public void DefectureByWeight()
		{
			var fileName = "DefectureByWeightCost.xls";
			Property("ReportType", 5);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 7160);
			Property("PriceCode", 196);
			Property("ByWeightCosts", true);
			report = new DefReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test]
		public void Ignore_unknown_producers()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var product = session.Query<TestProduct>().First(p => p.CatalogProduct.Pharmacie);
			var core1 = supplier.AddFullCore(session, product);
			var core2 = supplier.AddFullCore(session, product, session.Query<TestProducer>().First());
			core2.Code = Generator.Random().First().ToString();

			var client = TestClient.CreateNaked(session);
			session.CreateSQLQuery("delete from Customers.UserPrices where PriceId <> :priceId and UserId = :userId")
				.SetParameter("priceId", supplier.Prices[0].Id)
				.SetParameter("userId", client.Users[0].Id)
				.ExecuteUpdate();

			Property("ReportType", (int)DefReportType.ByNameAndFormAndFirmCr);
			Property("RegionEqual", new List<ulong> {
				client.RegionCode
			});
			Property("ClientCode", client.Id);
			Property("UserCode", client.Users[0].Id);
			Property("PriceCode", supplier.Prices[0].Id);

			ProcessReport(typeof(DefReport));
			var data = report.GetReportTable();

			Assert.AreEqual(1, data.Rows.Count, String.Format("клиент {0} поставщик {1}", client.Id, supplier.Id));
			Assert.AreEqual(core2.Code, data.Rows[0]["Code"]);
		}

		[Test]
		public void Build_excel_report()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var product = session.Query<TestProduct>().First(p => p.CatalogProduct.Pharmacie);
			var core1 = supplier.AddFullCore(session, product);
			var core2 = supplier.AddFullCore(session, product, session.Query<TestProducer>().First());
			core2.Code = Generator.Random().First().ToString();

			var client = TestClient.CreateNaked(session);
			session.CreateSQLQuery("delete from Customers.UserPrices where PriceId <> :priceId and UserId = :userId")
				.SetParameter("priceId", supplier.Prices[0].Id)
				.SetParameter("userId", client.Users[0].Id)
				.ExecuteUpdate();

			Property("ReportType", (int)DefReportType.ByNameAndFormAndFirmCr);
			Property("RegionEqual", new List<ulong> {
				client.RegionCode
			});
			Property("ClientCode", client.Id);
			Property("UserCode", client.Users[0].Id);
			Property("PriceCode", supplier.Prices[0].Id);

			var report = ReadReport<DefReport>();
			var result = ToText(report);
			Assert.That(result, Is.StringContaining("|Код|Наименование|Форма выпуска|Производитель|"));
		}
	}
}
