using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	[TestFixture]
	public class SpecialReportFilterFixture
	{
		private MySqlConnection MyCn;
		private MySqlCommand MyCmd;
		private MySqlDataAdapter MyDA;

		[SetUp]
		public void Setup()
		{
			MyCn = new MySqlConnection(FixtureSetup.ConnectionString);
			MyCmd = new MySqlCommand();
			MyDA = new MySqlDataAdapter();
		}

		DataTable FillClients(string proc, string filter, string id, string inTypes = null)
		{
			var dtProcResult = new DataTable();
			string db = String.Empty;
			try {
				if (MyCn.State != ConnectionState.Open)
					MyCn.Open();
				db = MyCn.Database;
				MyCn.ChangeDatabase("reports");
				MyCmd.Connection = MyCn;
				MyDA.SelectCommand = MyCmd;
				MyCmd.Parameters.Clear();
				MyCmd.Parameters.AddWithValue("inFilter", filter);
				MyCmd.Parameters["inFilter"].Direction = ParameterDirection.Input;
				if (id == String.Empty)
					MyCmd.Parameters.AddWithValue("inID", DBNull.Value);
				else
					MyCmd.Parameters.AddWithValue("inID", Convert.ToInt64(id));
				MyCmd.Parameters["inID"].Direction = ParameterDirection.Input;
				if(String.IsNullOrEmpty(inTypes)) {
					MyCmd.Parameters.AddWithValue("inTypes", -1);
				}
				else {
					MyCmd.Parameters.AddWithValue("inTypes", inTypes);
				}
				MyCmd.Parameters["inTypes"].Direction = ParameterDirection.Input;
				MyCmd.CommandText = proc;
				MyCmd.CommandType = CommandType.StoredProcedure;
				MyDA.Fill(dtProcResult);
			}
			finally {
				if (db != String.Empty)
					MyCn.ChangeDatabase(db);
				MyCmd.CommandType = CommandType.Text;
				MyCn.Close();
			}
			return dtProcResult;
		}

		[Test]
		public void TestSpecialReportPricesFilter()
		{
			var prices = FillClients("GetPricesByRegionMaskByTypes", "1", "1", String.Format("{0},{1}", 1, 2));
			List<uint> id = new List<uint>();
			foreach (DataRow row in prices.Rows) {
				id.Add(uint.Parse(row[0].ToString()));
			}
			using (new SessionScope()) {
				var query = TestPrice.Queryable.Where(t => id.Contains(t.Id));
				Assert.That(query.Count(q => q.PriceType == PriceType.Regular), Is.EqualTo(prices.Rows.Count));
			}
		}
	}
}
