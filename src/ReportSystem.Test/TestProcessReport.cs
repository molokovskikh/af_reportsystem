using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using ExecuteTemplate;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Model;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	class TestProcessReport
	{
		public class FakeReport : BaseReport
		{
			public FakeReport()
			{
				_dsReport = new DataSet();				
			}
			public override void ReadReportParams()
			{ }
			public override void GenerateReport(ExecuteArgs e)
			{
				Thread.Sleep(1000);
			}
			public override void ReportToFile(string fileName)
			{ }
		}

		public class FakeReportWithReportException : FakeReport
		{
			public override void GenerateReport(ExecuteArgs e)
			{
				base.GenerateReport(null);
				throw new ReportException("Ошибка при формировании отчета.");
			}
		}

		public class FakeReportWithException : FakeReport
		{
			public override void GenerateReport(ExecuteArgs e)
			{
				base.GenerateReport(null);
				throw new Exception("Системная ошибка.");
			}
		}

		public class FakeGeneralReport : GeneralReport
		{
			public FakeGeneralReport()
			{
				Reports = new List<BaseReport>();
			}
			public void Add(FakeReport report)
			{
				Reports.Add(report);
			}
			public void AddRange(FakeReport[] reports)
			{
				foreach (var fakeReport in reports)
				{
					Reports.Add(fakeReport);
				}
			}
		}

		[Test, Description("Тестирует обработку различных типов исключений в процессе работы отчетов")]
		public void TestExceptionDuringProcessReport()
		{
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordStarter.Initialize( typeof(ReportResultLog).Assembly, ActiveRecordSectionHandler.Instance);

			var dtStart = DateTime.Now;
			var gr = new FakeGeneralReport();
			gr.AddRange(new[] { new FakeReport(), new FakeReportWithReportException(), new FakeReportWithReportException(), 
								new FakeReport(), new FakeReportWithException(), new FakeReport() });

			var ex = false;
			try
			{
				gr.ProcessReports();
			}
			catch (Exception e)
			{
				Assert.That(e.Message, Is.EqualTo("Системная ошибка."));
				ex = true;
			}
			Assert.That(ex, Is.True);
			using(new SessionScope())
			{
				// Проверяем записи в логах
				var logs = ReportResultLog.Queryable.Where(l => l.StartTime >= dtStart).OrderBy(l => l.StartTime).ToList();
				Assert.That(logs.Count, Is.EqualTo(5));
				Assert.That(logs[0].ErrorMessage, Is.Null);
				Assert.That(logs[1].ErrorMessage, Is.StringContaining("Ошибка при формировании отчета."));
				Assert.That(logs[2].ErrorMessage, Is.StringContaining("Ошибка при формировании отчета."));
				Assert.That(logs[3].ErrorMessage, Is.Null);
				Assert.That(logs[4].ErrorMessage, Is.StringContaining("Системная ошибка."));
			}
		}
	}
}
