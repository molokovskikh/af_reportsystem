using System;
using System.Data;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.FastReports
{
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

select  ProductId,
        ProductName,
        ProducerId,
        ProducerName,
        SupplierName,
        min(cost) Cost
  from Core c
       join usersettings.PricesData pd on pd.PriceCode = c.PriceCode
group by ProductId, ProducerId, pd.FirmCode
order by ProductName, ProducerName, Cost;
";
		private int _clientId;
		private int _suppliersCount = 0;

		public PharmacyOffersReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties) 
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			_clientId = (int)getReportParam("ClientCode");
		}

		public override void GenerateReport(ExecuteTemplate.ExecuteArgs e)
		{
			ProfileHelper.Next("GetOffers");
			GetOffers(e, _clientId);

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

			string productName;
			int productId;
			int prevProductId = -1;
			string producerName;
			int producerId;
			int prevProducerId = -1;
			decimal cost = 0;
			int supplierNumb = 0;
			string supplierName;

			DataRow row = null;
			
			while(reader.Read())
			{
				
				productName = Convert.ToString(reader["ProductName"]);
				productId = Convert.ToInt32(reader["ProductId"]);
				producerName = Convert.ToString(reader["ProducerName"]);
				producerId = (reader["ProducerId"] != DBNull.Value) ? Convert.ToInt32(reader["ProducerId"]) : -1;
				supplierName = Convert.ToString(reader["SupplierName"]);
				cost = Convert.ToDecimal(reader["Cost"]);

				if(productId != prevProductId ||
					producerId != prevProducerId)
				{ // Стартуем новый Продукт
					if (row != null)
						dataTable.Rows.Add(row);

					row = dataTable.NewRow();

					row["ProductName"] = productName;
					row["ProducerName"] = producerName;

					supplierNumb = 0;
					prevProductId = productId;
					prevProducerId = producerId;
				}

				supplierNumb++;
				CheckSupplierNumb(supplierNumb, dataTable, row);

				row["Supplier" + supplierNumb] = supplierName;
				row["Cost" + supplierNumb] = cost;
			}

			if (row != null)
				dataTable.Rows.Add(row);

			return dataTable;
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

			DataColumn dc;

			dc = res.Columns.Add("Supplier" + _suppliersCount, typeof(String));
			dc.Caption = "Поставщик";
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("Cost" + _suppliersCount, typeof(Decimal));
			dc.Caption = "Цена";
			dc.ExtendedProperties.Add("Width", (int?)6);
		}

		private void CustomizeResultTableColumns(DataTable res)
		{
			DataColumn dc;

			dc = res.Columns.Add("ProductName", typeof(String));
			dc.Caption = "Наименование";
			dc.ExtendedProperties.Add("Width", (int?) 15);

			dc = res.Columns.Add("ProducerName", typeof(String));
			dc.Caption = "Производитель";
			dc.ExtendedProperties.Add("Width", (int?) 15);
		}
	}
}
