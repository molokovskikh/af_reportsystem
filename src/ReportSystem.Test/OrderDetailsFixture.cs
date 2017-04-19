using System;
using System.IO;
using Common.Tools.Helpers;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Models.Reports;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support;
using Test.Support.log4net;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OrderDetailsFixture : ReportFixture
	{
		[Test]
		public void Export()
		{
			File.Delete("test.xls");
			var order = CreateOrder();
			session.Save(order);
			var report = new OrderDetails();
			report.ReportCaption = "тест";
			report.ClientId = order.Client.Id;
			report.Connection = (MySqlConnection)session.Connection;
			report.Session = session;
			report.Interval = true;
			report.From = DateTime.Today.AddDays(-1);
			report.To = DateTime.Today;
			this.report = report;
			var sheet = ReadReport();
			Assert.That(ToText(sheet), Does.Contain(order.Id.ToString()));
			sheet = sheet.Workbook.GetSheetAt(1);
			Assert.That(ToText(sheet), Does.Contain(order.Id.ToString()), "на второй странице должна быть детализация");
		}
	}

	[TestFixture]
	public class CombFixture : ReportFixture
	{
		[Test]
		public void Build()
		{
			var supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore(session);
			var client = TestClient.Create(session);

			var report = new CombReport((MySqlConnection)session.Connection, properties);
			report.ClientCode = (int)client.Id;
			report.Configured = true;
			var sheet = ReadReport(report);
			Assert.That(ToText(sheet), Does.Contain("Комбинированный отчет без учета производителя"));
		}
	}
}