using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Inforoom.ReportSystem.Filters;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using DataTable = System.Data.DataTable;
using XlChartType = Microsoft.Office.Interop.Excel.XlChartType;
using System.Linq;

namespace Inforoom.ReportSystem
{
	public class RatingReport : BaseOrdersReport
	{
		public int JunkState { get; set; }
		public bool BuildChart { get; set; }
		public bool DoNotShowAbsoluteValues { get; set; }
		public List<ulong> ProductFromPriceEqual { get; set; }

		public DataTable ResultTable => _dsReport.Tables["Results"];

		public RatingReport()
		{
			Init();
		}

		public RatingReport(MySqlConnection conn, DataSet dsProperties)
			: base(conn, dsProperties)
		{
			Init();
			//это поле не настраивается в интерфейсе и формируется только
			//в конструкторе который используется для обработки отчетов
			RegistredField.Add(new FilterField {
				primaryField = "tpc.CodeCore",
				viewField = "tpc.CodeCore as ProductCodeCore",
				outputField = "ProductCodeCore",
				reportPropertyPreffix = "ProductCodeCore",
				outputCaption = "Код",
				position = -1
			});
		}

		private void Init()
		{
			RegistredField.Add(new FilterField {
				primaryField = "ol.SynonymCode",
				viewField = "if(s.SynonymCode is not null, s.Synonym, sa.Synonym) as SupplierProductName",
				outputField = "SupplierProductName",
				reportPropertyPreffix = "SupplierProductName",
				outputCaption = "Оригинальное наименование товара",
				position = 9
			});
			RegistredField.Add(new FilterField {
				primaryField = "ol.SynonymFirmCrCode",
				viewField = "sfc.Synonym as SupplierProducerName",
				outputField = "SupplierProducerName",
				reportPropertyPreffix = "SupplierProducerName",
				outputCaption = "Оригинальное наименование производителя",
				position = 10
			});
		}

		// #51954 Доработка рейтингового отчета
		public override void CheckAfterLoadFields()
		{
			base.CheckAfterLoadFields();
			if (ProductFromPriceEqual != null) {
				var productCodeCore = RegistredField.Single(f => f.outputField == "ProductCodeCore");
				productCodeCore.visible = true;
				selectedField.Add(productCodeCore);
			}
		}

		protected override void GenerateReport()
		{
			ProfileHelper.Next("Processing1");

			var selectCommand = BuildSelect();
			if (IncludeProducerName)
				selectCommand = selectCommand.Replace("cfc.Id", "if(c.Pharmacie = 1, cfc.Id, 0) as cfc_id")
					.Replace("cfc.Name", "if(c.Pharmacie = 1, cfc.Name, 'Нелекарственный ассортимент')");

			selectCommand = String.Concat(selectCommand, String.Format(@"
Sum(ol.cost*ol.Quantity) as Cost,
Sum(ol.Quantity) as PosOrder,
Min(ol.Cost) as MinCost,
Avg(ol.Cost) as AvgCost,
Max(ol.Cost) as MaxCost,
Count(distinct oh.RowId) as DistinctOrderId,
Count(distinct oh.AddressId) as DistinctAddressId
from {0}.OrdersHead oh
  join {0}.OrdersList ol on ol.OrderID = oh.RowID
  join catalogs.products p on p.Id = ol.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId
  left join catalogs.mnn m on cn.MnnId = m.Id
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
  left join Customers.Clients cl on cl.Id = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join Customers.suppliers prov on prov.Id = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join Customers.addresses ad on oh.AddressId = ad.Id
  join billing.LegalEntities le on ad.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId
  left join Farm.Synonym as s on s.SynonymCode = ol.SynonymCode
  left join Farm.SynonymArchive as sa on sa.SynonymCode = ol.SynonymCode
  left join Farm.SynonymFirmCr as sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode", OrdersSchema));

			// обрабатываем параметр Список значений "Наименование продукта" из прайс-листа без учета производителя
			if (ProductFromPriceEqual != null && !IncludeProducerName)
			{
				selectCommand = String.Concat(String.Format(@"drop temporary table if exists tempGetProductCode;
					create temporary table tempGetProductCode(ProductId INT(10) UNSIGNED, CodeCore varchar(100), INDEX USING HASH (ProductId))
					engine=memory
					select cr.ProductId, GROUP_CONCAT(DISTINCT cr.Code ORDER BY cr.Code) as CodeCore
					from farm.Core0 cr
					where cr.pricecode in ({0})
					group by cr.ProductId;",
					String.Join(",", ProductFromPriceEqual.ToArray())), Environment.NewLine, selectCommand);
				selectCommand = String.Concat(selectCommand, Environment.NewLine, "join tempGetProductCode tpc on tpc.ProductId = p.Id");
			}

			// обрабатываем параметр Список значений "Наименование продукта" из прайс-листа с учетом производителя
			if (ProductFromPriceEqual != null && IncludeProducerName)
			{
				selectCommand = String.Concat(String.Format(@"drop temporary table if exists tempGetProductCode;
					create temporary table tempGetProductCode(ProductId INT(10) UNSIGNED, ProducerId INT(11) UNSIGNED,
					CodeCore varchar(100), INDEX USING HASH (ProductId, ProducerId))
					engine=memory
					select cr.ProductId, fcr.CodeFirmCr as ProducerId, GROUP_CONCAT(DISTINCT cr.Code ORDER BY cr.Code) as CodeCore
					from farm.Core0 cr
					join farm.synonymfirmcr fcr on fcr.SynonymFirmCrCode = cr.SynonymFirmCrCode
					where cr.pricecode in ({0})
					group by cr.ProductId, fcr.CodeFirmCr;",
					String.Join(",", ProductFromPriceEqual.ToArray())), Environment.NewLine, selectCommand);
				selectCommand = String.Concat(selectCommand, Environment.NewLine, "join tempGetProductCode tpc on tpc.ProductId = p.Id and tpc.ProducerId = cfc.Id");
			}

			selectCommand = String.Concat(selectCommand, Environment.NewLine, "where pd.IsLocal = 0");
			selectCommand = ApplyFilters(selectCommand);

			if (1 == JunkState)
				selectCommand = String.Concat(selectCommand, Environment.NewLine + "and (ol.Junk = 0)");
			else if (2 == JunkState)
				selectCommand = String.Concat(selectCommand, Environment.NewLine + "and (ol.Junk = 1)");

			//Применяем группировку и сортировку
			selectCommand = ApplyGroupAndSort(selectCommand, "Cost desc;");
			if (IncludeProducerName) {
				var search = "group by";
				if (ProductFromPriceEqual != null)
					search = "group by tpc.CodeCore";
				var groupPart = selectCommand.Substring(selectCommand.IndexOf(search));
				var new_groupPart = groupPart.Replace("cfc.Id", "cfc_id");
				selectCommand = selectCommand.Replace(groupPart, new_groupPart);
			}

			// удалили временую таблицу
			if (ProductFromPriceEqual != null)
				selectCommand = String.Concat(selectCommand, Environment.NewLine, "drop temporary table if exists tempGetProductCode;");

#if DEBUG
			Debug.WriteLine(selectCommand);
#endif

			var selectTable = new DataTable();
			DataAdapter.SelectCommand.CommandText = selectCommand;
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.Fill(selectTable);

			ProfileHelper.Next("Processing2");

			var result = BuildResultTable(selectTable);

			DataColumn dc;
			dc = result.Columns.Add("Cost", typeof(Decimal));
			dc.Caption = "Сумма";

			dc = result.Columns.Add("CostPercent", typeof(Double));
			dc.Caption = "Доля рынка в %";

			dc = result.Columns.Add("PosOrder", typeof(Int32));
			dc.Caption = "Заказ";

			dc = result.Columns.Add("PosOrderPercent", typeof(Double));
			dc.Caption = "Доля от общего заказа в %";

			if (!DoNotShowAbsoluteValues) {
				dc = result.Columns.Add("MinCost", typeof(Decimal));
				dc.Caption = "Минимальная цена";
				dc = result.Columns.Add("AvgCost", typeof(Decimal));
				dc.Caption = "Средняя цена";
				dc = result.Columns.Add("MaxCost", typeof(Decimal));
				dc.Caption = "Максимальная цена";
				dc = result.Columns.Add("DistinctOrderId", typeof(Int32));
				dc.Caption = "Кол-во заявок по препарату";
				dc = result.Columns.Add("DistinctAddressId", typeof(Int32));
				dc.Caption = "Кол-во адресов доставки, заказавших препарат";
			}

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

			//эти колонки нужны для вычисления результата
			//но отображаться они не должны по этому удаляем
			if (DoNotShowAbsoluteValues) {
				result.Columns.Remove("Cost");
				result.Columns.Remove("PosOrder");
			}

			ProfileHelper.Next("PostProcessing");
		}

		protected override void PostProcessing(Application exApp, _Worksheet ws)
		{
			//Замораживаем некоторые колонки и столбцы
			ws.Range["A" + (2 + Header.Count), System.Reflection.Missing.Value].Select();
			exApp.ActiveWindow.FreezePanes = true;

			if (BuildChart) {
				var result = _dsReport.Tables["Results"];

				var firstDataRowIndex = 1 + EmptyRowCount;
				var lastDataRowIndex = 1 + result.Rows.Count;
				ws.Range[ws.Cells[firstDataRowIndex, 1], ws.Cells[lastDataRowIndex, 2]].Select();

				var range = ((Range)ws.Cells[firstDataRowIndex, result.Columns.Count + 1]);
				var top = Convert.ToSingle(range.Top);
				var left = Convert.ToSingle(range.Left);

				var shape = ws.Shapes.AddChart(XlChartType.xlPie, left, top, 600, 450);
			}
		}
	}
}