using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;

namespace Inforoom.ReportSystem
{
	//Индивидуальный отчет на основе комбинированного, где каждая позиция представляется отдельной строкой и выводится в текстовый файл
	class CombToPlainReport : ProviderReport
	{
		string _sharePath;
		string _filename;

		public CombToPlainReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
			_sharePath = Properties.Settings.Default.DBDumpPath;
			if (String.IsNullOrEmpty(_sharePath))
				throw new Exception("Не установлен параметр DBDumpPath в конфигурационном файле.");
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
  replace( replace( replace(c.name, '\t', ''), '\r', ''), '\n', '') as name,
  -- форма выпуска
  replace( replace( replace(c.form, '\t', ''), '\r', ''), '\n', '') as form,
  -- код поставщика
  replace( replace( replace(Core.code, '\t', ''), '\r', ''), '\n', '') as code,
  -- синоним
  replace( replace( replace(s.synonym, '\t', ''), '\r', ''), '\n', '') as synonym,
  -- синоним производителя
  replace( replace( replace(sfc.synonym, '\t', ''), '\r', ''), '\n', '') as sfcsynonym,
  -- упаковка
  replace( replace( replace(Core.volume, '\t', ''), '\r', ''), '\n', '') as volume,
  -- применчание
  replace( replace( replace(Core.note, '\t', ''), '\r', ''), '\n', '') as note,
  -- срок годности
  Core.period,
  -- признак уценки
  if(Core.junk, '1', '0'),
  -- наименование прайс-листа
  pd.PriceName,
  -- регион
  regions.Region,
  -- дата прайс-листа
  date_add(ActivePrices.PriceDate, interval time_to_sec(date_sub(now(), interval unix_timestamp() second)) second) as DateCurPrice, 
  -- цена препарата
  Core.Cost,
  -- кол-во препарата
  Core.Quantity,
  -- краткое название прайс-листа
  cd.ShortName,
  -- региональный телефон техподдержки
  rd.SupportPhone,
  -- факс
  cd.Fax as Fax,
  -- E-mail для заказов
  rd.adminmail,
  -- УРЛ
  cd.Url, 
  -- открытая наценка
  round(ActivePrices.PublicUpCost, 3) 
INTO OUTFILE 'results/{0}'
FIELDS TERMINATED BY '{1}'
LINES TERMINATED BY '\n'
from 
  Core,
  ActivePrices,
  farm.regions,
  farm.catalog c,
  farm.catalogfirmcr cfc,
  farm.synonym s,
  farm.synonymfirmcr sfc,
  usersettings.regionaldata rd,
  usersettings.clientsdata cd,
  usersettings.pricesdata pd
where
  c.FullCode = Core.fullcode 
and s.synonymcode = Core.synonymcode
and sfc.SynonymFirmCrCode = Core.SynonymFirmCrCode
and cfc.codefirmcr = Core.codefirmcr
and ActivePrices.PriceCode = Core.PriceCode
and ActivePrices.RegionCode = Core.RegionCode
and regions.RegionCode = ActivePrices.RegionCode
and rd.regioncode = Core.RegionCode
and rd.FirmCode = ActivePrices.FirmCode
and cd.FirmCode = ActivePrices.FirmCode
and pd.PriceCode = ActivePrices.PriceCode
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
						throw new Exception(String.Format("Не удалось переместить файл {0} в файл {1}.", _sharePath + _filename, FileName), e);
				}
			}
			while (!CopySucces);
		}

	}
}
