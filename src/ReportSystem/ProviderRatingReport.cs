using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Reflection;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using Inforoom.ReportSystem.Filters;
using ExecuteTemplate;
using Microsoft.Office.Core;

namespace Inforoom.ReportSystem
{
	public class ProviderRatingReport : OrdersReport
	{
		private const string providerCountProperty = "ProviderCount";

		private int providerCount;

		public ProviderRatingReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary)
			: base(ReportCode, ReportCaption, Conn, Temporary)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();

			providerCount = (int)getReportParam(providerCountProperty);
			if (providerCount <= 0)
				throw new Exception(String.Format("Некорректно задан параметр 'Кол-во поставщиков': {0}", providerCount));
		}

		protected override void CheckAfterLoadFields()
		{
			//Если поле поставщик не в выбранных параметрах, то добавляем его туда и устанавливаем "visible в true"
			var provideNameField = selectedField.Find(delegate(FilterField value) { return value.reportPropertyPreffix == "FirmCode"; });
			if (provideNameField == null)
			{
				provideNameField = registredField.Find(delegate(FilterField value) { return value.reportPropertyPreffix == "FirmCode"; });
				selectedField.Add(provideNameField);
			}
			provideNameField.visible = true;
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			string SelectCommand = "select ";
			foreach (FilterField rf in selectedField)
				if (rf.visible)
					SelectCommand = String.Concat(SelectCommand, rf.primaryField, ", ", rf.viewField, ", ");

			SelectCommand = String.Concat(SelectCommand, @"
Sum(ol.cost*ol.Quantity) as Summ ");
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
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "order by Summ desc");

#if DEBUG
			Debug.WriteLine(SelectCommand);
#endif

			DataTable SelectTable = new DataTable();

			e.DataAdapter.SelectCommand.CommandText = SelectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(SelectTable);

			decimal AllSumm = 0m;
			decimal OtherSumm = 0m;
			int currentCount = 0;
			foreach (DataRow dr in SelectTable.Rows)
			{
				currentCount++;
				AllSumm += Convert.ToDecimal(dr["Summ"]);
				if (currentCount > providerCount)
					OtherSumm += Convert.ToDecimal(dr["Summ"]);
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
			dc = res.Columns.Add("SummPercent", typeof(System.Double));
			dc.Caption = "Доля рынка в %";

			DataRow newrow;
			try
			{
				int visbleCount = selectedField.FindAll(delegate(FilterField x) { return x.visible; }).Count;
				res.BeginLoadData();
				currentCount = 0;
				foreach (DataRow dr in SelectTable.Rows)
				{
					currentCount++;
					newrow = res.NewRow();

					newrow["FirmShortName"] = dr["FirmShortName"];

					newrow["SummPercent"] = Decimal.Round(((decimal)dr["Summ"] * 100) / AllSumm, 2);

					res.Rows.Add(newrow);

					if (currentCount == providerCount)
						break;
				}

				if (OtherSumm > 0)
				{
					newrow = res.NewRow();
					newrow["FirmShortName"] = "Остальные";
					newrow["SummPercent"] = Decimal.Round((OtherSumm * 100) / AllSumm, 2);
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

		protected override void PostProcessing(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{
			DataTable res = _dsReport.Tables["Results"];

			//Выбираем диапазон, по которому будет строить диаграму
			((MSExcel.Range)ws.get_Range(ws.Cells[2 + filter.Count, 1], ws.Cells[res.Rows.Count + 1, 2])).Select();
			MSExcel.Shape s;
			s = ws.Shapes.AddChart(MSExcel.XlChartType.xlPie, 20, 40, 600, 250);
			
			//Устанавливаем диаграмму справа от таблицы
			s.Top = Convert.ToSingle(((MSExcel.Range)ws.Cells[2 + filter.Count, 4]).Top);
			s.Left = Convert.ToSingle(((MSExcel.Range)ws.Cells[2 + filter.Count, 4]).Left);

			//Производим подсчет высоты легенды, чтобы она полностью отобразилась на диаграмме
			double legendHeight = 0;
			for (int i = 1; i <= ((MSExcel.LegendEntries)s.Chart.Legend.LegendEntries(Type.Missing)).Count; i++)
				legendHeight += ((MSExcel.LegendEntry)s.Chart.Legend.LegendEntries(i)).Height;

			legendHeight = legendHeight * 1.7;

			if (legendHeight > s.Height)
				s.Height = Convert.ToSingle(legendHeight) + 10;

			//Отображаем диаграмму
			s.Fill.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
		}


	}
}
