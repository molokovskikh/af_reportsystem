using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	public class DefecturePharmacie
	{
		public static void TestReportResultOnPharmacie(DataTable result)
		{
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = holder.CreateSession(typeof(ActiveRecordBase));
			try {
				foreach (DataRow row in result.Rows) {
					var core = session.QueryOver<TestCore>().Where(t => t.Code == row["Code"].ToString()).List();
					Assert.That(core.Count(t => t.Product.CatalogProduct.Pharmacie == false && t.Product.CatalogProduct.CatalogName.Name == row["Name"].ToString()),
						Is.EqualTo(0));
				}
			}
			finally {
				holder.ReleaseSession(session);
			}
		}
	}
}
