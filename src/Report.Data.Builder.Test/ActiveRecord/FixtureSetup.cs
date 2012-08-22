using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Common.Web.Ui.Models.Jobs;
using NHibernate.Cfg;
using NUnit.Framework;

namespace Report.Data.Builder.Test.ActiveRecord
{
	[SetUpFixture]
	public class FixtureSetup
	{
		[SetUp]
		public void Setup()
		{
			var config = new InPlaceConfigurationSource();
			config.PluralizeTableNames = true;
			config.Add(typeof(ActiveRecordBase),
				new Dictionary<string, string> {
					{ Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
					{ Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
					{ Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
					{ Environment.ConnectionStringName, ConnectionHelper.GetConnectionName() },
					{ Environment.Isolation, "ReadCommitted" },
					{ Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle" },
					{ Environment.Hbm2ddlKeyWords, "none" },
					{ Environment.FormatSql, "true" },
					{ Environment.UseSqlComments, "true" }
				});
			ActiveRecordStarter.Initialize(new[] { typeof(Job).Assembly }, config);
		}
	}
}