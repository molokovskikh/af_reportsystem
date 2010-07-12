using System;
using System.Data;
using System.Data.OleDb;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.Writers
{
	public class BaseExcelWriter : IWriter
	{
		public const int MaxStringSize = 250;
		public const int MaxListName = 26;

		public void DataTableToExcel(DataTable dtExport, string ExlFileName, ulong reportCode)
		{
			DataTableToExcel(dtExport, ExlFileName, "rep" + reportCode);
		}

		protected void DataTableToExcel(DataTable dtExport, string ExlFileName, string listName)
		{
			//Имя листа генерируем сами, а потом переименовываем, т.к. русские названия листов потом невозможно найти
			OleDbConnection ExcellCon = new OleDbConnection();
			try
			{
				ExcellCon.ConnectionString = @"
Provider=Microsoft.Jet.OLEDB.4.0;Password="""";User ID=Admin;Data Source=" + ExlFileName +
@";Mode=Share Deny None;Extended Properties=""Excel 8.0;HDR=no"";";
				string CreateSQL = "create table [" + listName + "] (";
				for (int i = 0; i < dtExport.Columns.Count; i++)
				{
					CreateSQL += "[F" + (i + 1).ToString() + "] ";
					dtExport.Columns[i].ColumnName = "F" + (i + 1).ToString();
					if (dtExport.Columns[i].DataType == typeof(int))
						CreateSQL += " int";
					else
						if (dtExport.Columns[i].DataType == typeof(decimal))
							CreateSQL += " currency";
						else
							if (dtExport.Columns[i].DataType == typeof(double))
								CreateSQL += " real";
							else
								if ((dtExport.Columns[i].DataType == typeof(string)) && (dtExport.Columns[i].MaxLength > -1) && (dtExport.Columns[i].MaxLength <= MaxStringSize))
									CreateSQL += String.Format(" char({0})", MaxStringSize);
								else
									CreateSQL += " memo";
					if (i == dtExport.Columns.Count - 1)
						CreateSQL += ");";
					else
						CreateSQL += ",";
				}
				OleDbCommand cmd = new OleDbCommand(CreateSQL, ExcellCon);
				ExcellCon.Open();
				cmd.ExecuteNonQuery();
				OleDbDataAdapter daExcel = new OleDbDataAdapter("select * from [" + listName + "]", ExcellCon);
				OleDbCommandBuilder cdExcel = new OleDbCommandBuilder(daExcel);
				cdExcel.QuotePrefix = "[";
				cdExcel.QuoteSuffix = "]";
				daExcel.Update(dtExport);
			}
			finally
			{
				ExcellCon.Close();
			}
		}

		public void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
		{
			DataTableToExcel(reportData.Tables["Results"], fileName, settings.ReportCode);
			ProfileHelper.Next("FormatExcel");
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			var file = fileName;
			var result = reportData.Tables["Results"];
			var reportId = settings.ReportCode;
			var caption = settings.ReportCaption;
			try
			{
				exApp.DisplayAlerts = false;
				MSExcel.Workbook wb = exApp.Workbooks.Open(file, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try
				{
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + reportId.ToString()];

					try
					{
						ws.Name = caption.Substring(0, (caption.Length < MaxListName) ? caption.Length : MaxListName);

						for (int i = 0; i < result.Columns.Count; i++)
						{
							ws.Cells[1, i + 1] = "";
							ws.Cells[1, i + 1] = result.Columns[i].Caption;
							if (result.Columns[i].ExtendedProperties.ContainsKey("Width"))
								((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)result.Columns[i].ExtendedProperties["Width"]).Value;
							else
								((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).AutoFit();
							if (result.Columns[i].ExtendedProperties.ContainsKey("Color"))
								ws.get_Range(ws.Cells[1, i + 1], ws.Cells[result.Rows.Count + 1, i + 1]).Interior.Color = System.Drawing.ColorTranslator.ToOle((System.Drawing.Color)result.Columns[i].ExtendedProperties["Color"]);
						}

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[1, 1], ws.Cells[result.Rows.Count + 1, result.Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						ws.Range[ws.Cells[1, 1], ws.Cells[result.Rows.Count + 1, result.Columns.Count]].Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);
					}
					finally
					{
						wb.SaveAs(file, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
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
	}
}
