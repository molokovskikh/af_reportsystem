using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class MixedReportNewProfileFixture : BaseProfileFixture
	{
		[Test]
		public void MixedNew()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedNew);
			var report = new MixedReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedNew);
		}

		[Test]
		public void MixedNewDifficult()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.MixedNewDifficult);
			var report = new MixedReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.MixedNewDifficult);
		}


		[Test]
		public void Build_report_with_several_concurrent_groups()
		{
			Property("ShowCode", true);
			Property("ShowCodeCr", true);
			Property("ByPreviousMonth", false);
			Property("ReportInterval", 7);
			Property("ProductNamePosition", 0);
			//протек
			Property("SourceFirmCode", 5);
			//роста
			Property("BusinessRivals", new List<long> { 216 });
			//сиа
			Property("BusinessRivals2", new List<long> { 14 });
			//аптека холдинг
			Property("BusinessRivals3", new List<long> { 39 });
			BuildReport(reportType: typeof(MixedReport));
		}

		[Test]
		public void Throw_on_empty_order()
		{
			Property("ByPreviousMonth", false);
			Property("ProductNamePosition", 0);
			//протек
			Property("SourceFirmCode", 5);
			//роста
			Property("BusinessRivals", new List<long> { 216 });

			report = new MixedReport(1, "", Conn, ReportFormats.Excel, properties);
			report.Interval = true;
			report.From = new DateTime(2000, 1, 1);
			report.To = new DateTime(2000, 1, 2);

			var e = Assert.Throws<Exception>(() => BuildReport(checkEmptyData: true));
			Assert.That(e.Message, Is.StringContaining("В результате подготовки отчета получился пустой набор данных"));
		}
	}
}