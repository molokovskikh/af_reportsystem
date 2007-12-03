using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;

namespace Inforoom.ReportSystem
{
	//��������������� �����, ����������� �� ������ �����������
	public class ProviderReport : BaseReport
	{
		//��� �������, ����������� ��� ��������� ������� �����-������ � �����������, ������������ ����� �������
		protected int _clientCode;

		public ProviderReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
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
