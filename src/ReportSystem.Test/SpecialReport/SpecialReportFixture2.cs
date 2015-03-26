﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support.Suppliers;

namespace ReportSystem.Test.SpecialReport
{
	class SpecialReportFixture2 : BaseProfileFixture2
	{
		[Test(Description = "Тестирует, что в случае, если подотчет не поддерживает dbf, выполнения не происходит")]
		public void DbfFormatFailTest()
		{
			//На самом деле тест совершенно никчемный, так как тут необходимо тестировать отправку почты,
			//а текущая интеграционная среда имитирует реальную, чуть более чем никак.
			var dateTime = DateTime.Today.AddDays(-2);
			var supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore();
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
			report = new SpecReport(1, fileName, (MySqlConnection)session.Connection, ReportFormats.DBF, properties);
			report.Interval = true;
			report.From = dateTime;
			// На DBF должен быть эксепшен
			try {
				BuildReport(fileName);
				Assert.Fail("Тут должно было возникнуть исключение, так как спец. отчет не готовится в dbf");
			}
			catch (ReportException e) {
				Assert.That(e.Message, Is.StringContaining("не может готовиться в формате DBF"));
			}

			// Ну а теперь протестим обычкновенный вариант, который должен работать
			report = new SpecReport(1, fileName, (MySqlConnection)session.Connection, ReportFormats.Excel, properties);
			report.Interval = true;
			report.From = dateTime;
			BuildReport(fileName);
		}
	}
}