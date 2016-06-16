using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;
using ExcelLibrary.SpreadSheet;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test.SpecialReport
{
	public class SpecialByWeightCostFixture : BaseProfileFixture2
	{
		[Test, Ignore("Требуется тестовая база данных")]
		public void SpecialCountProducerByWeightCost()
		{
			var fileName = "SpecialCountProducerByWeightCost.xls";
			Property("ReportType", 2);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ReportIsFull", false);
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 5);
			Property("ByWeightCosts", true);
			report = new SpecReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

		[Test, Ignore("Требуется тестовая база данных")]
		public void SpecialCountProducerByWeightCostAssort()
		{
			var fileName = "SpecialCountProducerByWeightCostAssort.xls";
			Property("ReportType", 2);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ReportIsFull", false);
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 5699);
			Property("ByWeightCosts", true);
			report = new SpecReport(1, fileName, Conn, ReportFormats.Excel, properties);
			BuildReport(fileName);
		}

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
			report = new SpecReport(1, fileName, (MySqlConnection)session.Connection, ReportFormats.Excel, properties);
			report.Interval = true;
			report.From = dateTime;
			BuildReport(fileName);

			var book = Load(fileName);
			var sheet = book.GetSheetAt(0);
			Assert.That(sheet.GetRow(0).GetCell(0).StringCellValue,
				Does.Contain($"Специальный отчет по взвешенным ценам по данным на {dateTime.ToShortDateString()}"));
			Assert.That(sheet.GetRow(3).GetCell(1).StringCellValue, Does.Match(offer.ProductSynonym.Name));
		}
	}
}
