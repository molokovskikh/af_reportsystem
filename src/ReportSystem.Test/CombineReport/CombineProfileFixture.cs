using System;
using System.Collections.Generic;
using System.Configuration;
using NUnit.Framework;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;

namespace ReportSystem.Test
{
	[TestFixture]
	public class CombineProfileFixture : BaseProfileFixture
	{
		[Test, Ignore("Временно, выполняется слишком долго")]
		public void Combine()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.Combine);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.Combine);
		}

		[Test, Ignore("Временно, необходимо сменить клиента, т.к. этот отключен")]
		public void CombineCount()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineCount);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineCount);
		}

		[Test, Ignore("Временно, необходимо сменить клиента, т.к. этот отключен")]
		public void CombineCountAndProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineCountAndProducer);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineCountAndProducer);
		}

		[Test, Ignore("Временно, выполняется слишком долго")]
		public void CombineProducer()
		{
			var props = TestHelper.LoadProperties(ReportsTypes.CombineProducer);
			var report = new CombReport(0, "Automate Created Report", Conn, ReportFormats.Excel, props);
			TestHelper.ProcessReport(report, ReportsTypes.CombineProducer);
		}
	}
}