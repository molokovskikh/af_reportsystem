using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.IO;

namespace Inforoom.ReportSystem
{
	//Дефектурный отчет
	public class DefReport : ProviderReport
	{
		protected int _reportType;
		protected int _priceCode;

		public DefReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
		}

		public override void ReadReportParams()
		{
			_reportType = (int)getReportParam("ReportType");
			_priceCode = (int)getReportParam("PriceCode");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = @"
select 
  gr.FirmCode 
from 
  testreports.reports r,
  testreports.general_reports gr
where
    r.ReportCode = ?ReportCode
and gr.GeneralReportCode = r.GeneralReportCode";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.Add("ReportCode", _reportCode);
			int ClientCode = Convert.ToInt32(e.DataAdapter.SelectCommand.ExecuteScalar());
			//Устанавливаем код клиента, как код фирмы, относительно которой генерируется отчет
			_clientCode = ClientCode;

			//Выбираем 
			GetActivePricesT(e);
			GetAllCoreT(e);

			e.DataAdapter.SelectCommand.Parameters.Clear();

			string SelectCommandText = String.Empty;

			switch (_reportType)
			{
				case 1:
					{
						SelectCommandText = @"
drop temporary table IF EXISTS SummaryT;
CREATE temporary table SummaryT ( 
  STShortCode int Unsigned, 
  key STShortCode(STShortCode))engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryT 
select distinct ShortCode As STShortCode 
from 
  ActivePricesT apt, 
  AllCoreT c 
where apt.PriceCode <> ?SourcePC 
and apt.PriceCode=c.PriceCode;

drop temporary table IF EXISTS OtherShortCodeT;
CREATE temporary table OtherShortCodeT ( 
  OShortCode int Unsigned, 
  key OShortCode(OShortCode))engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherShortCodeT 
select distinct c.ShortCode As OShortCode 
from 
  AllCoreT c 
  left join SummaryT st on st.STShortCode=c.ShortCode 
where c.PriceCode=?SourcePC 
and st.STShortCode is NULL;

select distinct c.Code, ctlg.Name 
from 
 (
  farm.Catalog ctlg, 
  OtherShortCodeT osct 
 )
  left join AllCoreT c on c.ShortCode=osct.OShortCode and c.PriceCode = ?SourcePC 
where ctlg.ShortCode=osct.OShortCode ;";
						break;
					}
				case 2:
					{
						SelectCommandText = @"
drop temporary table IF EXISTS SummaryT;
CREATE temporary table SummaryT ( 
  STFullCode int Unsigned, 
  key STFullCode(STFullCode))engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryT 
select distinct FullCode As STFullCode 
from 
  ActivePricesT apt, 
  AllCoreT c 
where apt.PriceCode <> ?SourcePC 
and apt.PriceCode=c.PriceCode;

drop temporary table IF EXISTS OtherFullCodeT;
CREATE temporary table OtherFullCodeT ( 
  OFullCode int Unsigned, 
  key OFullCode(OFullCode) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherFullCodeT 
select distinct c.FullCode As OFullCode 
from 
  AllCoreT c 
  left join SummaryT st on st.STFullCode=c.FullCode 
where c.PriceCode=?SourcePC 
and st.STFullCode is NULL;

select  c.Code, ctlg.Name, ctlg.Form 
from 
 (
  farm.catalog ctlg, 
  OtherFullCodeT ofct 
 )
  left join AllCoreT c on c.FullCode=ofct.OFullCode and c.PriceCode = ?SourcePC 
where ctlg.FullCode = ofct.OFullCode;";
						break;
					}
				case 3:
					{
						SelectCommandText = @"
drop temporary table IF EXISTS SummaryT;
CREATE temporary table SummaryT ( 
  STFullCode int Unsigned, 
  STCodeFirmCr int Unsigned, 
  key STFullCode(STFullCode), 
  key STCodeFirmCr(STCodeFirmCr) )engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryT 
select distinct FullCode As STFullCode, CodeFirmCr As STCodeFirmCr 
from 
  ActivePricesT apt, 
  AllCoreT c 
where apt.PriceCode <> ?SourcePC 
and apt.PriceCode=c.PriceCode;

drop temporary table IF EXISTS OtherFullCodeT;
CREATE temporary table OtherFullCodeT ( 
  OFullCode int Unsigned, 
  OCodeFirmCr int Unsigned, 
  key OFullCode(OFullCode), 
  key OCodeFirmCr(OCodeFirmCr) )engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherFullCodeT 
select distinct c.FullCode As OFullCode, c.CodeFirmCr As OCodeFirmCr 
from 
  AllCoreT c 
  left join SummaryT st on st.STFullCode=c.FullCode and st.STCodeFirmCr=c.CodeFirmCr 
where 
    c.PriceCode=?SourcePC 
and st.STFullCode is NULL;

select distinct c.Code, ctlg.Name,  ctlg.Form,  cfcr.FirmCr 
from 
 (
  farm.CatalogFirmCr cfcr, 
  OtherFullCodeT ofct, 
  farm.catalog ctlg 
 )
  left join AllCoreT c on c.FullCode=ofct.OFullCode and c.CodeFirmCr=ofct.OCodeFirmCr and c.PriceCode = ?SourcePC 
where 
    ctlg.FullCode=ofct.OFullCode 
and ofct.OCodeFirmCr=cfcr.CodeFirmCr 
order by ctlg.Name,  ctlg.Form,  cfcr.FirmCr;";
						break;
					}
			}
			e.DataAdapter.SelectCommand.CommandText = SelectCommandText;
			e.DataAdapter.SelectCommand.Parameters.Add("SourcePC", _priceCode);
			e.DataAdapter.Fill(_dsReport, "Results");
		}

		public override void ReportToFile(string FileName)
		{
			DataTableToExcel(_dsReport.Tables["Results"].DefaultView.ToTable(), FileName);
			FormatExcel(FileName);
		}

		protected void FormatExcel(string FileName)
		{
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				MSExcel.Workbook wb = exApp.Workbooks.Open(FileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try
				{
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

					try
					{
						ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);

						//Форматируем заголовок отчета
						ws.Cells[1, 1] = "Код";
						((MSExcel.Range)ws.Columns[1, Type.Missing]).AutoFit();

						ws.Cells[1, 2] = "Наименование";
						((MSExcel.Range)ws.Columns[2, Type.Missing]).AutoFit();

						switch (_reportType)
						{
							case 2:
								{
									ws.Cells[1, 3] = "Форма выпуска";
									((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
									break;
								}
							case 3:
								{
									ws.Cells[1, 3] = "Форма выпуска";
									((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
									ws.Cells[1, 4] = "Производитель";
									((MSExcel.Range)ws.Columns[4, Type.Missing]).AutoFit();
									break;
								}
						}

						//рисуем границы на заголовок таблицы
						ws.get_Range(ws.Cells[1, 1], ws.Cells[1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThick;

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[2, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count+1, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//Замораживаем некоторые колонки и столбцы
						((MSExcel.Range)ws.get_Range("A2", System.Reflection.Missing.Value)).Select();
						exApp.ActiveWindow.FreezePanes = true;
					}
					finally
					{ 
						wb.SaveAs(FileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
					}
				}
				finally
				{
					ws = null;
					wb = null;
					try { exApp.Workbooks.Close(); }
					catch { }
				}
			}
			finally
			{
				try { exApp.Quit(); }
				catch { }
				exApp = null;
			}
		}

	}
}
