using System;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Common.Web.Ui.Models;
using Inforoom.ReportSystem;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	[TestFixture]
	public class LeakOffersReportFixture : BaseProfileFixture
	{
		[Test]
		public void Make_report()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.LeakOffers);
			var report = new LeakOffersReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.LeakOffers);
			VitallyImportantAndPharmacieTest(report.DSResult);
		}

		private void VitallyImportantAndPharmacieTest(DataSet result)
		{
			if(result.Tables["Prices"].Rows.Count == 0)
				return;
			var row = result.Tables["Prices"].Rows[0];
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = holder.CreateSession(typeof(ActiveRecordBase));
			try {
				int i = 0;
				foreach (DataRow offer in result.Tables[row["PriceCode"].ToString()].Rows) {
					var core = session.Query<TestCore>().Where(c => c.Code == offer["Code"] &&
						c.Quantity == offer["Quantity"] && c.Price.Id == Convert.ToInt64(row["PriceCode"]) &&
						c.Period == offer["Period"]).ToList();
					
					if(offer["VitallyImportant"].ToString().Contains("+")) 
						Assert.That(core[0].Product.CatalogProduct.VitallyImportant, Is.True);
					else {
						Assert.That(core[0].Product.CatalogProduct.VitallyImportant, Is.False);
					}
					if(offer["Pharmacie"].ToString().Contains("+")) 
						Assert.That(core[0].Product.CatalogProduct.Pharmacie, Is.True);
					else {
						Assert.That(core[0].Product.CatalogProduct.Pharmacie, Is.False);
					}
					i++;
					if (i > 100)
						break;
				}
			}
			finally {
				holder.ReleaseSession(session);
			}
		}
	}
}