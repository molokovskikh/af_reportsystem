using System;
using System.IO;
using Common.Tools;
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
			FileHelper.InitDir("tmp");
			if (String.Equals(Environment.MachineName, "devsrv", StringComparison.OrdinalIgnoreCase)) {
				Settings.Default.IntoOutfilePath = @"\\devsrv\public";
				Settings.Default.DBDumpPath = @"\\devsrv\public";
				File.Delete("C:\\public\\ind_r_1.txt");
			}
			else {
				Settings.Default.IntoOutfilePath = Path.GetFullPath(".");
				Settings.Default.DBDumpPath = Path.GetFullPath(".");
				File.Delete("ind_r_1.tx");
			}
			var client = TestClient.CreateNaked(session);
			Property("ClientCode", client.Id);
			TryInitReport<CombToPlainReport>("test", ReportFormats.DBF);
			BuildReport("tmp/test.dbf");
		}
	}
}