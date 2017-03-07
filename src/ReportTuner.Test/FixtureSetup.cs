using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.IO;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Common.Schedule;
using Common.Web.Ui.Models;
using NUnit.Framework;
using CassiniDev;
using ReportTuner.Helpers;
using Test.Support;
using Test.Support.Selenium;
using Test.Support.Web;
using Settings = WatiN.Core.Settings;

namespace ReportTuner.Test
{
	[SetUpFixture]
	public class FixtureSetup
	{
		private Server _webServer;

		public static string ConnectionString;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
			var connectionStringName = ConnectionHelper.GetConnectionName();
			ConnectionString = ConnectionHelper.GetConnectionString();
			if (!ActiveRecordStarter.IsInitialized) {
				var config = new InPlaceConfigurationSource();
				config.PluralizeTableNames = true;
				config.Add(typeof(ActiveRecordBase),
					new Dictionary<string, string> {
						{ NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
						{ NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
						{ NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
						{ NHibernate.Cfg.Environment.ConnectionStringName, connectionStringName },
						{ NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none" },
						{ NHibernate.Cfg.Environment.FormatSql, "true" },
						{ NHibernate.Cfg.Environment.UseSqlComments, "true" }
					});
				ActiveRecordStarter.Initialize(new[] { Assembly.Load("Test.Support"), Assembly.Load("ReportTuner"), Assembly.Load("Common.Web.Ui") }, config);
			}

			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = holder.CreateSession(typeof(ActiveRecordBase));
			var ownerId = uint.Parse(ConfigurationManager.AppSettings["ReportsContactGroupOwnerId"]);
			if (session.Get<ContactGroupOwner>(ownerId) == null) {
				session.CreateSQLQuery($"Insert into contacts.contact_group_owners (Id) VALUES({ownerId})").UniqueResult();
			}
			holder = ActiveRecordMediator.GetSessionFactoryHolder();
			holder.ReleaseSession(session);
			IntegrationFixture2.Factory = holder.GetSessionFactory(typeof(ActiveRecordBase));

			_webServer = SeleniumFixture.StartServer();
			SeleniumFixture.GlobalSetup();

			using (var taskService = ScheduleHelper.GetService()) {
				ScheduleHelper.CreateFolderIfNeeded(taskService);
			}
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_webServer.ShutDown();
			SeleniumFixture.GlobalTearDown();
		}
	}
}