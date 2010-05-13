using System;
using System.Data;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.FastReports
{
	public abstract class BaseFastReport : ProviderReport
	{
		public BaseFastReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties) 
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
		}

		protected override void FormatExcel(string FileName)
		{
			ProfileHelper.Next("FormatExcel");
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

						DataTable res = _dsReport.Tables["Results"];
						for (int i = 0; i < res.Columns.Count; i++)
						{
							ws.Cells[1, i + 1] = "";
							ws.Cells[1, i + 1] = res.Columns[i].Caption;
							if (res.Columns[i].ExtendedProperties.ContainsKey("Width"))
								((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)res.Columns[i].ExtendedProperties["Width"]).Value;
							else
								((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).AutoFit();
							if (res.Columns[i].ExtendedProperties.ContainsKey("Color"))
								ws.get_Range(ws.Cells[1, i + 1], ws.Cells[res.Rows.Count + 1, i + 1]).Interior.Color = System.Drawing.ColorTranslator.ToOle((System.Drawing.Color)res.Columns[i].ExtendedProperties["Color"]);
						}

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[1, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[1, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						PostProcessing(exApp, ws);
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
			ProfileHelper.End();
		}

		/// <summary>
		/// Дополнительные действия с форматированием отчета, специфичные для отчета
		/// </summary>
		/// <param name="exApp"></param>
		/// <param name="ws"></param>
		protected virtual void PostProcessing(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{
		}
	}
}
