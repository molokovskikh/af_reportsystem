using System;
using System.Data;
using System.Data.OleDb;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using Microsoft.Office.Interop.Excel;
using DataTable = System.Data.DataTable;
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

        public void FormatExcelFile(MSExcel._Worksheet _ws, DataTable _result, string _caption, int CountDownRows)
        {
            //MSExcel._Worksheet _ws = (MSExcel._Worksheet)_wb.Worksheets["rep" + _reportId.ToString()];
            _ws.Name = _caption.Substring(0, (_caption.Length < MaxListName) ? _caption.Length : MaxListName);

            if (CountDownRows > 0)
            {
                for (int j = 1; j < 3; j++)
                {
                    for (int i = 0; i < CountDownRows - 3; i++)
                    {
                        _ws.Cells[1 + i, j] = _ws.Cells[2 + i, j];
                    }
                    _ws.Cells[CountDownRows - 2, j] = "";
                }
            }
            if (CountDownRows == 0)
            {
                CountDownRows = 2;
            }
            for (int i = 0; i < _result.Columns.Count; i++)
            {
                _ws.Cells[CountDownRows-1, i + 1] = "";
                _ws.Cells[CountDownRows-1, i + 1] = _result.Columns[i].Caption;
                if (CountDownRows != 2)
                {
                    _ws.Cells[1, 3] = "";
                }
                if (_result.Columns[i].ExtendedProperties.ContainsKey("Width"))
                    ((MSExcel.Range)_ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)_result.Columns[i].ExtendedProperties["Width"]).Value;
                else
                    ((MSExcel.Range)_ws.Columns[i + 1, Type.Missing]).AutoFit();
                if (_result.Columns[i].ExtendedProperties.ContainsKey("Color"))
                    _ws.get_Range(_ws.Cells[ CountDownRows, i + 1], _ws.Cells[_result.Rows.Count + 1, i + 1]).Interior.Color = System.Drawing.ColorTranslator.ToOle((System.Drawing.Color)_result.Columns[i].ExtendedProperties["Color"]);
            }


            //рисуем границы на всю таблицу
            _ws.get_Range(_ws.Cells[CountDownRows-1, 1], _ws.Cells[_result.Rows.Count + 1, _result.Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

            //Устанавливаем шрифт листа
            _ws.Rows.Font.Size = 8;
            _ws.Rows.Font.Name = "Arial Narrow";
            _ws.Activate();

            //Устанавливаем АвтоФильтр на все колонки
            _ws.Range[_ws.Cells[CountDownRows-1, 1], _ws.Cells[_result.Rows.Count + 1, _result.Columns.Count]].Select();
            ((MSExcel.Range)_ws.Application.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);
            //MSExcel.Worksheet rws = new Worksheet();
            //rws = _ws;
            //return (_wb);
        }


	    public virtual void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
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
					    FormatExcelFile(ws, result, caption, 0);
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
