using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;

namespace Inforoom.ReportSystem
{
	//�������������� ����� �� ������ ����������������, ��� ������ ������� �������������� ��������� ������� � ��������� � ��������� ����
	class CombToPlainReport : ProviderReport
	{
		string _sharePath;
		string _filename;

		public CombToPlainReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
			_sharePath = Properties.Settings.Default.DBDumpPath;
			if (String.IsNullOrEmpty(_sharePath))
				throw new Exception("�� ���������� �������� DBDumpPath � ���������������� �����.");
			if (!_sharePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
				_sharePath += Path.DirectorySeparatorChar.ToString();
			_filename = "ind_r_" + ReportCode.ToString() + ".txt";
			if (File.Exists(_sharePath + _filename))
				File.Delete(_sharePath + _filename);
		}

		public override void ReadReportParams()
		{
			_clientCode = (int)getReportParam("ClientCode");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			//�������� 
			GetActivePricesT(e);
			GetAllCoreT(e);
			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select
  c.name,
  c.form,
  AllCoreT.code,
  replace( replace( replace(s.synonym, '\t', ''), '\r', ''), '\n', '') as synonym,
  cfc.firmcr,
  AllCoreT.volume,
  AllCoreT.note,
  AllCoreT.period,
  if(AllCoreT.junk, '1', '0'),
  -- ������ ������� ������������ ������� �������� ������������ �������
  -- ActivePricesT.FirmName,
  cd.ShortName,
  ActivePricesT.Region,
  -- ActivePricesT.DateCurPrice,
  date_add(ActivePricesT.DateCurPrice, interval time_to_sec(date_sub(now(), interval unix_timestamp() second)) second) as DateCurPrice, 
  -- replace( replace( replace(rd.OperativeInfo, '\t', ''), '\r', ''), '\n', '') as OperativeInfo,
  AllCoreT.Cost,
  AllCoreT.Quantity,
  cd.ShortName,
  rd.SupportPhone,
  cd.Fax as Fax,
  rd.adminmail,
  cd.Url -- ,
  -- replace( replace( replace(rd.ContactInfo, '\t', ''), '\r', ''), '\n', '') as ContactInfo,
  -- '' as MainManager,
  -- cd.OrderManagerName
INTO OUTFILE 'results/{0}'
FIELDS TERMINATED BY '{1}'
LINES TERMINATED BY '\n'
from 
  AllCoreT,
  ActivePricesT,
  farm.catalog c,
  farm.catalogfirmcr cfc,
  farm.synonym s,
  usersettings.regionaldata rd,
  usersettings.clientsdata cd
where
  c.FullCode = AllCoreT.fullcode 
and s.synonymcode = AllCoreT.synonymcode
and cfc.codefirmcr = AllCoreT.codefirmcr
and ActivePricesT.PriceCode = AllCoreT.PriceCode
and ActivePricesT.RegionCode = AllCoreT.RegionCode
and rd.regioncode = AllCoreT.RegionCode
and rd.FirmCode = ActivePricesT.FirmCode
and cd.FirmCode = ActivePricesT.FirmCode
",
			_filename,
			(char)9
			);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		public override void ReportToFile(string FileName)
		{
			int CopyErrorCount = 0;
			bool CopySucces = false;
			do
			{
				try
				{
					File.Copy(_sharePath + _filename, FileName, true);
					File.Delete(_sharePath + _filename);
					CopySucces = true;
				}
				catch (Exception e)
				{
					if (CopyErrorCount < 10)
					{
						CopyErrorCount++;
						System.Threading.Thread.Sleep(1000);
					}
					else
						throw new Exception(String.Format("�� ������� ����������� ���� {0} � ���� {1}.", _sharePath + _filename, FileName), e);
				}
			}
			while (!CopySucces);
		}

	}
}
