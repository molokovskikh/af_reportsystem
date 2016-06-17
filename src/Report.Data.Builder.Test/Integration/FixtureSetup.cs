using System.Collections.Generic;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Common.Web.Ui.Models.Jobs;
using NHibernate.Cfg;
using NUnit.Framework;

namespace Report.Data.Builder.Test.Integration
{
	[SetUpFixture]
	public class FixtureSetup
	{
		[OneTimeSetUp]
		public void Setup()
		{
			ConnectionHelper.DefaultConnectionStringName = ConnectionHelper.GetConnectionName();

			var config = new InPlaceConfigurationSource();
			config.PluralizeTableNames = true;
			config.Add(typeof(ActiveRecordBase),
				new Dictionary<string, string> {
					{ Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
					{ Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
					{ Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
					{ Environment.ConnectionStringName, ConnectionHelper.GetConnectionName() },
					{ Environment.Isolation, "ReadCommitted" },
					{ Environment.Hbm2ddlKeyWords, "none" },
					{ Environment.FormatSql, "true" },
					{ Environment.UseSqlComments, "true" }
				});
			ActiveRecordStarter.Initialize(new[] { typeof(Job).Assembly, Assembly.Load("Test.Support") }, config);
		}
	}
}