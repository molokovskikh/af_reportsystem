using System.Collections.Generic;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Common.Web.Ui.Models;
using Inforoom.ReportSystem;
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
			//в тестах не может быть блокировок
			With.DefaultMaxRepeatCount = 0;

			var nhibernateParams = new Dictionary<string, string> {
				{ Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
				{ Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
				{ Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
				{ Environment.ConnectionStringName, ConnectionStringName },
				{ Environment.Hbm2ddlKeyWords, "none" },
				{ Environment.FormatSql, "true" },
				{ Environment.UseSqlComments, "true" }
			};

			if (!ActiveRecordStarter.IsInitialized) {
				var config = new InPlaceConfigurationSource();
				config.PluralizeTableNames = true;
				config.Add(typeof(ActiveRecordBase), nhibernateParams);

				ActiveRecordStarter.Initialize(new[] {
						Assembly.Load("ReportSystem"),
						typeof(ContactGroup).Assembly,
						Assembly.Load("Test.Support")
					},
					config);

				HbmSerializer.Default.HbmAutoImport = false;
				foreach (var cfg in ActiveRecordMediator.GetSessionFactoryHolder().GetAllConfigurations()) {
					cfg.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Common.Models")));
				}
				Program.Factory = ActiveRecordMediator.GetSessionFactoryHolder().GetSessionFactory(typeof(ActiveRecordBase));
			}
		}
	}
}