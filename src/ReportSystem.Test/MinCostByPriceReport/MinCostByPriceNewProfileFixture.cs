using ExcelLibrary.SpreadSheet;
using NUnit.Framework;
using Inforoom.ReportSystem;

namespace ReportSystem.Test
{
	[TestFixture]
	public class	MinCostByPriceNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MinCostByPriceNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNew);
		}

		[Test, Ignore("Разобраться")]
		public void MinCostByPriceNewWithClients()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewWithClients);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewWithClients);

			var workbook = Workbook.Load(TestHelper.GetFileName(ReportsTypes.MinCostByPriceNewWithClients));
			Assert.That(workbook.Worksheets.Count, Is.GreaterThan(0));
			var list = workbook.Worksheets[0];
			Assert.That(list.Cells.Rows.Count, Is.GreaterThan(2));
			Assert.That(list.Cells[2, 0].StringValue, Is.StringStarting("Выбранные аптеки: "));
			Assert.That(list.Cells[5, 0].StringValue, Is.StringStarting("Список поставщиков: "));
		}

		[Test, Ignore("Разобраться")]
		public void MinCostByPriceNewWithOneClient()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewWithOneClient);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewWithOneClient);

			var workbook = Workbook.Load(TestHelper.GetFileName(ReportsTypes.MinCostByPriceNewWithOneClient));
			Assert.That(workbook.Worksheets.Count, Is.GreaterThan(0));
			var list = workbook.Worksheets[0];
			Assert.That(list.Cells.Rows.Count, Is.GreaterThan(2));
			Assert.That(list.Cells[2, 0].StringValue, Is.Not.StringStarting("Выбранные аптеки: "));
			Assert.That(list.Cells[2, 0].StringValue, Is.StringStarting("Список поставщиков: "));
		}

		[Test, Ignore("Разобраться")]
		public void MinCostByPriceNewWithClientsWithoutAssortmentPrice()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewWithClientsWithoutAssortmentPrice);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewWithClientsWithoutAssortmentPrice);
		}

		[Test]
		public void MinCostByPriceNewWithoutClients()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewWithoutClients);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			Assert.That(
				() => TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewWithoutClients),
				Throws.InstanceOf<ReportException>()
					.And.Property("Message").EqualTo("Параметр 'Clients' не найден."));
		}

		[Test]
		public void MinCostByPriceNewWithoutClientsZero()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewWithClientsZero);

			if (!props.Tables.Contains("ReportPropertyValues"))
			{
				var values = props.Tables.Add("ReportPropertyValues");
				values.Columns.Add("ReportPropertyID");
				values.Columns.Add("Value");
			}

			var report = new SpecShortReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			Assert.That(
				() => TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewWithClientsZero),
				Throws.InstanceOf<ReportException>()
					.And.Property("Message").EqualTo("Не установлен параметр \"Список аптек\"."));
		}

		[Test]
		public void With_ignored_suppliers()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNew);
			AddProperty(props, "IgnoredSuppliers", new [] {5, 7});
			var report = new SpecShortReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNew);
		}

		[Test]
		public void MinCostByPriceNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MinCostByPriceNewDifficult);
			var report = new SpecShortReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MinCostByPriceNewDifficult);
		}
	}
}
