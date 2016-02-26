using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.Model
{
	[Description("Статистика накладных")]
	public class WaybillsStatReport : OrdersReport
	{
		public WaybillsStatReport()
		{
			Init();
		}

		public WaybillsStatReport(ulong reportCode, string reportCaption, MySqlConnection conn, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, conn, format, dsProperties)
		{
			Init();
		}

		[Description("Показывать код поставщика")]
		public bool ShowCode { get; set; }

		[Description("Показывать код изготовителя")]
		public bool ShowCodeCr { get; set; }

		[Description("Поставщик")]
		public int SupplierId { get; set; }

		private void Init()
		{
			//накладные не связаны с прайс-листами
			registredField.Remove(registredField.First(f => f.primaryField == "pd.PriceCode"));
			registredField.Remove(registredField.First(f => f.primaryField == "ol.Code"));
		}

		protected override void GenerateReport()
		{
			var join = CalculateSupplierIds(SupplierId, ShowCode, ShowCodeCr);

			var sql = new StringBuilder();
			var selectCommand = BuildSelect();
			if (IncludeProducerName)
				selectCommand = selectCommand.Replace("cfc.Id", "if(c.Pharmacie = 1, cfc.Id, 0) as cfc_id")
					.Replace("cfc.Name", "if(c.Pharmacie = 1, cfc.Name, 'Нелекарственный ассортимент')");
			sql.AppendLine(selectCommand);

			if (ShowCode)
				sql.AppendLine("ProviderCodes.Code,");
			if (ShowCodeCr)
				sql.AppendLine("ProviderCodes.CodeCr,");

			sql.AppendLine(String.Format(@"
	Sum(db.SupplierCost * db.Quantity) as Cost,
	Sum(db.Quantity) as PosOrder,
	Min(db.SupplierCost) as MinCost,
	Avg(db.SupplierCost) as AvgCost,
	Max(db.SupplierCost) as MaxCost,
	Count(distinct dh.Id) as DistinctWaybillsId,
	Count(distinct dh.AddressId) as DistinctAddressId
from Documents.DocumentHeaders dh
	join Documents.DocumentBodies db on db.DocumentId = dh.Id
	join catalogs.products p on p.Id = db.ProductId
	join catalogs.catalog c on c.Id = p.CatalogId
	join catalogs.catalognames cn on cn.id = c.NameId
	join catalogs.catalogforms cf on cf.Id = c.FormId
	left join catalogs.mnn m on cn.MnnId = m.Id
	left join catalogs.Producers cfc on cfc.Id = db.ProducerId
	left join Customers.Clients cl on cl.Id = dh.ClientCode
	join farm.regions rg on rg.RegionCode = cl.RegionCode
	join Customers.suppliers prov on prov.Id = dh.FirmCode
	join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
	join Customers.addresses ad on dh.AddressId = ad.Id
	join billing.LegalEntities le on ad.LegalEntityId = le.Id
	join billing.Payers on Payers.PayerId = le.PayerId
	{0}
where 1 = 1", join));

			var commandText = ApplyFilters(sql.ToString(), "dh");
			commandText = ApplyGroupAndSort(commandText, "Cost desc");

			var selectTable = new DataTable();
			DataAdapter.SelectCommand.CommandText = commandText;
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.Fill(selectTable);

			var result = BuildResultTable(selectTable);

			DataColumn dc;
			if (ShowCode) {
				dc = result.Columns.Add("Code", typeof(String));
				dc.Caption = "Код";
				dc.SetOrdinal(0);
			}

			if (ShowCodeCr) {
				dc = result.Columns.Add("CodeCr", typeof(String));
				dc.Caption = "Код изготовителя";
				dc.SetOrdinal(1);
			}
			dc = result.Columns.Add("Cost", typeof(Decimal));
			dc.Caption = "Сумма";

			dc = result.Columns.Add("CostPercent", typeof(Double));
			dc.Caption = "Доля рынка в %";

			dc = result.Columns.Add("PosOrder", typeof(Int32));
			dc.Caption = "Заказ";

			dc = result.Columns.Add("PosOrderPercent", typeof(Double));
			dc.Caption = "Доля от общего заказа в %";

			dc = result.Columns.Add("MinCost", typeof(Decimal));
			dc.Caption = "Минимальная цена";
			dc = result.Columns.Add("AvgCost", typeof(Decimal));
			dc.Caption = "Средняя цена";
			dc = result.Columns.Add("MaxCost", typeof(Decimal));
			dc.Caption = "Максимальная цена";
			dc = result.Columns.Add("DistinctWaybillsId", typeof(Int32));
			dc.Caption = "Кол-во заявок по препарату";
			dc = result.Columns.Add("DistinctAddressId", typeof(Int32));
			dc.Caption = "Кол-во адресов доставки, заказавших препарат";

			CopyData(selectTable, result);

			var cost = 0m;
			var posOrder = 0;
			foreach (DataRow dr in selectTable.Rows) {
				if (dr["Cost"] == DBNull.Value)
					continue;
				cost += Convert.ToDecimal(dr["Cost"]);
				posOrder += Convert.ToInt32(dr["PosOrder"]);
			}

			foreach (DataRow dr in result.Rows) {
				if (dr["Cost"] == DBNull.Value)
					continue;
				dr["CostPercent"] = Decimal.Round((Convert.ToDecimal(dr["Cost"]) * 100) / cost, 2);
				dr["PosOrderPercent"] = Decimal.Round((Convert.ToDecimal(dr["PosOrder"]) * 100) / Convert.ToDecimal(posOrder), 2);
			}
		}
	}
}