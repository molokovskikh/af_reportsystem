using System;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace ReportSystem.Test
{
	public class BaseProfileFixture
	{
		protected MySqlConnection Conn;

		[SetUp]
		public void Start()
		{
			Conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
			Conn.Open();
		}

		[TearDown]
		public void Stop()
		{
			Conn.Close();
			Conn.Dispose();
		}
	}
}
