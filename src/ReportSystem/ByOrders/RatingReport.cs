using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using ExecuteTemplate;
using DataTable = System.Data.DataTable;
using XlChartType = Microsoft.Office.Interop.Excel.XlChartType;

namespace Inforoom.ReportSystem
{
	public class RatingReport : OrdersReport
	{
		public int JunkState { get; set; }
		public bool BuildChart { get; set; }
		public bool DoNotShowAbsoluteValues { get; set; }
		public List<ulong> ProductFromPriceEqual { get; set; }

		public DataTable ResultTable
		{
			get { return _dsReport.Tables["Results"]; }
		}

		public RatingReport(ulong reportCode, string reportCaption, MySqlConnection conn, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, conn, format, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();

			foreach (var property in GetType().GetProperties()) {
				if (reportParamExists(property.Name)) {
					property.SetValue(this, getReportParam(property.Name), null);
				}
			}
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("Processing1");

			var selectCommand = BuildSelect();
			if (firmCrPosition)
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
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
  left join Customers.Clients cl on cl.Id = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join Customers.suppliers prov on prov.Id = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join Customers.addresses ad on oh.AddressId = ad.Id
  join billing.LegalEntities le on ad.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId
where 1=1", OrdersSchema));

			selectCommand = ApplyFilters(selectCommand);

			if (1 == JunkState)
				selectCommand = String.Concat(selectCommand, Environment.NewLine + "and (ol.Junk = 0)");
			else if (2 == JunkState)
				selectCommand = String.Concat(selectCommand, Environment.NewLine + "and (ol.Junk = 1)");

			// обрабатываем параметр Список значений "Наименование продукта" из прайс-листа
			if(ProductFromPriceEqual != null) {
				selectCommand = String.Concat(selectCommand, Environment.NewLine +
					String.Format("and exists (select * from farm.Core0 cr where cr.ProductId = p.Id and cr.pricecode in ({0}))",
						String.Join(",", ProductFromPriceEqual.ToArray())));
				if(reportParamExists("FirmCrPosition")) {
					selectCommand = String.Concat(selectCommand, Environment.NewLine +
						String.Format("and exists (select * from farm.Core0 cr join farm.synonymfirmcr fcr on cr.SynonymFirmCrCode=fcr.SynonymFirmCrCode" +
							" where fcr.CodeFirmCr = cfc.Id and cr.pricecode in ({0}))",
							String.Join(",", ProductFromPriceEqual.ToArray())));
				}
			}

			//Применяем группировку и сортировку
			selectCommand = ApplyGroupAndSort(selectCommand, "Cost desc");
			if (firmCrPosition) {
				var groupPart = selectCommand.Substring(selectCommand.IndexOf("group by"));
				var new_groupPart = groupPart.Replace("cfc.Id", "cfc_id");
				selectCommand = selectCommand.Replace(groupPart, new_groupPart);
			}

#if DEBUG
			Debug.WriteLine(selectCommand);
#endif

			var selectTable = new DataTable();
			e.DataAdapter.SelectCommand.CommandText = selectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(selectTable);

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
			ws.Range["A" + (2 + FilterDescriptions.Count), System.Reflection.Missing.Value].Select();
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