using System;
using System.IO;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;

namespace Inforoom.ReportSystem
{
	public class CombToPlainReport : ProviderReport
	{
		string _sharePath;
		string _filename;

		public CombToPlainReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			_sharePath = Properties.Settings.Default.DBDumpPath;
			if (String.IsNullOrEmpty(_sharePath))
				throw new ReportException("�� ���������� �������� DBDumpPath � ���������������� �����.");
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
			GetOffers(e);

			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select
  -- ������������
  replace( replace( replace(catalognames.name, '\t', ''), '\r', ''), '\n', '') as name,
  -- ����� �������
  replace( replace( replace(catalogforms.form, '\t', ''), '\r', ''), '\n', '') as form,
  -- ��� ����������
  replace( replace( replace(FarmCore.code, '\t', ''), '\r', ''), '\n', '') as code,
  -- �������
  replace( replace( replace(s.synonym, '\t', ''), '\r', ''), '\n', '') as synonym,
  -- ������� �������������
  replace( replace( replace(sfc.synonym, '\t', ''), '\r', ''), '\n', '') as sfcsynonym,
  -- ��������
  replace( replace( replace(FarmCore.volume, '\t', ''), '\r', ''), '\n', '') as volume,
  -- �����������
  replace( replace( replace(FarmCore.note, '\t', ''), '\r', ''), '\n', '') as note,
  -- ���� ��������
  FarmCore.period,
  -- ������� ������
  if(FarmCore.junk, '1', '0'),
  -- ������������ �����-�����
  pd.PriceName,
  -- ������
  regions.Region,
  -- ���� �����-�����
  date_add(ActivePrices.PriceDate, interval time_to_sec(date_sub(now(), interval unix_timestamp() second)) second) as DateCurPrice, 
  -- ���� ���������
  Core.Cost,
  -- ���-�� ���������
  FarmCore.Quantity,
  -- ������� �������� �����-�����
  cd.ShortName,
  -- ������������ ������� ������������
  rd.SupportPhone,
  -- ����
  cd.Fax as Fax,
  -- E-mail ��� �������
  rd.adminmail,
  -- ���
  '' as Url, 
  -- �������� �������
  round(ActivePrices.PublicUpCost, 3),
  -- �������� ������
  catalog.VitallyImportant
INTO OUTFILE 'C:/AFFiles/{0}'
FIELDS TERMINATED BY '{1}'
LINES TERMINATED BY '\n'
from 
  Core,
  ActivePrices,
  farm.regions,
  Farm.Core0 FarmCore,
  catalogs.products,
  catalogs.catalog,
  catalogs.catalognames,
  catalogs.catalogforms,
  catalogs.Producers cfc,
  farm.synonym s,
  farm.synonymfirmcr sfc,
  usersettings.regionaldata rd,
  usersettings.clientsdata cd,
  usersettings.pricesdata pd
where
    FarmCore.Id = Core.Id 
and s.synonymcode = FarmCore.synonymcode
and sfc.SynonymFirmCrCode = FarmCore.SynonymFirmCrCode
and cfc.Id = FarmCore.codefirmcr
and ActivePrices.PriceCode = Core.PriceCode
and ActivePrices.RegionCode = Core.RegionCode
and regions.RegionCode = ActivePrices.RegionCode
and rd.regioncode = Core.RegionCode
and rd.FirmCode = ActivePrices.FirmCode
and cd.FirmCode = ActivePrices.FirmCode
and pd.PriceCode = ActivePrices.PriceCode
and products.id = Core.ProductId
and catalog.id = products.catalogid
and catalognames.id = catalog.nameid
and catalogforms.id = catalog.formid
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
#if !DEBUG
					File.Delete(_sharePath + _filename);
#endif
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
						throw new ReportException(String.Format("�� ������� ����������� ���� {0} � ���� {1}.", _sharePath + _filename, FileName), e);
				}
			}
			while (!CopySucces);
		}

	}
}
