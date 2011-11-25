using System;
using System.Collections.Generic;
using Common.MySql;
using Common.Tools;
using MySql.Data.MySqlClient;

namespace Report.Data.Builder
{
	public class Db
	{
		public static IEnumerable<T> Read<T>(string sql, Func<MySqlDataReader, T> read, object parameters = null)
		{
			using (var connection = new MySqlConnection(With.GetConnectionString()))
			{
				connection.Open();
				var commnad = new MySqlCommand(sql, connection);
				if (parameters != null)
				{
					foreach(var keyValue in ObjectExtentions.ToDictionary(parameters))
					{
						commnad.Parameters.AddWithValue(keyValue.Key, keyValue.Value);
					}
				}
				using (var reader = commnad.ExecuteReader())
				{
					do
					{
						while (reader.Read())
							yield return read(reader);
					} while (reader.NextResult());
				}
			}
		}

		public static IEnumerable<T> Read<T>(string sql)
		{
			return Read(sql, r => (T)Convert.ChangeType(r.GetValue(0), typeof(T)));
		}
	}
}