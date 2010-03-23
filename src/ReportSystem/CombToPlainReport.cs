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
				throw new ReportException("Не установлен параметр DBDumpPath в конфигурационном файле.");
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
			//Выбираем 
			GetOffers(e);

			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select
  -- наименование
  replace( replace( replace(catalognames.name, '\t', ''), '\r', ''), '\n', '') as name,
  -- форма выпуска
  replace( replace( replace(catalogforms.form, '\t', ''), '\r', ''), '\n', '') as form,
  -- код поставщика
  replace( replace( replace(FarmCore.code, '\t', ''), '\r', ''), '\n', '') as code,
  -- синоним
  replace( replace( replace(s.synonym, '\t', ''), '\r', ''), '\n', '') as synonym,
  -- синоним производителя
  replace( replace( replace(sfc.synonym, '\t', ''), '\r', ''), '\n', '') as sfcsynonym,
  -- упаковка
  replace( replace( replace(FarmCore.volume, '\t', ''), '\r', ''), '\n', '') as volume,
  -- применчание
  replace( replace( replace(FarmCore.note, '\t', ''), '\r', ''), '\n', '') as note,
  -- срок годности
  FarmCore.period,
  -- признак уценки
  if(FarmCore.junk, '1', '0'),
  -- наименование прайс-листа
  pd.PriceName,
  -- регион
  regions.Region,
  -- дата прайс-листа
  date_add(ActivePrices.PriceDate, interval time_to_sec(date_sub(now(), interval unix_timestamp() second)) second) as DateCurPrice, 
  -- цена препарата
  Core.Cost,
  -- кол-во препарата
  FarmCore.Quantity,
  -- краткое название прайс-листа
  cd.ShortName,
  -- региональный телефон техподдержки
  rd.SupportPhone,
  -- факс
  cd.Fax as Fax,
  -- E-mail для заказов
  rd.adminmail,
  -- УРЛ
  '' as Url, 
  -- открытая наценка
  round(ActivePrices.PublicUpCost, 3),
  -- жизненно важный
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
						throw new ReportException(String.Format("Не удалось переместить файл {0} в файл {1}.", _sharePath + _filename, FileName), e);
				}
			}
			while (!CopySucces);
		}

	}
}
