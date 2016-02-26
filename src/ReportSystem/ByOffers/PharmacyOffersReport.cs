using System;
using System.Data;

using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using MySql.Data.MySqlClient;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem.FastReports
{
	public enum GroupOffersBy
	{
		Product,
		ProductAndProducer
	}

	public class PharmacyOffersReport : ProviderReport
	{
		private const string headersql = @"
drop temporary table IF EXISTS ExtendedCore;
create temporary table ExtendedCore
(
  id bigint unsigned,
  ProductName VARCHAR(255),
  ProducerId INT UNSIGNED,
  ProducerName VARCHAR(255),
  SupplierName VARCHAR(255),
  INDEX (id),
  index (ProductName)
) engine=MEMORY;
";

		private const string sqlWithoutPriceCode = @"
insert into ExtendedCore (Id) select Id from Core;

update
  ExtendedCore ec
  inner join farm.Core0 on Core0.id = ec.Id
  inner join usersettings.PricesData pd on pd.PriceCode = Core0.PriceCode
  join Customers.suppliers supps on supps.Id = pd.FirmCode
  left join catalogs.Producers on Producers.ID = Core0.CodeFirmCr
set
  ec.ProducerId = Core0.CodeFirmCr,
  ec.ProducerName = Producers.Name,
  ec.SupplierName = supps.Name;

update
  ExtendedCore ec
  inner join Core cor on cor.id = ec.Id
set
  ec.ProductName = (select concat(cat.Name, ' ',
				 ifnull(GROUP_CONCAT(ifnull(PropertyValues.Value, '')
									order by Properties.PropertyName, PropertyValues.Value
									SEPARATOR ', '), ''))
			  from
				 catalogs.products inp
				 join catalogs.Catalog cat on cat.Id = inp.CatalogId
				 left join catalogs.ProductProperties on ProductProperties.ProductId = inp.Id
				 left join catalogs.PropertyValues on PropertyValues.Id = ProductProperties.PropertyValueId
				 left join catalogs.Properties on Properties.Id = PropertyValues.PropertyId
			  where
				inp.Id = cor.ProductId
			)
;
";

		private const string sqlByPriceCode = @"
drop temporary table IF EXISTS OffersByPrice;
create temporary table OffersByPrice
(
  ProductId INT UNSIGNED,
  ProducerId INT UNSIGNED,
  index (ProductId),
  index (ProducerId)
) engine=MEMORY;

insert into OffersByPrice
select distinct
  Core0.ProductId,
  Core0.CodeFirmCr
from
  usersettings.PricesData
  inner join usersettings.PricesCosts pc on pc.PriceCode = PricesData.PriceCode and exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = PricesData.PriceCode and prd.BaseCost=pc.CostCode limit 1)
  inner join farm.Core0 on Core0.PriceCode = PricesData.PriceCode
  inner join farm.CoreCosts cc on cc.Core_Id = Core0.Id and cc.PC_CostCode = pc.CostCode
where
    PricesData.PriceCode = @OffersPriceCode
and PricesData.CostType = 1
union distinct
select
  Core0.ProductId,
  Core0.CodeFirmCr
from
  usersettings.PricesData
  inner join farm.Core0 on Core0.PriceCode = PricesData.PriceCode
where
    PricesData.PriceCode = @OffersPriceCode
and (PricesData.CostType = 0 or PricesData.PriceType = 1);


insert into ExtendedCore (Id)
select
  distinct Core.Id
from
  OffersByPrice
  inner join Core on Core.ProductId = OffersByPrice.ProductId
  inner join farm.Core0 ExistsOffers on
		ExistsOffers.Id = Core.Id
    and ((OffersByPrice.ProducerId is null and ExistsOffers.CodeFirmCr is null) or (OffersByPrice.ProducerId = ExistsOffers.CodeFirmCr))
;
";

		private const string sqlFullOffers = @"
insert into ExtendedCore (Id) select Id from Core;
";

		private const string footersqlByPrice = @"

update
  ExtendedCore ec
  inner join farm.Core0 on Core0.id = ec.Id
  inner join usersettings.PricesData pd on pd.PriceCode = Core0.PriceCode
  join Customers.suppliers supps on supps.Id = pd.FirmCode
  left join catalogs.Producers on Producers.ID = Core0.CodeFirmCr
  left join farm.SynonymFirmCr on SynonymFirmCr.PriceCode = @OffersSynonymCode and SynonymFirmCr.CodeFirmCr = Core0.CodeFirmCr
set
  ec.ProducerId = Core0.CodeFirmCr,
  ec.ProducerName = ifnull(SynonymFirmCr.Synonym, Producers.Name),
  ec.SupplierName = supps.Name;

update
  ExtendedCore ec
  inner join Core cor on cor.id = ec.Id
  left join farm.Synonym on Synonym.PriceCode = @OffersSynonymCode and Synonym.ProductId = cor.ProductId
set
  ec.ProductName = Synonym.Synonym;

update
  ExtendedCore ec
  inner join Core cor on cor.id = ec.Id
set
  ec.ProductName =
		(select concat(cat.Name, ' ',
				 ifnull(GROUP_CONCAT(ifnull(PropertyValues.Value, '')
									order by Properties.PropertyName, PropertyValues.Value
									SEPARATOR ', '), ''))
			  from
				 catalogs.products inp
				 join catalogs.Catalog cat on cat.Id = inp.CatalogId
				 left join catalogs.ProductProperties on ProductProperties.ProductId = inp.Id
				 left join catalogs.PropertyValues on PropertyValues.Id = ProductProperties.PropertyValueId
				 left join catalogs.Properties on Properties.Id = PropertyValues.PropertyId
			  where
				inp.Id = cor.ProductId
		)
where
  ec.ProductName is null
;
";

		private const string footersql = @"
select  c.ProductId,
        ec.ProductName,
		m.Mnn,
        ec.ProducerId,
        ec.ProducerName,
        ec.SupplierName,
        min(if(c0.Junk=0, c.cost, null)) Cost,
		sum(c0.Quantity) as Quantity,
		sfc.Synonym as Producer,
		c0.Code
from Core c
	join ExtendedCore ec on ec.Id = c.Id
	join farm.Core0 c0 on c0.Id = c.Id
		join Catalogs.Products p on p.Id = c0.ProductId
			join Catalogs.Catalog ca on ca.Id = p.CatalogId
				join Catalogs.CatalogNames cn on cn.Id = ca.NameId
					left join Catalogs.Mnn m on m.Id = cn.MnnId
	left join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c0.SynonymFirmCrCode
	join usersettings.PricesData pd on pd.PriceCode = c.PriceCode
group by c.ProductId, ec.ProducerId, pd.FirmCode";

		private const string sqlSetParams = @"
set @OffersPriceCode = {0};
select
  ifnull(pricesdata.ParentSynonym, pricesdata.pricecode) PriceSynonymCode
from
  usersettings.PricesData
where
  PriceCode = @OffersPriceCode
into @OffersSynonymCode;
";

		private bool _includeQuantity;
		private bool _includeProducer;
		private decimal _costDiffThreshold;
		private int _suppliersCount = 0;
		private bool _reportIsFull;
		private int? _priceCode;

		public PharmacyOffersReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.Excel)
				return new BaseExcelWriter();
			return null;
		}

		protected override BaseReportSettings GetSettings()
		{
			return new BaseReportSettings(ReportCode, ReportCaption);
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_clientCode = (int)GetReportParam("ClientCode");
			_includeProducer = Convert.ToBoolean(GetReportParam("IncludeProducer"));
			_includeQuantity = Convert.ToBoolean(GetReportParam("IncludeQuantity"));
			if (ReportParamExists("CostDiffThreshold"))
				_costDiffThreshold = Convert.ToDecimal(GetReportParam("CostDiffThreshold"));
			if (ReportParamExists("ReportIsFull"))
				_reportIsFull = Convert.ToBoolean(GetReportParam("ReportIsFull"));
			if (ReportParamExists("PriceCode"))
				_priceCode = (int)GetReportParam("PriceCode");
		}

		protected override void GenerateReport()
		{
			CheckPriceCode();

			ProfileHelper.Next("GetOffers");
			GetOffers(_SupplierNoise);

			ProfileHelper.Next("GetData");

			if (_priceCode.HasValue) {
				if (_reportIsFull)
					DataAdapter.SelectCommand.CommandText =
						headersql +
							String.Format(sqlSetParams, _priceCode) +
							sqlFullOffers +
							footersqlByPrice +
							footersql;
				else
					DataAdapter.SelectCommand.CommandText =
						headersql +
							String.Format(sqlSetParams, _priceCode) +
							sqlByPriceCode +
							footersqlByPrice +
							footersql;
			}
			else
				DataAdapter.SelectCommand.CommandText = headersql + sqlWithoutPriceCode + footersql;

			if (_includeProducer)
				DataAdapter.SelectCommand.CommandText += " order by ec.ProductName, ec.ProducerName, Cost;";
			else
				DataAdapter.SelectCommand.CommandText += " order by ec.ProductName, Cost;";

			DataTable resultTable;
			using (var reader = DataAdapter.SelectCommand.ExecuteReader()) {
				ProfileHelper.Next("ProcessData");
				resultTable = FormReportTable(reader);
			}

			resultTable.TableName = "Results";
			_dsReport.Tables.Add(resultTable);
		}

		private void CheckPriceCode()
		{
			if (_priceCode.HasValue) {
				DataAdapter.SelectCommand.CommandText = @"
select
  pd.PriceCode,
  pd.PriceName,
  supps.Name as ShortName,
  count(c.id) as OffersCount
from
  usersettings.PricesData pd
  inner join Customers.suppliers supps on supps.Id = pd.FirmCode
  left join farm.Core0 c on c.PriceCode = pd.PriceCode
where
  pd.PriceCode = " + _priceCode;
				using (var reader = DataAdapter.SelectCommand.ExecuteReader()) {
					if (reader.Read() && !reader.IsDBNull(0)) {
						var priceName = reader.GetString("PriceName");
						var shortName = reader.GetString("ShortName");
						var offersCount = reader.GetUInt32("OffersCount");
						if (!_reportIsFull && offersCount == 0)
							throw new ReportException(
								String.Format(
									"У прайс-листа {0} {1} ({2}) нет предложений.",
									shortName,
									priceName,
									_priceCode));
					}
					else
						throw new ReportException(String.Format("Не найден прайс-лист с кодом: {0}.", _priceCode));
				}
			}
		}

		private DataTable FormReportTable(MySqlDataReader reader)
		{
			var dataTable = new DataTable();
			CustomizeResultTableColumns(dataTable);

			decimal prevCost = 0;

			int prevProductId = -1;
			int prevProducerId = -1;
			int supplierIndex = 0;

			DataRow row = null;
			while (reader.Read()) {
				var productName = Convert.ToString(reader["ProductName"]);
				var productId = Convert.ToInt32(reader["ProductId"]);
				var producerName = Convert.ToString(reader["ProducerName"]);
				var producerId = (reader["ProducerId"] != DBNull.Value) ? Convert.ToInt32(reader["ProducerId"]) : -1;
				var supplierName = Convert.ToString(reader["SupplierName"]);
				var cost = reader["Cost"];

				if (ShouldCreateNewRow(productId, prevProductId, producerId, prevProducerId)) {
					// Стартуем новый Продукт
					prevCost = 0;
					AddRow(dataTable, row);
					row = dataTable.NewRow();

					row["Code"] = reader["Code"];
					row["ProductName"] = productName;
					row["Mnn"] = reader["Mnn"].ToString();
					if (_includeProducer)
						row["ProducerName"] = producerName;

					supplierIndex = 0;
					prevProductId = productId;
					prevProducerId = producerId;
				}

				supplierIndex++;
				CheckSupplierNumb(supplierIndex, dataTable, row);

				row["Supplier" + supplierIndex] = supplierName;
				row["Producer" + supplierIndex] = reader["Producer"].ToString();
				row["Cost" + supplierIndex] = cost;
				if (_includeQuantity)
					row["Quantity" + supplierIndex] = reader["Quantity"].ToString();
				if(String.IsNullOrEmpty(cost.ToString())) {
					prevCost = -1;
				}
				else if (prevCost > 0)
					row["Diff" + supplierIndex] = Math.Round(((Convert.ToDecimal(cost) - prevCost) / prevCost) * 100, 2);
				else {
					prevCost = Convert.ToDecimal(cost);
				}
			}

			AddRow(dataTable, row);

			return dataTable;
		}

		private void AddRow(DataTable dataTable, DataRow row)
		{
			if (row == null)
				return;

			if (dataTable.Columns.Contains("Diff2")
				&& row["Diff2"] != DBNull.Value
				&& Convert.ToDecimal(row["Diff2"]) < _costDiffThreshold)
				return;

			dataTable.Rows.Add(row);
		}

		private bool ShouldCreateNewRow(int productId, int prevProductId, int producerId, int prevProducerId)
		{
			if (_includeProducer)
				return productId != prevProductId || producerId != prevProducerId;
			return productId != prevProductId;
		}

		private void CheckSupplierNumb(int numb, DataTable table, DataRow row)
		{
			if (numb > _suppliersCount) {
				AddNewSupplierColumn(table);

				var newRow = table.NewRow();
				for (int i = 0; i < numb; i++)
					newRow[i] = row[i];
			}
		}

		private void AddNewSupplierColumn(DataTable res)
		{
			_suppliersCount++;

			var dc = res.Columns.Add("Cost" + _suppliersCount, typeof(Decimal));
			dc.Caption = "Цена";
			dc.ExtendedProperties.Add("Width", (int?)6);

			if (_includeQuantity) {
				dc = res.Columns.Add("Quantity" + _suppliersCount, typeof(string));
				dc.Caption = "Остаток";
				dc.ExtendedProperties.Add("Width", (int?)4);
			}

			dc = res.Columns.Add("Producer" + _suppliersCount, typeof(string));
			dc.Caption = "Производитель";
			dc.ExtendedProperties.Add("Width", (int?)15);

			dc = res.Columns.Add("Diff" + _suppliersCount, typeof(double));
			dc.Caption = "Разница %";
			dc.ExtendedProperties.Add("Width", (int?)5);

			dc = res.Columns.Add("Supplier" + _suppliersCount, typeof(String));
			dc.Caption = "Поставщик";
			dc.ExtendedProperties.Add("Width", (int?)8);
		}

		private void CustomizeResultTableColumns(DataTable res)
		{
			var dc = res.Columns.Add("Code");
			dc.Caption = "Код";
			dc.ExtendedProperties.Add("Width", (int?)6);

			dc = res.Columns.Add("ProductName", typeof(String));
			dc.Caption = "Наименование";
			dc.ExtendedProperties.Add("Width", (int?)15);

			dc = res.Columns.Add("Mnn", typeof(String));
			dc.Caption = "Мнн";
			dc.ExtendedProperties.Add("Width", (int?)15);

			if (_includeProducer) {
				dc = res.Columns.Add("ProducerName", typeof(String));
				dc.Caption = "Производитель";
				dc.ExtendedProperties.Add("Width", (int?)15);
			}
		}
	}
}