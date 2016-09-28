using System;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support;

namespace ReportTuner.Test
{
	[TestFixture]
	public class UpdateConfigFixture : IntegrationFixture
	{
		[Test]
		public void Update_config()
		{
			new UpdateReportConfig(session).Execute();
		}
	}
}