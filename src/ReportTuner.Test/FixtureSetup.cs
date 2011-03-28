﻿using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.IO;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using NUnit.Framework;
using CassiniDev;
using Settings = WatiN.Core.Settings;

namespace ReportTuner.Test
{
	[SetUpFixture]
	class FixtureSetup
	{
		private Server _webServer;

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
				ActiveRecordStarter.Initialize(new[] { Assembly.Load("Test.Support"), Assembly.Load("ReportTuner"), Assembly.Load("Common.Web.Ui") }, config);			 
			}

			var port = int.Parse(ConfigurationManager.AppSettings["webPort"]);
			var webDir = ConfigurationManager.AppSettings["webDirectory"];	
			_webServer = new Server(port, "/", Path.GetFullPath(webDir));
			_webServer.Start();
			Settings.Instance.AutoMoveMousePointerToTopLeft = false;
			Settings.Instance.MakeNewIeInstanceVisible = false;			
		}

		[TearDown]
		public void TeardownFixture()
		{
			_webServer.ShutDown();
		}
	}
}