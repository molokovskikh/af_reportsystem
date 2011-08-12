using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExecuteTemplate;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.Helpers
{
	public static class ReadParameterHelper
	{
		/// <summary>
		/// Метод возвращает имена по списку ID и SQL запросу
		/// </summary>
		/// <param name="action">лямбда вырадение считывания данных</param>
		/// <param name="command">SQL запрос к серверу</param>
		/// <returns></returns>
		private static List<string> GetNames(Func<MySqlDataReader, string> action, string command, ExecuteArgs e)
		{
			var result = new List<string>();
			var selectCommand = e.DataAdapter.SelectCommand;
			selectCommand.CommandText = command;
			using (var reader = selectCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					result.Add(action(reader));
				}
				reader.Close();
			}
			return result;
		}

		public static List<String> GetSupplierNames(List<ulong> suppliers, ExecuteArgs e)
		{			
            var command = @"select supps.Name as ShortName from future.suppliers supps where supps.Id in " + ProviderReport.ConcatWhereIn(suppliers);
			return GetNames(r => r["ShortName"].ToString(), command, e);
		}

		public static List<String> GetClientNames(List<ulong> _clients, ExecuteArgs e)
		{
			var command = @"select cl.FullName from future.Clients cl where cl.Id in " + ProviderReport.ConcatWhereIn(_clients);
			return GetNames(r => r["FullName"].ToString(), command, e);
		}

		public static List<String> GetPayerNames(List<ulong> _payers, ExecuteArgs e)
		{
			var command = @"SELECT p.ShortName FROM billing.payers p where p.PayerId in " + ProviderReport.ConcatWhereIn(_payers);
			return GetNames(r => r["ShortName"].ToString(), command, e);
		}

		public static List<String> GetPriceName(List<ulong> _priceCode, ExecuteArgs e)
		{			
            var command = @"SELECT supps.Name as ShortName FROM usersettings.PricesData P 
							join future.suppliers supps on supps.Id = p.FirmCode
							where p.PriceCode = " + _priceCode[0];
			return GetNames(r => r["ShortName"].ToString(), command, e);
		}

		public static List<String> GetCrNames(List<ulong> _produsers, ExecuteArgs e)
		{
			var command = @"SELECT P.Name FROM catalogs.Producers P where p.id in " + ProviderReport.ConcatWhereIn(_produsers);
			return GetNames(r => r["Name"].ToString(), command, e);
		}

		public static List<String> GetRegionNames(List<ulong> _regions, ExecuteArgs e)
		{
			var command = @"SELECT R.Region FROM farm.Regions R where R.RegionCode in " + ProviderReport.ConcatWhereIn(_regions);
			return GetNames(r => r["Region"].ToString(), command, e);
		}

		public static List<String> GetPriceNames(List<ulong> _prices, ExecuteArgs e)
		{			
            var command = @"select pd.PriceCode as PriceCode,
	convert(concat(supps.Name, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName
  from
    usersettings.pricesdata pd
    inner join future.suppliers supps on supps.Id = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = supps.HomeRegion
	where pd.PriceCode in " + ProviderReport.ConcatWhereIn(_prices);
			return GetNames(r => r["PriceName"].ToString(), command, e);
		}
	}
}
