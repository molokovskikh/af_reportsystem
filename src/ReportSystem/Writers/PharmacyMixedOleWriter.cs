using System;
using System.Data;
using System.Drawing;
using System.Reflection;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using Microsoft.Office.Interop.Excel;
using MSExcel = Microsoft.Office.Interop.Excel;
using Inforoom.ReportSystem.Filters;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem.Writers
{
	public class PharmacyMixedOleWriter : BaseExcelWriter, IWriter
	{
		public void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
		{
			DataTableToExcel(reportData.Tables["Results"], fileName, settings.ReportCode);
			FormatExcel(reportData, fileName, settings as PharmacyMixedSettings);
		}

		public void FormatExcel(DataSet reportData, string fileName, PharmacyMixedSettings settings)
		{
			ProfileHelper.Next("FormatExcel");
			UseExcel.Workbook(fileName, b => {
				var exApp = b.Application;
				var wb = b;
				var ws = (_Worksheet) wb.Worksheets["rep" + settings.ReportCode.ToString()];

				ws.Name = settings.ReportCaption.Substring(0,
					(settings.ReportCaption.Length < MaxListName) ? settings.ReportCaption.Length : MaxListName);

				DataTable res = reportData.Tables["Results"];
				for (int i = 0; i < res.Columns.Count; i++)
				{
					ws.Cells[1, i + 1] = "";
					ws.Cells[1 + settings.Filter.Count, i + 1] = res.Columns[i].Caption;
					if (res.Columns[i].ExtendedProperties.ContainsKey("Width"))
						((Range) ws.Columns[i + 1, Type.Missing]).ColumnWidth =
							((int?) res.Columns[i].ExtendedProperties["Width"]).Value;
					else
						((Range) ws.Columns[i + 1, Type.Missing]).AutoFit();
					if (res.Columns[i].ExtendedProperties.ContainsKey("Color"))
						ws.get_Range(ws.Cells[1 + settings.Filter.Count, i + 1], ws.Cells[res.Rows.Count + 1, i + 1]).Interior.Color =
							ColorTranslator.ToOle((Color) res.Columns[i].ExtendedProperties["Color"]);
				}

				//рисуем границы на всю таблицу
				ws.get_Range(ws.Cells[1 + settings.Filter.Count, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count])
					.Borders.Weight = XlBorderWeight.xlThin;

				//Устанавливаем шрифт листа
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";
				ws.Activate();

				//Устанавливаем АвтоФильтр на все колонки
				ws.get_Range(ws.Cells[1 + settings.Filter.Count, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count]).Select();
				((Range) exApp.Selection).AutoFilter(1, Missing.Value,
					XlAutoFilterOperator.xlAnd, Missing.Value, true);

				for (int i = 0; i < settings.Filter.Count; i++)
					ws.Cells[1 + i, 1] = settings.Filter[i];

				int _freezeCount = settings.SelectedField.FindAll(fld => fld.visible).Count;

				//Замораживаем некоторые колонки и столбцы
				ws.get_Range(ws.Cells[2 + settings.Filter.Count, _freezeCount + 1],
					ws.Cells[2 + settings.Filter.Count, _freezeCount + 1]).Select();
				exApp.ActiveWindow.FreezePanes = true;
			});
			ProfileHelper.End();
		}
	}
}
