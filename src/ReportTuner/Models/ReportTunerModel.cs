using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Criterion;
using System.Text;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data.Common;
using System.Collections;

namespace ReportTuner.Models
{
	public static class ReportTunerModel
	{
		private const string allClientsSql =
@"
select 
       cd.FirmCode Id,
       cd.ShortName,
       GROUP_CONCAT(reg.Region ORDER BY reg.Region SEPARATOR ', ') Regions
  from usersettings.ClientsData cd
       left join farm.Regions reg on (reg.regionCode & cd.MaskRegion) > 0
 where cd.FirmStatus = 1
   and cd.FirmType = ?firmType
   and (cd.MaskRegion & ?region) > 0
   and cd.FirmCode not in {0}
   and cd.ShortName like ?filterStr
group by Id

union

select
       cl.Id,
       cl.Name ShortName,
       GROUP_CONCAT(reg.Region ORDER BY reg.Region SEPARATOR ', ') Regions
  from future.Clients cl
       left join farm.Regions reg on (reg.regionCode & cl.MaskRegion) > 0
 where ?firmType = 1
   and cl.Status = 1
   and (cl.MaskRegion & ?region) > 0
   and cl.Id not in {0}
   and cl.Name like ?filterStr
group by Id
{1} {2}
";

		private const string selectedClientsSql =
@"
select 
       cd.FirmCode Id,
       cd.ShortName,
       GROUP_CONCAT(reg.Region ORDER BY reg.Region SEPARATOR ', ') Regions
  from usersettings.ClientsData cd
       left join farm.Regions reg on (reg.regionCode & cd.MaskRegion) > 0
 where cd.FirmCode in {0}
group by Id

union

select
       cl.Id,
       cl.Name ShortName,
       GROUP_CONCAT(reg.Region ORDER BY reg.Region SEPARATOR ', ') Regions
  from future.Clients cl
       left join farm.Regions reg on (reg.regionCode & cl.MaskRegion) > 0
 where cl.Id in {0}
group by Id
{1} {2}
";

		private static string GetSelectedIds(ulong reportProperty)
		{
			var addedClients = ReportPropertyValue.FindAll(Expression.Eq("ReportPropertyId", reportProperty));
			var addedClientsIds = new StringBuilder("(0,");
			foreach (var clientId in addedClients)
				addedClientsIds.Append(clientId.Value).Append(',');
			addedClientsIds[addedClientsIds.Length - 1] = ')';
			return addedClientsIds.ToString();
		}

		private static string GetPreparedSql(string sql, int sortOrder, int currenPage, int pageSize, string selectedIds, bool usePadding)
		{
			string[] headers = new[] { "", "Id", "ShortName", "RegionCode" };
			string order = (sortOrder < 1)
        		? ""
        		: ("order by " + headers[Math.Abs(sortOrder) - 1] + ((sortOrder > 0) ? " asc" : " desc"));
			string limit = usePadding ? String.Format("limit {0}, {1}", currenPage*pageSize, pageSize) : "";

			return String.Format(sql, selectedIds, order, limit);
		}

		private static List<object> ExtractClientsFromCommand(MySqlCommand command)
		{
			var reader = command.ExecuteReader();
			var clients = from row in reader.Cast<DbDataRecord>()
					  select new
					  {
						  Id = row["Id"],
						  ShortName = row["ShortName"],
						  Regions = row["Regions"]
					  };
			return clients.Cast<object>().ToList();
		}

		public static List<object> GetAllSuppliers(ulong reportProperty, int sortOrder, int currenPage, int pageSize,
			ref int? rowsCount, ulong region, byte firmType, string findStr)
		{
			List<object> clients;
			using(var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				var Ids = GetSelectedIds(reportProperty);
				var sql = GetPreparedSql(allClientsSql, sortOrder, currenPage, pageSize, Ids, rowsCount.HasValue);
				var command = new MySqlCommand(sql, conn);

				command.Parameters.AddWithValue("?firmType", firmType);
				command.Parameters.AddWithValue("?region", region);
				command.Parameters.AddWithValue("?filterStr", "%" + findStr + "%");

				conn.Open();

				clients = ExtractClientsFromCommand(command);
				if (!rowsCount.HasValue)
				{
					rowsCount = clients.Count;
					clients = clients.GetRange(0, pageSize);
				}
			}
			return clients;
		}

		public static List<object> GetAddedSuppliers(ulong reportCode, ulong reportProperty, int sortOrder, int startPage,
			int pageSize)
		{
			List<object> clients;
			using(var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				var Ids = GetSelectedIds(reportProperty);
				var sql = GetPreparedSql(selectedClientsSql, sortOrder, 0, pageSize, Ids, false);
				var command = new MySqlCommand(sql, conn);

				conn.Open();

				clients = ExtractClientsFromCommand(command);
				
			}
			return clients;
		}

		public static void DeleteClient(ulong reportProperty, ulong clientCode)
		{
			var properties = ReportPropertyValue.FindAll(new[] 
				{Expression.Eq("ReportPropertyId", reportProperty), Expression.Eq("Value", clientCode.ToString())});

			foreach(var property in properties)
				property.DeleteAndFlush();
		}

		public static void AddClient(ulong reportProperty, ulong clientCode)
		{
			var property = new ReportPropertyValue();
			property.ReportPropertyId = reportProperty;
			property.Value = clientCode.ToString();

			property.CreateAndFlush();
		}
	}
}
