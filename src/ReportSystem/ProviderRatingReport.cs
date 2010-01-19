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
using ReportSystem.Profiling;

namespace Inforoom.ReportSystem
{
	public class ProviderRatingReport : OrdersReport
	{
		private const string providerCountProperty = "ProviderCount";

		private int providerCount;

		public ProviderRatingReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();

			providerCount = (int)getReportParam(providerCountProperty);
			if (providerCount <= 0)
				throw new ReportException(String.Format("Некорректно задан параметр 'Кол-во поставщиков': {0}", providerCount));
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
			ProfileHelper.Next("Processing1");
			string SelectCommand = "select ";
			foreach (FilterField rf in selectedField)
				if (rf.visible)
					SelectCommand = String.Concat(SelectCommand, rf.primaryField, ", ", rf.viewField, ", ");

			SelectCommand = String.Concat(SelectCommand, @"
Sum(ol.cost*ol.Quantity) as Summ ");
			SelectCommand = String.Concat(
				SelectCommand, @"
from 
  orders.OrdersHead oh 
  join orders.OrdersList ol on ol.OrderID = oh.RowID
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
  and payers.PayerId <> 921
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

			ProfileHelper.Next("Processing2");

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
			ProfileHelper.End();
		}

		protected override void PostProcessing(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{
			ProfileHelper.Next("ExcelDiagrammProcessing");
			DataTable res = _dsReport.Tables["Results"];

			//Выбираем диапазон, по которому будет строить диаграму
			((MSExcel.Range)ws.get_Range(ws.Cells[2 + filter.Count, 1], ws.Cells[res.Rows.Count + 1, 2])).Select();
			MSExcel.Shape s;
			s = ws.Shapes.AddChart(MSExcel.XlChartType.xlPie, 20, 40, 450, 230);
			
			//Устанавливаем диаграмму справа от таблицы
			s.Top = 5;
			s.Left = Convert.ToSingle(((MSExcel.Range)ws.Cells[1 + filter.Count, 5]).Left);

			//Производим подсчет высоты легенды, чтобы она полностью отобразилась на диаграмме
			double legendHeight = 0;
			for (int i = 1; i <= ((MSExcel.LegendEntries)s.Chart.Legend.LegendEntries(Type.Missing)).Count; i++)
				legendHeight += ((MSExcel.LegendEntry)s.Chart.Legend.LegendEntries(i)).Height;

			legendHeight *= 0.9;

			if (legendHeight > s.Height)
				s.Height = Convert.ToSingle(legendHeight);
			
			//Увеличиваем зону легенды, прижимаем рисунок диаграммы к рамке
			s.Chart.Legend.Top = 0;
			s.Chart.Legend.Left = 220;
			s.Chart.Legend.Width = 230;
			s.Chart.PlotArea.Left = 0;
			s.Chart.PlotArea.Width = 220;
			s.Chart.Legend.Height = s.Chart.ChartArea.Height;

			//Отображаем диаграмму
			s.Fill.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
			ProfileHelper.End();
		}


	}
}
