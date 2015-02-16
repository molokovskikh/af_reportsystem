using System;
using System.IO;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Properties;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	[TestFixture]
	public class CombToPlainReportFixture : BaseProfileFixture2
	{
		[Test]
		public void Build()
		{
			if (!String.Equals(Environment.MachineName, "devsrv", StringComparison.OrdinalIgnoreCase)) {
				Settings.Default.IntoOutfilePath = Path.GetFullPath(".");
				Settings.Default.DBDumpPath = Path.GetFullPath(".");
			}
			var client = TestClient.CreateNaked(session);
			Property("ClientCode", client.Id);
			InitReport<CombToPlainReport>("test", ReportFormats.DBF);
			BuildReport("tmp/test.dbf");
		}
	}
}