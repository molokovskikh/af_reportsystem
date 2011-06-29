﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Data;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using DataTable = System.Data.DataTable;
using LegendEntries = Microsoft.Office.Interop.Excel.LegendEntries;
using LegendEntry = Microsoft.Office.Interop.Excel.LegendEntry;
using Shape = Microsoft.Office.Interop.Excel.Shape;
using XlChartType = Microsoft.Office.Interop.Excel.XlChartType;

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
			var provideNameField = selectedField.Find(value => value.reportPropertyPreffix == "FirmCode");
			if (provideNameField == null)
			{
				provideNameField = registredField.Find(value => value.reportPropertyPreffix == "FirmCode");
				selectedField.Add(provideNameField);
			}
			provideNameField.visible = true;
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("Processing1");
			var selectCommand = BuildSelect();

			selectCommand = String.Concat(
				selectCommand, @"
Sum(ol.cost*ol.Quantity) as Summ
from " +
#if DEBUG
  @"orders.OrdersHead oh 
  join orders.OrdersList ol on ol.OrderID = oh.RowID " +
#else
  @"ordersold.OrdersHead oh 
  join ordersold.OrdersList ol on ol.OrderID = oh.RowID " +
#endif
 @"join catalogs.products p on p.Id = ol.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
#left join usersettings.clientsdata cd on cd.FirmCode = oh.ClientCode
  left join future.Clients cl on cl.Id = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
#  join usersettings.clientsdata prov on prov.FirmCode = pd.FirmCode
  join future.suppliers prov on prov.Id = pd.FirmCode
#  join farm.regions provrg on provrg.RegionCode = prov.RegionCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join future.addresses adr on oh.AddressId = adr.Id
  join billing.LegalEntities le on adr.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId
where 1=1");

			selectCommand = ApplyFilters(selectCommand);
			selectCommand = ApplyGroupAndSort(selectCommand, "Summ desc");

#if DEBUG
			Debug.WriteLine(selectCommand);
#endif

			var selectTable = new DataTable();
			e.DataAdapter.SelectCommand.CommandText = selectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(selectTable);

			ProfileHelper.Next("Processing2");

			decimal AllSumm = 0m;
			decimal OtherSumm = 0m;
			int currentCount = 0;
			foreach (var dr in selectTable.Rows.Cast<DataRow>())
			{
				currentCount++;
				AllSumm += Convert.ToDecimal(dr["Summ"]);
				if (currentCount > providerCount)
					OtherSumm += Convert.ToDecimal(dr["Summ"]);
			}

			var res = BuildResultTable(selectTable);
			var dc = res.Columns.Add("SummPercent", typeof (Double));
			dc.Caption = "Доля рынка в %";

			DataRow newrow;
			res.BeginLoadData();
			currentCount = 0;
			foreach (DataRow dr in selectTable.Rows)
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
			res.EndLoadData();
			ProfileHelper.End();
		}

		protected override void PostProcessing(Application exApp, _Worksheet ws)
		{
			ProfileHelper.Next("ExcelDiagrammProcessing");
			var res = _dsReport.Tables["Results"];

			//Выбираем диапазон, по которому будет строить диаграму
			(ws.Range[ws.Cells[2 + filterDescriptions.Count, 1], ws.Cells[res.Rows.Count + 1, 2]]).Select();
			Shape s;
			s = ws.Shapes.AddChart(XlChartType.xlPie, 20, 40, 450, 230);

			//Устанавливаем диаграмму справа от таблицы
			s.Top = 5;
			s.Left = Convert.ToSingle(((Range) ws.Cells[1 + filterDescriptions.Count, 5]).Left);

			//Производим подсчет высоты легенды, чтобы она полностью отобразилась на диаграмме
			double legendHeight = 0;
			for (int i = 1; i <= ((LegendEntries) s.Chart.Legend.LegendEntries(Type.Missing)).Count; i++)
				legendHeight += ((LegendEntry) s.Chart.Legend.LegendEntries(i)).Height;

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
			s.Fill.Visible = MsoTriState.msoTrue;
			ProfileHelper.End();
		}
	}
}
