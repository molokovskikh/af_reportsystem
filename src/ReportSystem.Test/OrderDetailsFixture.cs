﻿using System;
using System.IO;
using Common.Tools.Helpers;
using Inforoom.ReportSystem.Models.Reports;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support.log4net;

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
}