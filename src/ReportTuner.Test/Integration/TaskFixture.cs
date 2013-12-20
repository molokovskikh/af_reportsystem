using System;
using NUnit.Framework;
using ReportSysmte.Tasks;
using Test.Support;
using Test.Support.log4net;

namespace ReportTuner.Test
{
	[TestFixture, Explicit]
	public class TaskFixture : IntegrationFixture
	{
		[Test(Description = "Задача предназначена для генерации миграций")]
		public void Test()
		{
			QueryCatcher.Catch();
			var t = new UpdateReportConfig(session);
			t.Execute();
		}
	}
}
