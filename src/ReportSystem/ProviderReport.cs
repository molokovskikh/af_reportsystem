using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using System.Configuration;

namespace Inforoom.ReportSystem
{
	//Вспомогательный отчет, создаваемый по заказу поставщиков
	public class ProviderReport : BaseReport
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

		public override void ReadReportParams()
		{}

		//Получили список действующих прайс-листов для интересующего клиента
		protected void GetActivePrices(ExecuteArgs e)
		{
			//удаление временных таблиц
			e.DataAdapter.SelectCommand.CommandText = "drop temporary table IF EXISTS Prices, ActivePrices, Core, MinCosts";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			if(IsNewClient)
				GetActivePricesNew(e);
			else
				GetActivePricesOld(e);

			List<ulong> allowedFirms = null;
			if (_reportParams.ContainsKey("FirmCodeEqual"))
				allowedFirms = (List<ulong>)_reportParams["FirmCodeEqual"];
			if(allowedFirms != null && allowedFirms.Count > 0)
			{  // Если задана настройка список клиентов, то исключаем ПЛ поставщиков кот. не в списке
				var firms = new StringBuilder();
				firms.Append('(');
				allowedFirms.ForEach( f => firms.Append(f).Append(','));
				firms[firms.Length-1] = ')';

				e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
				e.DataAdapter.SelectCommand.CommandText = "delete from ActivePrices where FirmCode not in " + firms;
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
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

		protected void GetActivePricesNew(ExecuteArgs e)
		{// Небольшая магия, через любого пользователя получаем прайсы клиента

			// Получаем первого попавшегося пользователя
			var userId = GetUserId(e);

			// Получаем для него все прайсы
			e.DataAdapter.SelectCommand.CommandText = "future.GetPrices";
			e.DataAdapter.SelectCommand.CommandType = CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			// Включаем для него все прайсы
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
			e.DataAdapter.SelectCommand.CommandText = "update Prices set DisabledByClient = 0";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			// Получаем для пользователя активные (которыми теперь являются все) прайсы
			e.DataAdapter.SelectCommand.CommandText = "future.GetActivePrices";
			e.DataAdapter.SelectCommand.CommandType = CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		private uint GetUserId(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = "select Id from future.Users where ClientId = " + _clientCode + " limit 1";
			return Convert.ToUInt32(e.DataAdapter.SelectCommand.ExecuteScalar());
		}

		protected void GetActivePricesOld(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = "usersettings.GetActivePrices";
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ClientCodeParam", _clientCode);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		//Получили список предложений для интересующего клиента
		protected void GetOffers(ExecuteArgs e)
		{
			GetActivePrices(e);

			if(IsNewClient)
				GetOffersNew(e);
			else
				GetOffersOld(e);

			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
		}

		protected void GetOffersNew(ExecuteArgs e)
		{ // Небольшая магия, через любого пользователя получаем предложение для клиента

			// Получаем первого попавшегося пользователя
			var userId = GetUserId(e);

			//Проверка существования и отключения клиента
			e.DataAdapter.SelectCommand.CommandText =
				"select * from future.Clients cl where cl.Id = " + _clientCode;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			if (!reader.Read())
				throw new ReportException(String.Format("Невозможно найти клиента с кодом {0}.", _clientCode));
			if (Convert.ToByte(reader["Status"]) == 0)
				throw new ReportException(String.Format("Невозможно сформировать отчет по отключенному клиенту {0} ({1}).", reader["Name"], _clientCode));
			reader.Close();

			e.DataAdapter.SelectCommand.CommandText = "future.GetOffers";
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		protected void GetOffersOld(ExecuteArgs e)
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

			e.DataAdapter.SelectCommand.CommandText = "usersettings.GetOffers";
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ClientCodeParam", _clientCode);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?FreshOnly", 0);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}
	}
}
