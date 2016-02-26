using System;
using System.Diagnostics;
using System.IO;
using Inforoom.ReportSystem.Properties;
using MySql.Data.MySqlClient;

using System.Data;

namespace Inforoom.ReportSystem
{
	public class CombToPlainReport : ProviderReport
	{
		private string _filename;
		private string _exportFilename;

		public CombToPlainReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
			if (String.IsNullOrEmpty(Settings.Default.DBDumpPath))
				throw new ReportException("Не установлен параметр DBDumpPath в конфигурационном файле.");
			if (String.IsNullOrEmpty(Settings.Default.IntoOutfilePath))
				throw new ReportException("Не установлен параметр IntoOutfilePath в конфигурационном файле.");

			var name = "ind_r_" + ReportCode.ToString() + ".txt";
			_exportFilename = Path.Combine(Settings.Default.IntoOutfilePath, name).Replace('\\', '/');
			_filename = Path.Combine(Settings.Default.DBDumpPath, name).Replace('\\', '/');
			if (File.Exists(_filename))
				File.Delete(_filename);
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_clientCode = (int)GetReportParam("ClientCode");
		}

		protected override void GenerateReport()
		{
			//Выбираем
			GetOffers(_SupplierNoise);

			DataAdapter.SelectCommand.CommandText = String.Format(@"
drop temporary table if exists Usersettings.MaxProducerCosts;
create temporary table Usersettings.MaxProducerCosts(
	ProductId int unsigned not null,
	ProducerId int unsigned,
	Cost decimal(19, 2) not null,
	key(ProductId, ProducerId)
) engine=memory;

insert into Usersettings.MaxProducerCosts(ProductId, ProducerId, Cost)
select c0.ProductId, c0.CodeFirmCr, max(cc.Cost)
from Farm.Core0 c0
	join Farm.CoreCosts cc on cc.Core_Id = c0.Id
	join Catalogs.Products p on p.Id = c0.ProductId
		join Catalogs.Catalog c on c.Id = p.CatalogId
	join Farm.Synonym s on s.SynonymCode = c0.SynonymCode
	left join Farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c0.SynonymFirmCrCode
where c0.PriceCode = ?priceId and cc.PC_CostCode = ?costId
group by c0.ProductId, c0.CodeFirmCr;

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
  -- примечание
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
  (case Core.Cost
    when 1000000 then 0
    when 999999.99 then 0
    else Core.Cost
  end) as Cost,
  -- кол-во препарата
  FarmCore.Quantity,
  -- краткое название прайс-листа
  supps.Name,
  -- региональный телефон техподдержки
  rd.SupportPhone,
  -- факс
  '' as Fax,
  -- E-mail для заказов
  rd.adminmail,
  -- УРЛ
  '' as Url,
  -- открытая наценка
  0 as PublicUpCost,
  -- жизненно важный
  catalog.VitallyImportant,
  -- МНН
  mnn.Mnn,
  -- Производитель
  producers.Name,
  catalog.VitallyImportant,
  catalog.MandatoryList,
  m.Cost
INTO OUTFILE '{0}'
FIELDS TERMINATED BY '{1}'
LINES TERMINATED BY '\n'
from
  (Core,
  ActivePrices,
  farm.regions,
  Farm.Core0 FarmCore,
  catalogs.products,
  catalogs.catalog,
  catalogs.catalognames,
  catalogs.catalogforms,
  farm.synonym s,
  farm.synonymfirmcr sfc,
  usersettings.regionaldata rd,
  Customers.suppliers supps,
  usersettings.pricesdata pd)
  left join catalogs.mnn on mnn.Id = catalognames.mnnid
  left join catalogs.producers on producers.id = FarmCore.CodeFirmCr
  left join Usersettings.MaxProducerCosts m on m.ProductId = FarmCore.ProductId and m.ProducerId = FarmCore.CodeFirmCr
where
	FarmCore.Id = Core.Id
and s.synonymcode = FarmCore.synonymcode
and sfc.SynonymFirmCrCode = FarmCore.SynonymFirmCrCode
and ActivePrices.PriceCode = Core.PriceCode
and ActivePrices.RegionCode = Core.RegionCode
and regions.RegionCode = ActivePrices.RegionCode
and rd.regioncode = Core.RegionCode
and rd.FirmCode = ActivePrices.FirmCode
and supps.Id = ActivePrices.FirmCode
and pd.PriceCode = ActivePrices.PriceCode
and products.id = Core.ProductId
and catalog.id = products.catalogid
and catalognames.id = catalog.nameid
and catalogforms.id = catalog.formid;

drop temporary table if exists Usersettings.MaxProducerCosts;
",
				_exportFilename,
				(char)9);
#if DEBUG
			Debug.WriteLine(DataAdapter.SelectCommand.CommandText);
#endif
			DataAdapter.SelectCommand.Parameters.AddWithValue("priceId", 4863);
			DataAdapter.SelectCommand.Parameters.AddWithValue("costId", 8148);
			DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		public override void Write(string FileName)
		{
			ReadReportParams();
			ProcessReport();
			int CopyErrorCount = 0;
			bool CopySucces = false;
			do {
				try {
					File.Copy(_filename, FileName, true);
#if !DEBUG
					File.Delete(_filename);
#endif
					CopySucces = true;
				}
				catch (Exception e) {
					if (CopyErrorCount < 10) {
						CopyErrorCount++;
						System.Threading.Thread.Sleep(1000);
					}
					else
						throw new ReportException(String.Format("Не удалось переместить файл {0} в файл {1}.", _filename, FileName), e);
				}
			} while (!CopySucces);
		}
	}
}