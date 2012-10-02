using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using NHibernate.Linq;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using Test.Support;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SpecialProfileFixture : BaseProfileFixture
	{
		[Test]
		public void Special()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Special);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Special);
		}

		[Test]
		public void SpecialCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialCount);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialCount);
			ProductQuantityTest(report.DSResult, Convert.ToUInt32(report.getReportParam("PriceCode")));
		}

		[Test]
		public void SpecialCountProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialCountProducer);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialCountProducer);
		}

		[Test]
		public void SpecialCountProducerByWeightCost()
		{
			Property("ReportType", 4);
			Property("RegionEqual", new List<ulong> {
				1
			});
			Property("ClientCode", 5101);
			Property("ReportSortedByPrice", false);
			Property("ReportIsFull", false);
			Property("ShowPercents", true);
			Property("CalculateByCatalog", false);
			Property("PriceCode", 216);
			Property("ByWeightCosts", true);
			BuildReport("SpecialCountProducerByWeightCost.xls", typeof(SpecReport));
		}

		[Test]
		public void SpecialProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.SpecialProducer);
			var report = new SpecReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.SpecialProducer);
		}

		private void ProductQuantityTest(DataSet resultDS, uint priceCode)
		{
			var catalog = resultDS.Tables["Catalog"];
			var result = resultDS.Tables["Results"];
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = holder.CreateSession(typeof(ActiveRecordBase));
			int maxRowCount = 0;
			try {
				foreach (DataRow row in result.Rows) {
					if(String.IsNullOrEmpty(row[0].ToString()))
						continue;
					var productRows = catalog.Select(String.Format("Code='{0}'", row["F1"]));
					var product = session.Query<TestCore>().First(t => t.Code == row["F1"] && t.Id == Convert.ToUInt64(productRows[0]["ID"])).Product;
					var core = session.Query<TestCore>().Where(t => t.Price.Id == priceCode && t.Product == product);
					int quantity = 0;
					foreach (var testCore in core) {
						quantity += Convert.ToInt32(testCore.Quantity);
					}
					Assert.That(quantity.ToString(), Is.EqualTo(row["F5"]));
					maxRowCount++;
					if(maxRowCount > 100)
						break;
				}
			}
			finally {
				holder.ReleaseSession(session);
			}
		}
	}
}