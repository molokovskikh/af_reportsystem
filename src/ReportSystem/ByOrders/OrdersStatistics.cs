using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using DataTable = System.Data.DataTable;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.ByOrders
{
	public class OrdersStatistics : OrdersReport
	{
		public OrdersStatistics(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();

			FilterDescriptions.Remove(FilterDescriptions.First(d => d.StartsWith("Период дат")));
			FilterDescriptions.Insert(0, String.Format("Период дат: {0} - {1} (включительно)", dtFrom.ToString("dd.MM.yyyy"), dtTo.Date.AddDays(-1).ToString("dd.MM.yyyy")));
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next(String.Format("CalculateOrders: dtFrom={0}, dtTo={1}", dtFrom.ToString(), dtTo.ToString()));
			FillFilterDescriptions();
			var sql = @"
SELECT
    supps.Payer PayerId,
    supps.Name SupplierName,
    rg.region,
    round(sum(if(free.ClientPayerId is null, cost * quantity, 0)), 2) OrdersSum,
	count(*) RowCount
FROM Orders.OrdersHead oh
    join usersettings.pricesdata pd on oh.pricecode = pd.pricecode
    join Customers.suppliers supps on pd.firmcode = supps.Id
    join farm.regions rg on oh.regioncode = rg.regioncode
    join Orders.OrdersList ol on ol.orderid = oh.rowid
    join usersettings.retclientsset rcs on rcs.clientcode = oh.clientcode
    join Customers.Users u on u.Id = oh.UserId
    join Customers.Addresses adr on oh.AddressId = adr.Id
    left join billing.FreeOrders free on free.ClientPayerId = adr.PayerId and free.SupplierPayerId = supps.Payer
where
    oh.writetime between ?StartDate and ?EndDate
    and rcs.InvisibleOnFirm < 2
    and rcs.ServiceClient = 0
    and u.PayerId <> 921
    and oh.Deleted = 0
    and oh.Submited = 1
    and rg.RegionCode <> 524288
    and rg.Retail = 0
";
			sql = ApplyUserFilters(sql);
			sql += @"
group by supps.id, rg.regioncode
order by supps.Name, supps.Payer, rg.Region;";

			var dtNewRes = new DataTable();
			dtNewRes.Columns.Add("PayerId", typeof(int));
			dtNewRes.Columns.Add("SupplierName", typeof(string));
			dtNewRes.Columns.Add("Region", typeof(string));
			var column = dtNewRes.Columns.Add("OrdersSum", typeof(decimal));
			column.ExtendedProperties.Add("AsDecimal", "");
			dtNewRes.Columns.Add("RowCount", typeof(int));
			dtNewRes.Columns["PayerId"].Caption = "Код плательщика поставщика";
			dtNewRes.Columns["SupplierName"].Caption = "Поставщик";
			dtNewRes.Columns["Region"].Caption = "Регион";
			dtNewRes.Columns["OrdersSum"].Caption = "Сумма заказов";
			dtNewRes.Columns["RowCount"].Caption = "Количество записей";
			var selectCommand = e.DataAdapter.SelectCommand;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?StartDate", dtFrom);
			selectCommand.Parameters.AddWithValue("?EndDate", dtTo);
			selectCommand.CommandText = sql;
			e.DataAdapter.Fill(dtNewRes);
			ProfileHelper.WriteLine(e.DataAdapter.SelectCommand);
			//Добавляем несколько пустых строк, чтобы потом вывести в них значение фильтра в Excel
			foreach (string t in FilterDescriptions)
				dtNewRes.Rows.InsertAt(dtNewRes.NewRow(), 0);

			var res = dtNewRes.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
		}

		protected override void PostProcessing(Application exApp, _Worksheet ws)
		{
			ws.Range[ws.Cells[1 + FilterDescriptions.Count, 1], ws.Cells[1 + FilterDescriptions.Count, 1]].Select();
		}
	}
}