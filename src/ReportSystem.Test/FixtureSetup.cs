using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Inforoom.ReportSystem.Model;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[SetUpFixture]
	public class FixtureSetup
	{
		[SetUp]
		public void SetupFixture()
		{			
			var connectionStringName = ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>().Skip(1).First().Name;
			if (!ActiveRecordStarter.IsInitialized)
			{
				var config = new InPlaceConfigurationSource();
				config.Add(typeof(ActiveRecordBase),
					new Dictionary<string, string> {
						{NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
						{NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
						{NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
						{NHibernate.Cfg.Environment.ConnectionStringName, connectionStringName},
						{NHibernate.Cfg.Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"},
						{NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none"},
						{NHibernate.Cfg.Environment.FormatSql, "true"},
						{NHibernate.Cfg.Environment.UseSqlComments, "true"}
					});
				ActiveRecordStarter.Initialize(new[] {  typeof(Supplier).Assembly, Assembly.Load("Test.Support")}, config);			 
			}
		}
	}
}
