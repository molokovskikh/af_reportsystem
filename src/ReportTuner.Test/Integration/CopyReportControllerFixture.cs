using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.TestSupport;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ReportTuner.Controllers;
using ReportTuner.Models;

namespace ReportTuner.Test.Integration
{
	public class CopyReportControllerFixture : BaseControllerTest
	{
		private ISession _session;

		protected ISessionScope scope;

		protected ISession session
		{
			get
			{
				if (_session == null) {
					var holder = ActiveRecordMediator.GetSessionFactoryHolder();
					_session = holder.CreateSession(typeof(ActiveRecordBase));
				}
				return _session;
			}
		}

		[SetUp]
		public void SetUp()
		{
			scope = new SessionScope();
		}

		[Test]
		public void CopyReportTest()
		{
			var generalReport1 = new GeneralReport {
				Comment = "тестовый отчет1"
			};
			session.Save(generalReport1);
			var generalReport2 = new GeneralReport {
				Comment = "тестовый отчет2"
			};
			session.Save(generalReport2);
			var report1 = new Report {
				GeneralReport = generalReport1,
				Enabled = true,
				ReportCaption = "testReport1",
				ReportType = session.Query<ReportType>().First(),
			};
			session.Save(report1);
			var property = session.Query<ReportProperty>().First(t => t.Report == report1);
			property.Value = "propertyValue1";
			session.Save(property);
			generalReport1.Reports.Add(report1);
			session.Save(generalReport1);
			var controller = new CopyReportController();
			PrepareController(controller);
			controller.DbSession = session;
			controller.CopyReport(generalReport2.Id, new GeneralReportsFilter {
				Report = report1.Id,
				GeneralReport = generalReport1.Id
			});
			session.Evict(generalReport2);
			generalReport2 = session.Query<GeneralReport>().First(t => t.Id == generalReport2.Id);
			var report2 = generalReport2.Reports.First();
			Assert.That(report2, Is.Not.Null);
			Assert.That(report2.Enabled, Is.EqualTo(report1.Enabled));
			Assert.That(report2.ReportCaption, Is.EqualTo(String.Concat("Копия ", report1.ReportCaption)));
			var properties2 = session.Query<ReportProperty>().Where(t => t.Report == report2);
			var properties1 = session.Query<ReportProperty>().Where(t => t.Report == report1);
			foreach (var reportProperty in properties1) {
				Assert.That(properties2.Count(p => p.PropertyType == reportProperty.PropertyType && p.Value == reportProperty.Value), Is.Not.EqualTo(0));
			}
		}

		[TearDown]
		public void TearDown()
		{
			if (_session != null) {
				var holder = ActiveRecordMediator.GetSessionFactoryHolder();
				holder.ReleaseSession(session);
				_session = null;
			}
		}
	}
}
