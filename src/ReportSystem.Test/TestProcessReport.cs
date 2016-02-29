using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
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

			protected override void GenerateReport()
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

			protected override void GenerateReport()
			{
				Thread.Sleep(1000);
			}

			public override void Write(string fileName)
			{
			}
		}

		public class FakeReportWithReportException : FakeReport
		{
			public override void Write(string fileName)
			{
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

			public override void Write(string fileName)
			{
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
			}
		}

		[Test]
		public void Skip_empty_check_if_result_table_not_exists()
		{
			var report = new FakeEmptyReport();
			report.Write("test.xls");
		}

		[Test, Description("Тестирует обработку различных типов исключений в процессе работы отчетов")]
		public void TestExceptionDuringProcessReport()
		{
			var dtStart = DateTime.Now;
			var gr = new FakeGeneralReport();
			var reports = new[] {
				new FakeReport(), new FakeReportWithReportException(), new FakeReportWithReportException(),
				new FakeReport(), new FakeReportWithException(), new FakeReport()
			};
			reports.Each(x => gr.Reports.Enqueue(x));

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