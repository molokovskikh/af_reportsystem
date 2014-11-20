using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Model;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	[TestFixture]
	internal class TestProcessReport : IntegrationFixture
	{
		public class FakeEmptyReport : BaseReport
		{
			public FakeEmptyReport()
			{
				_dsReport = new DataSet();
			}

			protected override void GenerateReport(ExecuteArgs e)
			{
			}

			public override void ReadReportParams()
			{
			}
		}

		public class FakeReport : BaseReport
		{
			public FakeReport()
			{
				_dsReport = new DataSet();
			}

			public override void ReadReportParams()
			{
			}

			protected override void GenerateReport(ExecuteArgs e)
			{
				Thread.Sleep(1000);
			}

			public override void ReportToFile(string fileName)
			{
			}
		}

		public class FakeReportWithReportException : FakeReport
		{
			protected override void GenerateReport(ExecuteArgs e)
			{
				base.GenerateReport(null);
				throw new ReportException("Ошибка при формировании отчета.");
			}
		}

		public class FakeReportWithException : FakeReport
		{
			public FakeReportWithException()
			{
				ReportCode = 10;
				ReportCaption = "FakeReportWithException";
			}

			protected override void GenerateReport(ExecuteArgs e)
			{
				base.GenerateReport(null);
				throw new Exception("Системная ошибка.");
			}
		}

		public class FakeGeneralReport : GeneralReport
		{
			public FakeGeneralReport()
			{
				Payer = new Payer {
					Name = "Тестовый плательщик"
				};
				Reports = new List<BaseReport>();
			}

			public void Add(FakeReport report)
			{
				Reports.Add(report);
			}

			public void AddRange(FakeReport[] reports)
			{
				foreach (var fakeReport in reports) {
					Reports.Add(fakeReport);
				}
			}
		}

		[Test]
		public void Skip_empty_check_if_result_table_not_exists()
		{
			var report = new FakeEmptyReport();
			report.ReportToFile("test.xls");
		}

		[Test, Description("Тестирует обработку различных типов исключений в процессе работы отчетов")]
		public void TestExceptionDuringProcessReport()
		{
			var dtStart = DateTime.Now;
			var gr = new FakeGeneralReport();
			gr.AddRange(new[] {
				new FakeReport(), new FakeReportWithReportException(), new FakeReportWithReportException(),
				new FakeReport(), new FakeReportWithException(), new FakeReport()
			});

			var ex = false;
			try {
				gr.ProcessReports(new ReportExecuteLog(), null, false, DateTime.Today, DateTime.Today, false);
			}
			catch (ReportException e) {
				Assert.That(e.Message, Is.EqualTo("Системная ошибка."));
				Assert.That(e.SubreportCode, Is.EqualTo(10));
				Assert.That(e.Payer, Is.EqualTo("Тестовый плательщик"));
				Assert.That(e.ReportCaption, Is.EqualTo("FakeReportWithException"));
				ex = true;
			}
			Assert.That(ex, Is.True);
			// Проверяем записи в логах
			var logs = session.Query<ReportResultLog>().Where(l => l.StartTime >= dtStart).OrderBy(l => l.StartTime).ToList();
			Assert.That(logs.Count, Is.EqualTo(5));
			Assert.That(logs[0].ErrorMessage, Is.Null);
			Assert.That(logs[1].ErrorMessage, Is.StringContaining("Ошибка при формировании отчета."));
			Assert.That(logs[2].ErrorMessage, Is.StringContaining("Ошибка при формировании отчета."));
			Assert.That(logs[3].ErrorMessage, Is.Null);
			Assert.That(logs[4].ErrorMessage, Is.StringContaining("Системная ошибка."));
		}
	}
}