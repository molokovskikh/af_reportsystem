using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using System.Configuration;

namespace Inforoom.ReportSystem
{
	//��������������� �����, ����������� �� ������ �����������
	public class ProviderReport : BaseReport
	{
		//��� �������, ����������� ��� ��������� ������� �����-������ � �����������, ������������ ����� �������
		protected int _clientCode;

		public ProviderReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, dsProperties)
		{ 
		}

		public override void GenerateReport(ExecuteArgs e)
		{ 
		}

		public override void ReportToFile(string FileName)
		{ }

		public override void ReadReportParams()
		{}

		//�������� ������ ����������� �����-������ ��� ������������� �������
		protected void GetActivePrices(ExecuteArgs e)
		{
			//�������� ��������� ������
			e.DataAdapter.SelectCommand.CommandText = "drop temporary table IF EXISTS Prices, ActivePrices, Core, MinCosts";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = "usersettings.GetActivePrices";
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ClientCodeParam", _clientCode);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.Text;

			//��������� � ������� ActivePrices ���� FirmName � ��������� ��� �����, ��� ������ ��� �������
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

		//�������� ������ ����������� ��� ������������� �������
		protected void GetOffers(ExecuteArgs e)
		{
			//�������� ������������� � ���������� �������
			DataRow drClient = MySqlHelper.ExecuteDataRow(
				ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
				"select FirmCode, FirmStatus, ShortName from usersettings.clientsdata cd where cd.FirmCode = ?FirmCode",
				new MySqlParameter("?FirmCode", _clientCode));
			if (drClient == null)
				throw new Exception(String.Format("���������� ����� ������� � ����� {0}.", _clientCode));
			else
				if (Convert.ToByte(drClient["FirmStatus"]) == 0)
					throw new Exception(String.Format("���������� ������������ ����� �� ������������ ������� {0} ({1}).", drClient["ShortName"], _clientCode));

			//�������� ��������� ������
			e.DataAdapter.SelectCommand.CommandText = "drop temporary table IF EXISTS Prices, ActivePrices, Core, MinCosts";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = "usersettings.GetOffers";
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ClientCodeParam", _clientCode);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?FreshOnly", 0);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.Text;

			//��������� � ������� ActivePrices ���� FirmName � ��������� ��� �����, ��� ������ ��� �������
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
	}
}
