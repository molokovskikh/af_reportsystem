using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using NHibernate.Linq;
using NPOI.SS.UserModel;
using NUnit.Framework;
using Test.Support;
using Test.Support.Logs;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SupplierMarketShareByUserFixture : ReportFixture
	{
		private TestSupplier supplier;
		private TestOrder order;

		[SetUp]
		public void Setup()
		{
			order = CreateOrder();
			supplier = order.Price.Supplier;
			session.Save(order);
			Property("SupplierId", supplier.Id);
			Property("Begin", DateTime.Now.AddDays(-10));
			Property("End", DateTime.Now.AddDays(1));
			Property("Regions", new List<long> { (long)order.RegionCode });
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 12);
		}

		[Test]
		public void Build_report_with_ShareMoreThan()
		{
			// вторая аптека заказала у этого поставщика
			var order2 = CreateOrder(null, supplier);
			session.Save(order2);

			// вторая аптека заказала у другого поставщика в 199 раз больше - доля 0.5%
			var order3 = CreateOrder(order2.Client);
			order3.Items[0].Cost *= 199;
			session.Save(order3);

			Property("Type", 0);
			Property("ShareMoreThan", "0.5", "PERCENT");
			var rep = ReadReport<SupplierMarketShareByUser>();
			var rows = rep.Rows().ToArray();

			var firstClient = rows.First(r => r.GetCell(2) != null && r.GetCell(2).StringCellValue == order.Client.Users[0].Id.ToString());
			// доля поставщика в заказах первой аптеки 100%
			Assert.AreEqual(100, NullableConvert.ToDecimal(firstClient.GetCell(3).StringCellValue));

			// второй аптеки нет в отчете
			var secondClient = rows.FirstOrDefault(r => r.GetCell(2) != null && r.GetCell(2).StringCellValue == order2.Client.Users[0].Id.ToString());
			Assert.IsNull(secondClient);

			var result = ToText(rep);
			Assert.That(result, Does.Contain("Из отчета ИСКЛЮЧЕНЫ юр. лица, клиенты, адреса, по которым доля НЕ превышает 0,5%"));
		}

		[Test]
		public void Build_report()
		{
			Property("Type", 0);

			report = new SupplierMarketShareByUser(Conn, properties);
			BuildReport();
		}

		[Test]
		public void Build_report_by_address()
		{
			Property("Type", 1);

			report = new SupplierMarketShareByUser(Conn, properties);
			BuildReport("SupplierMarketShareByUserByAddress.xls");
		}

		[Test]
		public void Build_report_by_client()
		{
			Property("Type", 2);

			report = new SupplierMarketShareByUser(Conn, properties);
			BuildReport("SupplierMarketShareByUserByClient.xls");
		}

		[Test]
		public void Build_report_by_legal_entity()
		{
			Property("Type", 3);

			report = new SupplierMarketShareByUser(Conn, properties);
			BuildReport("SupplierMarketShareByUserByLegalEntity.xls");
		}

		[Test]
		public void Build_report_without_notAccepted_orders()
		{
			Property("Type", 0);
			var order2 = CreateOrder(null, supplier);
			order2.Deleted = true;
			session.Save(order2);

			var order3 = CreateOrder(null, supplier);
			order3.Submited = false;
			session.Save(order3);

			report = new SupplierMarketShareByUser(Conn, properties);
			var resfile = "RepWithoutNotAcceptedOrders.xls";
			BuildReport(resfile);
			Assert.IsTrue(File.Exists(resfile));

			var repTable = report.GetReportTable();
			bool acceptedOrder = false;
			foreach (DataRow row in repTable.Rows) {
				if (!String.IsNullOrEmpty(row.ItemArray[2].ToString())) {
					Assert.AreNotEqual(row.ItemArray[2].ToString(), order2.User.Id.ToString());
					Assert.AreNotEqual(row.ItemArray[2].ToString(), order3.User.Id.ToString());
					if(order.User.Id.ToString().Equals(row.ItemArray[2].ToString()))
						acceptedOrder = true;
				}
			}
			Assert.True(acceptedOrder);
		}

		[Test]
		public void Calculate_supplier_client_id()
		{
			var intersection = session.Query<TestIntersection>()
				.First(i => i.Price == order.Price && i.Client == order.Client);
			intersection.SupplierClientId = Guid.NewGuid().ToString();
			session.Save(intersection);

			session.Save(new TestAnalitFUpdateLog(TestRequestType.SendOrders, order.User) {
				RequestTime = DateTime.Now.AddDays(-1)
			});

			session.Save(new TestAnalitFUpdateLog(TestRequestType.AnalitFNetSentOrders, order.User) {
				RequestTime = DateTime.Now.AddDays(-1)
			});

			//Заявка должна попасть в предыдущий период
			var prevOrder1 = CreateOrder(order.Client);
			prevOrder1.WriteTime = DateTime.Now.AddDays(-20);
			session.Save(prevOrder1);

			var prevOrder2 = CreateOrder(order.Client, order.Price.Supplier);
			prevOrder2.WriteTime = DateTime.Now.AddDays(-20);
			session.Save(prevOrder2);

			Property("Type", 3);

			var report = ReadReport<SupplierMarketShareByUser>();
			var result = ToText(report);
			Assert.That(result, Does.Contain(intersection.SupplierClientId));
			Assert.That(result, Does.Contain("Кол-во поставщиков"));
			Assert.That(result, Does.Contain("Кол-во сессий отправки заказов"));
			Assert.That(result, Does.Contain("Самая поздняя заявка"));
			Assert.That(result, Does.Contain("Изменение доли"));
			var rows = report.Rows().ToArray();
			//проверяем что индексы которые используются ниже не изменились
			var header = rows[4];
			Assert.AreEqual("Изменение доли", header.GetCell(3).StringCellValue);
			Assert.AreEqual("Кол-во поставщиков", header.GetCell(5).StringCellValue);
			Assert.AreEqual("Кол-во сессий отправки заказов", header.GetCell(6).StringCellValue);
			Assert.AreEqual("Самая поздняя заявка", header.GetCell(7).StringCellValue);
			//проверяем что в колонке Кол-во поставщиков есть данные
			var reportRow = rows
				.First(r => r.GetCell(0) != null && r.GetCell(0).StringCellValue == intersection.SupplierClientId);
			Assert.AreEqual(50, NullableConvert.ToDecimal(reportRow.GetCell(3).StringCellValue));
			Assert.That(Convert.ToUInt32(reportRow.GetCell(5).StringCellValue), Is.GreaterThan(0));
			Assert.AreEqual("2", reportRow.GetCell(6).StringCellValue);
			Assert.AreEqual(order.WriteTime.ToString("HH:mm:ss"), reportRow.GetCell(7).StringCellValue);
		}

		[Test]
		public void Zero_report_interval()
		{
			var intersection = session.Query<TestIntersection>()
				.First(i => i.Price == order.Price && i.Client == order.Client);
			intersection.SupplierClientId = Guid.NewGuid().ToString();
			session.Save(intersection);

			order.WriteTime = DateTime.Now;

			Property("Type", 3);
			TryInitReport<SupplierMarketShareByUser>();
			((BaseOrdersReport)report).ReportPeriod = ReportPeriod.ByToday;
			var sheet = ReadReport<SupplierMarketShareByUser>();
			Assert.AreEqual(order.Sum().ToString("C"), ValueByColumn(sheet, intersection.SupplierClientId, "Сумма по 'Тестовый поставщик'"));
		}

		private object ValueByColumn(ISheet sheet, string key, string name)
		{
			var rows = sheet.Rows().ToArray();
			var header = rows[4];
			var reportRow = rows
				.FirstOrDefault(r => r.GetCell(0) != null && r.GetCell(0).StringCellValue == key);
			Assert.IsNotNull(reportRow, $"Не удалось найти строку с кодом {key} на листе {ToText(sheet)}");
			return reportRow.Cells[header.Cells.IndexOf(x => x.StringCellValue == name)].StringCellValue;
		}

		[Test]
		public void Show_total_sum()
		{
			Property("ShowAllSum", true);
			Property("Type", 2);

			var report = ReadReport<SupplierMarketShareByUser>();
			var result = ToText(report);
			Assert.That(result, Does.Contain("Сумма по всем поставщикам"));
			var rows = report.Rows().ToArray();
			var header = rows[4];
			Assert.AreEqual("Сумма по всем поставщикам", header.GetCell(5).StringCellValue);
			Assert.That(Convert.ToDecimal(rows[5].GetCell(5).StringCellValue), Is.GreaterThan(0));
		}

		[Test]
		public void SetTotalSumTest()
		{
			var testReport = new SupplierMarketShareByUser();

			var table = new DataTable("testTable");
			table.Columns.Add("TotalSum");
			table.Columns.Add("SupplierSum");
			table.Columns.Add("PrevTotalSum");
			table.Columns.Add("PrevSupplierSum");
			var dataRow = table.NewRow();

			var resultTable = new DataTable("resultTable");
			resultTable.Columns.Add("Share");
			resultTable.Columns.Add("SupplierSum");
			var resultRow = resultTable.NewRow();

			dataRow["TotalSum"] = 0;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual(resultRow["Share"], DBNull.Value);

			dataRow["TotalSum"] = 100000;
			dataRow["SupplierSum"] = 5000;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual("5,00", resultRow["Share"]);

			dataRow["SupplierSum"] = 20000;
			testReport.SetTotalSum(dataRow, resultRow);
			Assert.AreEqual("20,0", resultRow["Share"]);
		}
	}
}