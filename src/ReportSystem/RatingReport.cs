using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Data;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using Inforoom.ReportSystem.Filters;
using ExecuteTemplate;
using System.Collections.Generic;

namespace Inforoom.ReportSystem
{
	/// <summary>
	/// Summary description for RatingReport.
	/// </summary>
	public class RatingReport : OrdersReport
	{
		private const string junkProperty = "JunkState";

		private int JunkState;

		public RatingReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary)
			: base(ReportCode, ReportCaption, Conn, Temporary)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			JunkState = (int)getReportParam(junkProperty);

			selectedField = new List<FilterField>();
			foreach (FilterField rf in registredField)
			{
				if (rf.LoadFromDB(this))
					selectedField.Add(rf);
			}

			if (!selectedField.Exists(delegate(FilterField x) { return x.visible; }))
				throw new Exception("Не выбраны поля для отображения в заголовке отчета.");

			selectedField.Sort(delegate(FilterField x, FilterField y) { return (x.position - y.position); });
		}

    	public override void GenerateReport(ExecuteArgs e)
		{
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
  orders.OrdersHead oh, 
  orders.OrdersList ol,
  catalogs.products p,
  catalogs.catalog c,
  catalogs.catalognames cn,
  catalogs.catalogforms cf, 
  farm.CatalogFirmCr cfc, 
  usersettings.clientsdata cd,
  usersettings.retclientsset rcs, 
  farm.regions rg, 
  usersettings.pricesdata pd, 
  usersettings.clientsdata prov,
  billing.payers
where 
    ol.OrderID = oh.RowID 
and oh.deleted = 0
and oh.processed = 1
and p.Id = ol.ProductId
and c.Id = p.CatalogId
and cn.id = c.NameId
and cf.Id = c.FormId
and cfc.CodeFirmCr = if(ol.CodeFirmCr is not null, ol.CodeFirmCr, 1) 
and cd.FirmCode = oh.ClientCode
and cd.BillingCode <> 921
and payers.PayerId = cd.BillingCode
and rcs.ClientCode = oh.ClientCode
and rcs.InvisibleOnFirm < 2 
and rg.RegionCode = oh.RegionCode 
and pd.PriceCode = oh.PriceCode 
and prov.FirmCode = pd.FirmCode");

			foreach (FilterField rf in selectedField)
			{
				if ((rf.equalValues != null) && (rf.equalValues.Count > 0))
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetEqualValues());
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

					for (int i = (visbleCount * 2); i < SelectTable.Columns.Count; i++)
					{
						if (!(dr[SelectTable.Columns[i].ColumnName] is DBNull))
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
		}

		protected override void FreezePanes(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{
			//Замораживаем некоторые колонки и столбцы
			((MSExcel.Range)ws.get_Range("A" + (2 + filter.Count).ToString(), System.Reflection.Missing.Value)).Select();
			exApp.ActiveWindow.FreezePanes = true;
		}

	}
}
