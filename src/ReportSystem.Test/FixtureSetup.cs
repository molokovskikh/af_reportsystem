using System.Collections.Generic;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Inforoom.ReportSystem.Model;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[SetUpFixture]
	public class FixtureSetup
	{
		public static string ConnectionStringName;
		public static string ConnectionString;

		[SetUp]
		public void SetupFixture()
		{
			ConnectionStringName = ConnectionHelper.GetConnectionName();
			ConnectionString = ConnectionHelper.GetConnectionString();
			if (!ActiveRecordStarter.IsInitialized) {
				var config = new InPlaceConfigurationSource();
				config.Add(typeof(ActiveRecordBase),
					new Dictionary<string, string> {
						{ NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
						{ NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
						{ NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
						{ NHibernate.Cfg.Environment.ConnectionStringName, ConnectionStringName },
						{ NHibernate.Cfg.Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle" },
						{ NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none" },
						{ NHibernate.Cfg.Environment.FormatSql, "true" },
						{ NHibernate.Cfg.Environment.UseSqlComments, "true" }
					});
				ActiveRecordStarter.Initialize(new[] { typeof(Supplier).Assembly, Assembly.Load("Test.Support") }, config);
			}
		}
	}
}