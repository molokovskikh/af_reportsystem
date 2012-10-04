using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Model;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	[TestFixture]
	public class ProgramTest : IntegrationFixture
	{
		[Test]
		public void Base_test()
		{
			session.CreateSQLQuery("delete from `logs`.reportexecutelogs; update  reports.general_reports set allow = 0;").ExecuteUpdate();
			Close();
			Program.Main(new[] { "/gr:1" });
			var reportLogCount = session.Query<ReportExecuteLog>().Count();
			Assert.AreEqual(reportLogCount, 1);
		}
	}
}
