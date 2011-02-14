using System;
using System.Diagnostics;
using System.Data;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using ExecuteTemplate;

namespace Inforoom.ReportSystem
{
	public class RatingReport : OrdersReport
	{
		private const string junkProperty = "JunkState";

		private int JunkState;

		public RatingReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			JunkState = (int)getReportParam(junkProperty);
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("Processing1");

			var selectCommand = BuildSelect();

			selectCommand = String.Concat(selectCommand, @"
Sum(ol.cost*ol.Quantity) as Cost, 
Sum(ol.Quantity) as PosOrder, 
Min(ol.Cost) as MinCost,
Avg(ol.Cost) as AvgCost,
Max(ol.Cost) as MaxCost,
Count(distinct oh.RowId) as DistinctOrderId,
Count(distinct oh.ClientCode) as DistinctClientCode ");
			selectCommand = String.Concat(
				selectCommand, @"
from 
  orders.OrdersHead oh 
  join orders.OrdersList ol on  ol.OrderID = oh.RowID
  join catalogs.products p on p.Id = ol.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
  left join usersettings.clientsdata cd on cd.FirmCode = oh.ClientCode
  left join future.Clients cl on cl.Id = oh.ClientCode
  join usersettings.retclientsset rcs on rcs.ClientCode = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join usersettings.clientsdata prov on prov.FirmCode = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.RegionCode
  join billing.payers on payers.PayerId = IFNULL(cl.PayerId, cd.BillingCode)
where 
	  oh.deleted = 0
  and oh.processed = 1
  and IFNULL(cl.PayerId, cd.BillingCode) <> 921
  and rcs.InvisibleOnFirm < 2");

			selectCommand = ApplyFilters(selectCommand);

			if (1 == JunkState)
				selectCommand = String.Concat(selectCommand, Environment.NewLine + "and (ol.Junk = 0)");
			else if (2 == JunkState)
				selectCommand = String.Concat(selectCommand, Environment.NewLine + "and (ol.Junk = 1)");

			//Применяем группировку и сортировку
			selectCommand = ApplyGroupAndSort(selectCommand, "Cost desc");
 
#if DEBUG
			Debug.WriteLine(selectCommand);
#endif

			var selectTable = new DataTable();
			e.DataAdapter.SelectCommand.CommandText = selectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(selectTable);

			ProfileHelper.Next("Processing2");

			var result = BuildResultTable(selectTable);

			var dc = result.Columns.Add("Cost", typeof (Decimal));
			dc.Caption = "Сумма";
			dc = result.Columns.Add("CostPercent", typeof (Double));
			dc.Caption = "Доля рынка в %";
			dc = result.Columns.Add("PosOrder", typeof (Int32));
			dc.Caption = "Заказ";
			dc = result.Columns.Add("PosOrderPercent", typeof (Double));
			dc.Caption = "Доля от общего заказа в %";
			dc = result.Columns.Add("MinCost", typeof (Decimal));
			dc.Caption = "Минимальная цена";
			dc = result.Columns.Add("AvgCost", typeof (Decimal));
			dc.Caption = "Средняя цена";
			dc = result.Columns.Add("MaxCost", typeof (Decimal));
			dc.Caption = "Максимальная цена";
			dc = result.Columns.Add("DistinctOrderId", typeof (Int32));
			dc.Caption = "Кол-во заявок по препарату";
			dc = result.Columns.Add("DistinctClientCode", typeof (Int32));
			dc.Caption = "Кол-во клиентов, заказавших препарат";

			CopyData(selectTable, result);

			var cost = 0m;
			var posOrder = 0;
			foreach (DataRow dr in selectTable.Rows)
			{
				if (dr["Cost"] == DBNull.Value)
					continue;
				cost += Convert.ToDecimal(dr["Cost"]);
				posOrder += Convert.ToInt32(dr["PosOrder"]);
			}

			foreach (DataRow dr in result.Rows)
			{
				if (dr["Cost"] == DBNull.Value)
					continue;
				dr["CostPercent"] = Decimal.Round((Convert.ToDecimal(dr["Cost"]) * 100) / cost, 2);
				dr["PosOrderPercent"] = Decimal.Round((Convert.ToDecimal(dr["PosOrder"]) * 100) / Convert.ToDecimal(posOrder), 2);
			}

			ProfileHelper.Next("PostProcessing");
		}

		protected override void PostProcessing(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{
			//Замораживаем некоторые колонки и столбцы
			ws.Range["A" + (2 + filterDescriptions.Count), System.Reflection.Missing.Value].Select();
			exApp.ActiveWindow.FreezePanes = true;
		}

	}
}
