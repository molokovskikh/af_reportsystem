using System;
using System.Diagnostics;
using System.Data;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using Inforoom.ReportSystem.Filters;
using ExecuteTemplate;
using System.Collections.Generic;
using ReportSystem.Profiling;

namespace Inforoom.ReportSystem
{
	/// <summary>
	/// Summary description for RatingReport.
	/// </summary>
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
			string SelectCommand = "select ";
			foreach (FilterField rf in selectedField)
				if (rf.visible)
					SelectCommand = String.Concat(SelectCommand, rf.primaryField, ", ", rf.viewField, ", ");

			SelectCommand = String.Concat(SelectCommand, @"
Sum(ol.cost*ol.Quantity) as Cost, 
Sum(ol.Quantity) as PosOrder, 
Min(ol.Cost) as MinCost,
Avg(ol.Cost) as AvgCost,
Max(ol.Cost) as MaxCost,
Count(distinct oh.RowId) as DistinctOrderId,
Count(distinct oh.ClientCode) as DistinctClientCode ");
			SelectCommand = String.Concat(
				SelectCommand, @"
from 
  orders.OrdersHead oh 
  join orders.OrdersList ol on  ol.OrderID = oh.RowID
  join catalogs.products p on p.Id = ol.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId
  join catalogs.Producers cfc on cfc.Id = if(ol.CodeFirmCr is not null, ol.CodeFirmCr, 1)
  left join usersettings.clientsdata cd on cd.FirmCode = oh.ClientCode
  left join future.Clients cl on cl.Id = oh.ClientCode
  join usersettings.retclientsset rcs on rcs.ClientCode = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join usersettings.clientsdata prov on prov.FirmCode = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.RegionCode
  join billing.payers on payers.PayerId = IFNULL(cd.BillingCode, cl.PayerId)
where 
      oh.deleted = 0
  and oh.processed = 1
  and IFNULL(cd.BillingCode, cl.PayerId) <> 921
  and rcs.InvisibleOnFirm < 2");

			foreach (FilterField rf in selectedField)
			{
				if (rf.equalValues != null && rf.equalValues.Count > 0)
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetEqualValues());
					if (rf.reportPropertyPreffix == "ClientCode") // Список клиентов особенный т.к. выбирается из двух таблиц
						filter.Add(String.Format("{0}: {1}", rf.equalValuesCaption, GetClientsNamesFromSQL(e, rf.equalValues)));
					else
						filter.Add(String.Format("{0}: {1}", rf.equalValuesCaption, GetValuesFromSQL(e, rf.GetEqualValuesSQL())));
				}
				if ((rf.nonEqualValues != null) && (rf.nonEqualValues.Count > 0))
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetNonEqualValues());
					filter.Add(String.Format("{0}: {1}", rf.nonEqualValuesCaption, GetValuesFromSQL(e, rf.GetNonEqualValuesSQL())));
				}
			}

			if (1 == JunkState)
				SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and (ol.Junk = 0)");
			else
				if (2 == JunkState)
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and (ol.Junk = 1)");

			SelectCommand = String.Concat(SelectCommand, String.Format(Environment.NewLine + "and (oh.WriteTime > '{0}')", dtFrom.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand, String.Format(Environment.NewLine + "and (oh.WriteTime < '{0}')", dtTo.ToString(MySQLDateFormat)));

			//Применяем группировку и сортировку
			List<string> GroupByList = new List<string>();
			foreach (FilterField rf in selectedField)
				if (rf.visible)
				{
					GroupByList.Add(rf.primaryField);
				}
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "group by ", String.Join(",", GroupByList.ToArray()));
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "order by Cost desc");
 
#if DEBUG
			Debug.WriteLine(SelectCommand);
#endif

			DataTable SelectTable = new DataTable();

			e.DataAdapter.SelectCommand.CommandText = SelectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(SelectTable);

			ProfileHelper.Next("Processing2");

			decimal Cost = 0m;
			int PosOrder = 0;
			foreach (DataRow dr in SelectTable.Rows)
			{
				Cost += Convert.ToDecimal(dr["Cost"]);
				PosOrder += Convert.ToInt32(dr["PosOrder"]);
			}

			System.Data.DataTable res = new System.Data.DataTable();
			DataColumn dc;
			foreach (FilterField rf in selectedField)
			{
				if (rf.visible)
				{
					dc = res.Columns.Add(rf.outputField, SelectTable.Columns[rf.outputField].DataType);
					dc.Caption = rf.outputCaption;
					if (rf.width.HasValue)
						dc.ExtendedProperties.Add("Width", rf.width);
				}
			}
			dc = res.Columns.Add("Cost", typeof(System.Decimal));
			dc.Caption = "Сумма";
			dc = res.Columns.Add("CostPercent", typeof(System.Double));
			dc.Caption = "Доля рынка в %";
			dc = res.Columns.Add("PosOrder", typeof(System.Int32));
			dc.Caption = "Заказ";
			dc = res.Columns.Add("PosOrderPercent", typeof(System.Double));
			dc.Caption = "Доля от общего заказа в %";
			dc = res.Columns.Add("MinCost", typeof(System.Decimal));
			dc.Caption = "Минимальная цена";
			dc = res.Columns.Add("AvgCost", typeof(System.Decimal));
			dc.Caption = "Средняя цена";
			dc = res.Columns.Add("MaxCost", typeof(System.Decimal));
			dc.Caption = "Максимальная цена";
			dc = res.Columns.Add("DistinctOrderId", typeof(System.Int32));
			dc.Caption = "Кол-во заявок по препарату";
			dc = res.Columns.Add("DistinctClientCode", typeof(System.Int32));
			dc.Caption = "Кол-во клиентов, заказавших препарат";

			DataRow newrow;
			try
			{
				int visbleCount = selectedField.FindAll(delegate(FilterField x) { return x.visible; }).Count;
				res.BeginLoadData();
				foreach (DataRow dr in SelectTable.Rows)
				{
					newrow = res.NewRow();

					foreach (FilterField rf in selectedField)
						if (rf.visible)
							newrow[rf.outputField] = dr[rf.outputField];

					//Выставляем явно значения определенного типа для полей: "Сумма", "Доля рынка в %" и т.д.
					//(visbleCount * 2) - потому, что участвует код (первичный ключ) и строковое значение,
					//пример: PriceCode и PriceName.
					for (int i = (visbleCount * 2); i < SelectTable.Columns.Count; i++)
					{
						if (!(dr[SelectTable.Columns[i].ColumnName] is DBNull) && res.Columns.Contains(SelectTable.Columns[i].ColumnName))
							newrow[SelectTable.Columns[i].ColumnName] = Convert.ChangeType(dr[SelectTable.Columns[i].ColumnName], res.Columns[SelectTable.Columns[i].ColumnName].DataType);
					}

					newrow["CostPercent"] = Decimal.Round(((decimal)newrow["Cost"] * 100) / Cost, 2);
					newrow["PosOrderPercent"] = Decimal.Round((Convert.ToDecimal(newrow["PosOrder"]) * 100) / Convert.ToDecimal(PosOrder), 2);

					res.Rows.Add(newrow);
				}
			}
			finally
			{
				res.EndLoadData();
			}

			//Добавляем несколько пустых строк, чтобы потом вывести в них значение фильтра в Excel
			for (int i = 0; i < filter.Count; i++)
				res.Rows.InsertAt(res.NewRow(), 0);

			res = res.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
			ProfileHelper.Next("PostProcessing");
		}

		protected override void PostProcessing(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{
			//Замораживаем некоторые колонки и столбцы
			((MSExcel.Range)ws.get_Range("A" + (2 + filter.Count).ToString(), System.Reflection.Missing.Value)).Select();
			exApp.ActiveWindow.FreezePanes = true;
		}

	}
}
