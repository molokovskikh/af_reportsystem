using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.MySql;
using Common.Tools;

using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.Helpers
{
	public static class ReadParameterHelper
	{
		public static List<String> GetSupplierNames(List<ulong> suppliers, MySqlConnection connection)
		{
			var command = String.Format("select supps.Name as ShortName from Customers.suppliers supps where supps.Id in ({0})", suppliers.Implode());
			return connection.Read(command, r => r["ShortName"].ToString()).ToList();
		}

		public static List<String> GetClientNames(List<ulong> clients, MySqlConnection connection)
		{
			var command = String.Format("select cl.FullName from Customers.Clients cl where cl.Id in ({0})", clients.Implode());
			return connection.Read(command, r => r["FullName"].ToString()).ToList();
		}

		public static List<String> GetPayerNames(List<ulong> payers, MySqlConnection connection)
		{
			var command = String.Format("SELECT p.ShortName FROM billing.payers p where p.PayerId in ({0})", payers.Implode());
			return connection.Read(command, r => r["ShortName"].ToString()).ToList();
		}

		public static List<String> GetPriceName(List<ulong> priceCode, MySqlConnection connection)
		{
			var command = @"SELECT supps.Name as ShortName FROM usersettings.PricesData P
							join Customers.suppliers supps on supps.Id = p.FirmCode
							where p.PriceCode = " + priceCode[0];
			return connection.Read(command, r => r["ShortName"].ToString()).ToList();
		}

		public static List<String> GetCrNames(List<ulong> produsers, MySqlConnection connection)
		{
			var command = String.Format("SELECT P.Name FROM catalogs.Producers P where p.id in ({0})", produsers.Implode());
			return connection.Read(command, r => r["Name"].ToString()).ToList();
		}

		public static List<String> GetRegionNames(List<ulong> regions, MySqlConnection connection)
		{
			var command = String.Format("SELECT R.Region FROM farm.Regions R where R.RegionCode in ({0})", regions.Implode());
			return connection.Read(command, r => r["Region"].ToString()).ToList();
		}

		public static List<String> GetPriceNames(List<ulong> prices, MySqlConnection connection)
		{
			var command = String.Format(@"select pd.PriceCode as PriceCode,
	convert(concat(supps.Name, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName
  from
	usersettings.pricesdata pd
	inner join Customers.suppliers supps on supps.Id = pd.FirmCode
	inner join farm.regions rg on rg.RegionCode = supps.HomeRegion
	where pd.PriceCode in ({0})", prices.Implode());
			return connection.Read(command, r => r["PriceName"].ToString()).ToList();
		}
	}
}