using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using DataTable = System.Data.DataTable;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.ByOrders
{
	public class OrdersStatistics : OrdersReport
	{
		protected const string reportIntervalProperty = "ReportInterval";

		public OrdersStatistics(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			ByPreviousMonth = (bool)getReportParam(byPreviousMonthProperty);
			if (Interval) {
				dtFrom = From;
				dtTo = To;
				dtTo = dtTo.Date.AddDays(1);
			}
			else if (ByPreviousMonth) {
				dtTo = DateTime.Now;
				dtTo = dtTo.AddDays(-(dtTo.Day - 1)).Date; // Первое число текущего месяца
				dtFrom = dtTo.AddMonths(-1).Date;
			}
			else {
				_reportInterval = (int)getReportParam(reportIntervalProperty);
				dtTo = DateTime.Now.Date;
				dtFrom = dtTo.AddDays(-_reportInterval).Date;
			}
			FilterDescriptions = new List<string>();
			FilterDescriptions.Add(String.Format("Период дат: {0} - {1} (включительно)", dtFrom.ToString("dd.MM.yyyy"), dtTo.Date.AddDays(-1).ToString("dd.MM.yyyy")));
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next(String.Format("CalculateOrders: dtFrom={0}, dtTo={1}", dtFrom.ToString(), dtTo.ToString()));
			var selectCommand = e.DataAdapter.SelectCommand;
			selectCommand.CommandText = "orders.CalculateOrders"; // в ХП в фильтре по регионам указаны платные регионы
			selectCommand.CommandType = CommandType.StoredProcedure;
			selectCommand.Parameters.Clear();
			selectCommand.Parameters.AddWithValue("?StartDate", dtFrom);
			selectCommand.Parameters.AddWithValue("?EndDate", dtTo);

			var dtNewRes = new DataTable();
			dtNewRes.Columns.Add("PayerId", typeof(int));
			dtNewRes.Columns.Add("SupplierName", typeof(string));
			dtNewRes.Columns.Add("Region", typeof(string));
			var column = dtNewRes.Columns.Add("OrdersSum", typeof(decimal));
			column.ExtendedProperties.Add("AsDecimal", "");
			dtNewRes.Columns["PayerId"].Caption = "Код плательщика поставщика";
			dtNewRes.Columns["SupplierName"].Caption = "Поставщик";
			dtNewRes.Columns["Region"].Caption = "Регион";
			dtNewRes.Columns["OrdersSum"].Caption = "Сумма заказов";
			e.DataAdapter.Fill(dtNewRes);
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