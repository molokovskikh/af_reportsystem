using System;
using System.Data;
using Inforoom.ReportSystem.Helpers;
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
		private const string sql = @"
drop temporary table IF EXISTS ExtendedCore;
create temporary table ExtendedCore
(
  id bigint unsigned,
  ProductName VARCHAR(255),
  ProducerId INT UNSIGNED,
  ProducerName VARCHAR(255),
  SupplierName VARCHAR(255),
  INDEX (id)
) engine=MEMORY;

insert into ExtendedCore (Id) select Id from Core;

update 
  ExtendedCore ec
  inner join farm.Core0 on Core0.id = ec.Id
  inner join usersettings.PricesData pd on pd.PriceCode = Core0.PriceCode
  join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
  left join catalogs.Producers on Producers.ID = Core0.CodeFirmCr
set
  ec.ProducerId = Core0.CodeFirmCr,
  ec.ProducerName = Producers.Name,
  ec.SupplierName = cd.ShortName;

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

select  c.ProductId,
        ec.ProductName,
		m.RussianMnn as Mnn,
        ec.ProducerId,
        ec.ProducerName,
        ec.SupplierName,
        min(c.cost) Cost,
		c0.Quantity,
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
group by c.ProductId, ec.ProducerId, pd.FirmCode

";
		private bool _includeQuantity;
		private bool _includeProducer;
		private decimal _costDiffThreshold;
		private int _suppliersCount = 0;
		private bool _reportIsFull;
		private int? _priceCode;

		public PharmacyOffersReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties) 
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			
		}

		public override void ReadReportParams()
		{
			_clientCode = (int)getReportParam("ClientCode");
			_includeProducer = Convert.ToBoolean(getReportParam("IncludeProducer"));
			_includeQuantity = Convert.ToBoolean(getReportParam("IncludeQuantity"));
			if (reportParamExists("CostDiffThreshold"))
				_costDiffThreshold = Convert.ToDecimal(getReportParam("CostDiffThreshold"));
			if (reportParamExists("ReportIsFull"))
				_reportIsFull = Convert.ToBoolean(getReportParam("ReportIsFull"));
			if (reportParamExists("PriceCode"))
				_priceCode = (int)getReportParam("PriceCode");
		}

		public override void GenerateReport(ExecuteTemplate.ExecuteArgs e)
		{
			base.GenerateReport(e);
			ProfileHelper.Next("GetOffers");
			GetOffers(e);

			ProfileHelper.Next("GetData");
			e.DataAdapter.SelectCommand.CommandText = sql;
			if (_includeProducer)
				e.DataAdapter.SelectCommand.CommandText += "order by ec.ProductName, ec.ProducerName, Cost;";
			else
				e.DataAdapter.SelectCommand.CommandText += "order by ec.ProductName, Cost;";

			DataTable resultTable;
			using (var reader = e.DataAdapter.SelectCommand.ExecuteReader())
			{
				ProfileHelper.Next("ProcessData");
				resultTable = FormReportTable(reader);
			}

			resultTable.TableName = "Results";
			_dsReport.Tables.Add(resultTable);
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
			while(reader.Read())
			{
				var productName = Convert.ToString(reader["ProductName"]);
				var productId = Convert.ToInt32(reader["ProductId"]);
				var producerName = Convert.ToString(reader["ProducerName"]);
				var producerId = (reader["ProducerId"] != DBNull.Value) ? Convert.ToInt32(reader["ProducerId"]) : -1;
				var supplierName = Convert.ToString(reader["SupplierName"]);
				var cost = Convert.ToDecimal(reader["Cost"]);

				if(ShouldCreateNewRow(productId, prevProductId, producerId, prevProducerId))
				{ // Стартуем новый Продукт
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
				row["Cost" + supplierIndex] = cost;
				row["Producer" + supplierIndex] = reader["Producer"].ToString();
				if (_includeQuantity)
					row["Quantity" + supplierIndex] = reader["Quantity"].ToString();
				if (prevCost > 0)
					row["Diff" + supplierIndex] = Math.Round(((cost - prevCost) / prevCost) *100, 2);
				prevCost = cost;
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
			if (numb > _suppliersCount)
			{
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

			if (_includeQuantity)
			{
				dc = res.Columns.Add("Quantity" + _suppliersCount, typeof (string));
				dc.Caption = "Остаток";
				dc.ExtendedProperties.Add("Width", (int?) 4);
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
			dc.ExtendedProperties.Add("Width", (int?) 6);

			dc = res.Columns.Add("ProductName", typeof(String));
			dc.Caption = "Наименование";
			dc.ExtendedProperties.Add("Width", (int?) 15);

			dc = res.Columns.Add("Mnn", typeof(String));
			dc.Caption = "Мнн";
			dc.ExtendedProperties.Add("Width", (int?) 15);

			if (_includeProducer)
			{
				dc = res.Columns.Add("ProducerName", typeof (String));
				dc.Caption = "Производитель";
				dc.ExtendedProperties.Add("Width", (int?) 15);
			}
		}
	}
}
