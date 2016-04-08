using System.Linq;
using NHibernate.Linq;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support;
using Test.Support.Web;

namespace ReportTuner.Test.TestHelpers
{
	[TestFixture]
	public class ReportWatinFixture : WatinFixture2
	{
		protected TestPayer payer;

		protected void OpenReport(Report report)
		{
			Open("Reports/ReportProperties.aspx?rp={0}&r={1}", report.Id, report.GeneralReport.Id);
		}

		protected Report CreateReport(string reportType)
		{
			payer = new TestPayer("Тестовый плательщик");
			var org = new TestLegalEntity(payer, "Тестовое юр. лицо");
			payer.Orgs.Add(org);
			session.Save(payer);
			session.Flush();
			org.Name += " " + org.Id;
			session.Save(org);

			var type = session.Query<ReportType>().First(t => t.ReportTypeFilePrefix == reportType);
			var generalReport = new GeneralReport(session.Load<Payer>(payer.Id));
			var report = generalReport.AddReport(type);
			session.Save(generalReport);
			session.Save(report);
			//что сработал триггер который создаст параметры
			session.Flush();

			report.Refresh();

			return report;
		}
	}
}