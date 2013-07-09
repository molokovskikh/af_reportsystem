using System.Collections.Generic;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Inforoom.ReportSystem.Model;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
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

			var nhibernateParams = new Dictionary<string, string> {
				{ NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
				{ NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
				{ NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
				{ NHibernate.Cfg.Environment.ConnectionStringName, ConnectionStringName },
				{ NHibernate.Cfg.Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle" },
				{ NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none" },
				{ NHibernate.Cfg.Environment.FormatSql, "true" },
				{ NHibernate.Cfg.Environment.UseSqlComments, "true" }
			};

			if (!ActiveRecordStarter.IsInitialized) {
				var config = new InPlaceConfigurationSource();
				config.PluralizeTableNames = true;
				config.Add(typeof(ActiveRecordBase), nhibernateParams);

				ActiveRecordStarter.Initialize(new[] { typeof(Region).Assembly, Assembly.Load("Test.Support") }, config);

				foreach (Configuration cfg in ActiveRecordMediator.GetSessionFactoryHolder().GetAllConfigurations()) {
					cfg.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Common.Models")));
				}
			}
		}
	}
}