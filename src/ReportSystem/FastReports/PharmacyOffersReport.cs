using System;
using System.Collections.Generic;
using System.Data;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.FastReports
{
	public enum GroupOffersBy
	{
		Product,
		ProductAndProducer
	}

	public class PharmacyOffersReport : BaseFastReport
	{
		private const string sql =
			@"
alter table Core add ProductName VARCHAR(255);
alter table Core add ProducerId INT UNSIGNED;
alter table Core add ProducerName VARCHAR(255);
alter table Core add SupplierName VARCHAR(255);

update Core cor set
  ProductName = (select concat(cat.Name, ' ',
				 ifnull(GROUP_CONCAT(ifnull(PropertyValues.Value, '')
									order by Properties.PropertyName, PropertyValues.Value
									SEPARATOR ', '), ''))
			  from
				 catalogs.products inp
         join catalogs.Catalog cat on cat.Id = inp.CatalogId
				 left join catalogs.ProductProperties on ProductProperties.ProductId = inp.Id
				 left join catalogs.PropertyValues on PropertyValues.Id = ProductProperties.PropertyValueId
				 left join catalogs.Properties on Properties.Id = PropertyValues.PropertyId
			   where inp.Id = cor.ProductId),
  ProducerId = (select CodeFirmCr from farm.Core0 where id = cor.Id),
  SupplierName = (select ShortName 
                    from usersettings.PricesData pd 
                         join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
                    where pd.PriceCode = cor.PriceCode);

update Core cor set
  ProducerName = (select Name from catalogs.Producers where ID = cor.ProducerId);

select  c.ProductId,
        c.ProductName,
        c.ProducerId,
        c.ProducerName,
        c.SupplierName,
        min(c.cost) Cost,
		c0.Quantity,
		sfc.Synonym as Producer,
		c0.Code
from Core c
	join farm.Core0 c0 on c0.Id = c.Id
	left join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c0.SynonymFirmCrCode
	join usersettings.PricesData pd on pd.PriceCode = c.PriceCode
group by c.ProductId, c.ProducerId, pd.FirmCode
order by c.ProductName, c.ProducerName, Cost;
";
		private bool _includeQuantity;
		private bool _includeProducer;
		private decimal _costDiffTheshold;
		private int _suppliersCount = 0;

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
				_costDiffTheshold = Convert.ToDecimal(getReportParam("CostDiffThreshold"));
		}

		public override void GenerateReport(ExecuteTemplate.ExecuteArgs e)
		{
			ProfileHelper.Next("GetOffers");
			GetOffers(e);

			ProfileHelper.Next("GetData");
			e.DataAdapter.SelectCommand.CommandText = sql;
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

			if (dataTable.Columns.Contains("Diff1")
				&& row["Diff1"] != DBNull.Value
				&& Convert.ToDecimal(row["Diff1"]) < _costDiffTheshold)
				return;

			dataTable.Rows.Add(row);
		}

		private bool ShouldCreateNewRow(int productId, int prevProductId, int producerId, int prevProducerId)
		{
			if (_includeProducer)
				return productId != prevProductId || producerId != prevProducerId;
			else
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
				row = newRow;
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

			dc = res.Columns.Add("Diff" + _suppliersCount, typeof(string));
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

			if (_includeProducer)
			{
				dc = res.Columns.Add("ProducerName", typeof (String));
				dc.Caption = "Производитель";
				dc.ExtendedProperties.Add("Width", (int?) 15);
			}
		}
	}
}
