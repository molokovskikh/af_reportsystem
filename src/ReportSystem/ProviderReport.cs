using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using System.Configuration;

namespace Inforoom.ReportSystem
{
	//Вспомогательный отчет, создаваемый по заказу поставщиков
	public abstract class ProviderReport : BaseReport
	{
		//Код клиента, необходимый для получения текущих прайс-листов и предложений, относительно этого клиента
		protected int _clientCode;

		protected bool IsNewClient = false;

		public ProviderReport(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, temporary, format, dsProperties)
		{ 
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = "select * from future.Clients where Id = " + _clientCode;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			IsNewClient = reader.Read();
			reader.Close();
		}


		/// <summary>
		/// Метод по списку ID формарует строку для вставки в запрос вида: where t.item in (id1, id2, id3...)
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public virtual string ConcatWhereIn(List<ulong> items)
		{
			var result = "(";
			foreach (var item in items)
			{
				result += (item + ", ");
			}
			result = result.Substring(0, result.Length - 2);
			result += ")";
			return result;
		}

		/// <summary>
		/// Метод возвращает имена по списку ID и SQL запросу
		/// </summary>
		/// <param name="action">лямбда вырадение считывания данных</param>
		/// <param name="command">SQL запрос к серверу</param>
		/// <returns></returns>
		private List<string> GetNames(Func<MySqlDataReader, string> action, string command)
		{
			var result = new List<string>();
			var selectCommand = args.DataAdapter.SelectCommand;
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

		public virtual List<String> GetSupplierNames(List<ulong > suppliers)
		{
			var command = string.Format(@"
select concat(cd.ShortName, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')') as SupplierName
from usersettings.Core cor
	join usersettings.PricesData pd on pd.PriceCode = cor.PriceCode
	join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
where cd.FirmCode in {0}
group by cd.FirmCode
order by cd.ShortName", ConcatWhereIn(suppliers));
			return GetNames(r => r["SupplierName"].ToString(), command);
		}

		public virtual List<String> GetClientNames(List<ulong > _clients)
		{
			var command = @"select cl.FullName from future.Clients cl where cl.Id in " + ConcatWhereIn(_clients);
			return GetNames(r => r["FullName"].ToString(), command);
		}

		protected void GetActivePrices()
		{
			GetActivePrices(args);
		}

		protected void GetOffers()
		{
			GetOffers(args);
		}

		//Получили список действующих прайс-листов для интересующего клиента
		protected void GetActivePrices(ExecuteArgs e)
		{
			//удаление временных таблиц
			e.DataAdapter.SelectCommand.CommandText = "drop temporary table IF EXISTS Prices, ActivePrices, Core, MinCosts";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			if(IsNewClient)
				GetActivePricesNew();
			else
				GetActivePricesOld();

			List<ulong> allowedFirms = null;
			if (_reportParams.ContainsKey("FirmCodeEqual"))
				allowedFirms = (List<ulong>)_reportParams["FirmCodeEqual"];
			if(allowedFirms != null && allowedFirms.Count > 0)
			{
				e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
				e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where FirmCode not in ({0})", allowedFirms.Implode());
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
			}

			if (_reportParams.ContainsKey("IgnoredSuppliers"))
			{
				var suppliers = (List<ulong>)_reportParams["IgnoredSuppliers"];
				if (suppliers != null && suppliers.Count > 0)
				{
					e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
					e.DataAdapter.SelectCommand.CommandText = String.Format("delete from ActivePrices where FirmCode in ({0})", suppliers.Implode());
					e.DataAdapter.SelectCommand.ExecuteNonQuery();
				}
			}

			//Добавляем в таблицу ActivePrices поле FirmName и заполняем его также, как раньше для отчетов
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
			e.DataAdapter.SelectCommand.CommandText = @"
alter table ActivePrices add column FirmName varchar(100);
update 
  ActivePrices, usersettings.clientsdata, farm.regions 
set 
  FirmName = concat(clientsdata.ShortName, '(', ActivePrices.PriceName, ') - ', regions.Region)
where 
    activeprices.FirmCode = clientsdata.FirmCode 
and regions.RegionCode = activeprices.RegionCode";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

		}

		protected void GetActivePricesNew()
		{// Небольшая магия, через любого пользователя получаем прайсы клиента

			// Получаем первого попавшегося пользователя
			var userId = GetUserId();

			// Получаем для него все прайсы
			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.CommandText = "future.GetPrices";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			selectCommand.ExecuteNonQuery();

			// Включаем для него все прайсы
			selectCommand.CommandType = CommandType.Text;
			selectCommand.CommandText = "update Prices set DisabledByClient = 0";
			selectCommand.ExecuteNonQuery();

			// Получаем для пользователя активные (которыми теперь являются все) прайсы
			selectCommand.CommandText = "future.GetActivePrices";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			selectCommand.ExecuteNonQuery();
		}

		private uint GetUserId()
		{
			args.DataAdapter.SelectCommand.CommandText = "select Id from future.Users where ClientId = " + _clientCode + " limit 1";
			return Convert.ToUInt32(args.DataAdapter.SelectCommand.ExecuteScalar());
		}

		private void GetActivePricesOld()
		{
			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.CommandText = "usersettings.GetActivePrices";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?ClientCodeParam", _clientCode);
			selectCommand.ExecuteNonQuery();
		}

		//Получили список предложений для интересующего клиента
		protected void GetOffers(ExecuteArgs e)
		{
			GetActivePrices(e);

			if(IsNewClient)
				GetOffersNew();
			else
				GetOffersOld();

			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
		}

		private void GetOffersNew()
		{ // Небольшая магия, через любого пользователя получаем предложение для клиента

			// Получаем первого попавшегося пользователя
			var userId = GetUserId();

			//Проверка существования и отключения клиента
			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.CommandText =
				"select * from future.Clients cl where cl.Id = " + _clientCode;
			var reader = selectCommand.ExecuteReader();
			if (!reader.Read())
				throw new ReportException(String.Format("Невозможно найти клиента с кодом {0}.", _clientCode));
			if (Convert.ToByte(reader["Status"]) == 0)
				throw new ReportException(String.Format("Невозможно сформировать отчет по отключенному клиенту {0} ({1}).", reader["Name"], _clientCode));
			reader.Close();

			selectCommand.CommandText = "future.GetOffers";
			selectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			selectCommand.ExecuteNonQuery();
		}

		protected void GetOffersOld()
		{
			//Проверка существования и отключения клиента
			DataRow drClient = MySqlHelper.ExecuteDataRow(
				ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
				"select FirmCode, FirmStatus, ShortName from usersettings.clientsdata cd where cd.FirmCode = ?FirmCode",
				new MySqlParameter("?FirmCode", _clientCode));
			if (drClient == null)
				throw new ReportException(String.Format("Невозможно найти клиента с кодом {0}.", _clientCode));
			else
				if (Convert.ToByte(drClient["FirmStatus"]) == 0)
					throw new ReportException(String.Format("Невозможно сформировать отчет по отключенному клиенту {0} ({1}).", drClient["ShortName"], _clientCode));

			var selectCommand = args.DataAdapter.SelectCommand;
			selectCommand.CommandText = "usersettings.GetOffers";
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?ClientCodeParam", _clientCode);
			selectCommand.Parameters.AddWithValue("?FreshOnly", 0);
			selectCommand.ExecuteNonQuery();
		}

		public static string GetSuppliers(ExecuteArgs e)
		{
			var suppliers = new List<string>();
			e.DataAdapter.SelectCommand.CommandText = @"
select concat(cd.ShortName, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from Core cor
	join usersettings.PricesData pd on pd.PriceCode = cor.PriceCode
	join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
group by cd.FirmCode
order by cd.ShortName";
			using(var reader = e.DataAdapter.SelectCommand.ExecuteReader())
			{
				while(reader.Read())
					suppliers.Add(Convert.ToString(reader[0]));
			}
			return suppliers.Distinct().Implode();
		}

		public string GetIgnoredSuppliers(ExecuteArgs e)
		{
			if (!_reportParams.ContainsKey("IgnoredSuppliers"))
				return null;

			var supplierIds = (List<ulong>)_reportParams["IgnoredSuppliers"];
			if (supplierIds.Count == 0)
				return null;

			var suppliers = new List<string>();
			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select concat(cd.ShortName, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from usersettings.PricesData pd
	join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
where pd.PriceCode in ({0})
group by cd.FirmCode
order by cd.ShortName", supplierIds.Implode());
			using(var reader = e.DataAdapter.SelectCommand.ExecuteReader())
			{
				while(reader.Read())
					suppliers.Add(Convert.ToString(reader[0]));
			}
			return suppliers.Distinct().Implode();
		}
	}
}
