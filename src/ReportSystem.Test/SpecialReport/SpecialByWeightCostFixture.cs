using System;
using System.Collections.Generic;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support.Suppliers;

namespace ReportSystem.Test.SpecialReport
{
	public class SpecialByWeightCostFixture : ReportFixture
	{
		[Test]
		public void Build_data_for_interval()
		{
			var dateTime = DateTime.Today.AddDays(-2);
			var supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore(session);

			var offer = supplier.Prices[0].Core[0];
			var product = offer.Product;
			session.CreateSQLQuery("insert into Reports.AverageCosts(Date, SupplierId, RegionId, ProductId, ProducerId, Cost, Quantity) values (:date, :supplierId, :regionId, :productId, :producerId, 100, 1);")
				.SetParameter("supplierId", supplier.Id)
				.SetParameter("regionId", supplier.HomeRegion.Id)
				.SetParameter("date", dateTime)
				.SetParameter("productId", product.Id)
				.SetParameter("producerId", offer.Producer.Id)
				.ExecuteUpdate();

			var fileName = "temp.xls";
			Property("ReportType", 2);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 0);
			Property("ReportIsFull", false);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", (int)supplier.Prices[0].Id);
			Property("ByWeightCosts", true);
			report = new SpecReport((MySqlConnection)session.Connection, properties);
			report.Interval = true;
			report.From = dateTime;
			BuildReport(fileName);

			var book = Load(fileName);
			var sheet = book.GetSheetAt(0);
			Assert.That(sheet.GetRow(0).GetCell(0).StringCellValue,
				Does.Contain($"Специальный отчет по взвешенным ценам по данным на {dateTime.ToShortDateString()}"), ToText(sheet));
			Assert.That(sheet.GetRow(3).GetCell(1).StringCellValue, Does.Match(offer.ProductSynonym.Name));
		}
	}
}
