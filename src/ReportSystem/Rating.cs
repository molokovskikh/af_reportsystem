using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Data;
using MySql.Data.MySqlClient;
//using Aspose.Excel;
using ICSharpCode.SharpZipLib.Zip;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using Inforoom.ReportSystem.RatingReports;
using ExecuteTemplate;

namespace Inforoom.ReportSystem
{
	/// <summary>
	/// Summary description for RatingReport.
	/// </summary>
	public class RatingReport : BaseReport
	{
     	public const string fromProperty = "FromDate";
		public const string toProperty = "ToDate";
		public const string junkProperty = "JunkState";

		public int reportID;
		public int clientCode;
		public string reportCaption;
		public ArrayList allField;
		public ArrayList selectField;

		public DateTime dtFrom;
		public DateTime dtTo;
		public int JunkState;

		public RatingReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
		}

		public override void ReadReportParams()
		{
			dtFrom = (DateTime)getReportParam(fromProperty);
			dtTo = (DateTime)getReportParam(toProperty);
			JunkState = (int)getReportParam(junkProperty);

			allField = new ArrayList(9);
			selectField = new ArrayList(9);
			allField.Add(new RatingField("c.FullCode", "concat(c.Name, ' ', c.Form) as FullName", "FullName", "FullName", "Наименование и форма выпуска"));
			allField.Add(new RatingField("c.ShortCode", "c.Name as PosName", "PosName", "ShortName", "Наименование"));
			allField.Add(new RatingField("cfc.CodeFirmCr", "cfc.FirmCr as FirmCr", "FirmCr", "FirmCr", "Производитель"));
			allField.Add(new RatingField("rg.RegionCode", "rg.Region as RegionName", "RegionName", "Region", "Регион"));
			allField.Add(new RatingField("prov.FirmCode", "prov.ShortName as FirmShortName", "FirmShortName", "FirmCode", "Поставщик"));
			allField.Add(new RatingField("pd.PriceCode", "pd.PriceName as PriceName", "PriceName", "PriceCode", "Прайс-лист"));
			allField.Add(new RatingField("cd.FirmCode", "cd.ShortName as ClientShortName", "ClientShortName", "ClientCode", "Аптека"));

			foreach (RatingField rf in allField)
			{
				if (rf.LoadFromDB(this))
					selectField.Add(rf);
			}

			selectField.Sort(new RatingComparer());
		}

		public void ExportToExcel(System.Data.DataTable dtRes)
		{
/*
			Aspose.Excel.Excel ex = new Excel();
			while (ex.Worksheets.Count > 1)
				ex.Worksheets.RemoveAt(0);
			Aspose.Excel.Worksheet ws = ex.Worksheets[0];
			ws.Name = "Отчет";
			ws.Cells.Clear();
			ws.Cells.ImportDataTable(dtRes, true, "A3");
			for(int i = 0; i<dtRes.Columns.Count; i++)
			  ws.AutoFitColumn(i);
			string ShortName =  "RatingReport" + clientCode.ToString() + ".xls";
			string FileName = System.IO.Path.GetTempPath() + ShortName;
			ex.Save(FileName);
			ws = null;
			ex = null;
 */ 

			//return;

/*
			MSExcel.Application exap = new MSExcel.Application();
			try
			{
//				MSExcel._Workbook exWB = (MSExcel._Workbook)exap.Workbooks.Open("C:\\TEMP\\TempWarn.xls", Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
				MSExcel._Workbook exWB = (MSExcel._Workbook)exap.Workbooks.Open("C:\\TEMP\\1.xls", Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
//				MSExcel._Workbook exWB = exap.Workbooks.Add(Missing.Value);
				MSExcel._Worksheet exWS;
				try
				{
//					exWS = (MSExcel.Worksheet)exWB.ActiveSheet;
//					if (exWS.Name != "Отчет")
//					{
//						exWS.Delete();
//					}

//					((MSExcel._Worksheet)exWB.Worksheets[1]).Delete();


					exWS = (MSExcel._Worksheet)exWB.Worksheets[1];
//					exWS = (MSExcel.Worksheet)exWB.Sheets[1];
					exWS.Cells.Clear();
					exWS.Delete();
					exWS = null;
//					if (exWS.Name == "Отчет")
//					{
//						exWS.Unprotect(Missing.Value);
//						exWS.Delete();
//					}

//					exWS.Rows.Font.Size = 8;
//					exWS.Rows.Font.Name = "Arial Narrow";

//					exWB.SaveAs("C:\\TEMP\\TempWarn.xls", MSExcel.XlFileFormat.xlWorkbookNormal, Missing.Value, Missing.Value, Missing.Value, Missing.Value, MSExcel.XlSaveAsAccessMode.xlNoChange, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);

				}
				finally
				{
					exWS = null;
					exWB.Close(MSExcel.XlSaveAction.xlSaveChanges, Missing.Value, Missing.Value);
					exWB = null;
				}
			}
			finally
			{
				exap.Quit();
				exap = null;
				GC.Collect();
			}
*/			
		

/*
			MemoryStream ZipOutputStream = new MemoryStream();
            ZipOutputStream ZipInputStream = new ZipOutputStream(ZipOutputStream);
			ZipEntry ZipObject = new ZipEntry(ShortName);
			FileStream MySqlFileStream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 10240);
			byte[] MySqlFileByteArray = new byte[MySqlFileStream.Length];

			MySqlFileStream.Read(MySqlFileByteArray, 0, Convert.ToInt32(MySqlFileStream.Length));

			ZipInputStream.SetLevel(9);
			ZipObject.DateTime = DateTime.Now;
            ZipInputStream.PutNextEntry(ZipObject);
            ZipInputStream.Write(MySqlFileByteArray, 0, Convert.ToInt32(MySqlFileStream.Length));
			MySqlFileStream.Close();
            ZipInputStream.Finish();

//			string ResDirPath = @"\\iserv\FTP\OptBox\"
			string ResDirPath = @"C:\Temp\Reports\";
			string ClientCodeStr = clientCode.ToString();
			string ResFileName = "RatingReport" + ClientCodeStr + ".zip";
			if (ClientCodeStr.Length < 3)
				ClientCodeStr = "0" + ClientCodeStr;
            ResDirPath += ClientCodeStr + @"\Reports\";

			if (!Directory.Exists(ResDirPath))
				Directory.CreateDirectory(ResDirPath);

			if (File.Exists(ResDirPath + ResFileName))
				File.Delete(ResDirPath + ResFileName);

			FileStream ResultFile = new FileStream(ResDirPath + ResFileName, FileMode.CreateNew);
	        ResultFile.Write(ZipOutputStream.ToArray(), 0, Convert.ToInt32(ZipOutputStream.Length));

            ZipOutputStream.Close();
            ZipInputStream.Close();
            ResultFile.Close();
			
			File.Delete(FileName);
 */ 

		}

		public override void GenerateReport(ExecuteArgs e)
		{
			string SelectCommand = "select ";
			foreach (RatingField rf in selectField)
				if (rf.visible)
					SelectCommand = String.Concat(SelectCommand, rf.primaryField, ", ", rf.viewField, ", ");

			SelectCommand = String.Concat(SelectCommand, "Sum(ol.cost*ol.Quantity) as Cost, Sum(ol.Quantity) as PosOrder ");
			SelectCommand = String.Concat(
				SelectCommand, @"
from 
  orders.OrdersHead oh, 
  orders.OrdersList ol, 
  farm.Catalog c, 
  farm.CatalogFirmCr cfc, 
  usersettings.clientsdata cd, 
  farm.regions rg, 
  usersettings.pricesdata pd, 
  usersettings.clientsdata prov 
where 
    ol.OrderID = oh.RowID 
and c.FullCode = ol.FullCode 
and cfc.CodeFirmCr = if(ol.CodeFirmCr,  ol.CodeFirmCr, 1) 
and cd.FirmCode = oh.ClientCode 
and rg.RegionCode = oh.RegionCode 
and pd.PriceCode = oh.PriceCode 
and prov.FirmCode = pd.FirmCode");

			foreach (RatingField rf in selectField)
			{
				if ((null != rf.equalValues) && (rf.equalValues.Length > 0))
					SelectCommand = String.Concat(SelectCommand, " and ", rf.GetEqualValues());
				if ((null != rf.nonEqualValues) && (rf.nonEqualValues.Length > 0))
					SelectCommand = String.Concat(SelectCommand, " and ", rf.GetNonEqualValues());
			}

			if (1 == JunkState)
				SelectCommand = String.Concat(SelectCommand, " and (ol.Junk = 0)");
			else
				if (2 == JunkState)
					SelectCommand = String.Concat(SelectCommand, " and (ol.Junk = 1)");

			SelectCommand = String.Concat(SelectCommand, String.Format(" and (oh.WriteTime > '{0}')", dtFrom.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand, String.Format(" and (oh.WriteTime < '{0}')", dtTo.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand, " group by ", ((RatingField)selectField[0]).primaryField);
			string Sort = ((RatingField)selectField[0]).outputField;
			for (int i = 1; i < selectField.Count; i++)
				if (((RatingField)selectField[i]).visible)
				{
					SelectCommand = String.Concat(SelectCommand, ", ", ((RatingField)selectField[i]).primaryField);
					Sort = String.Concat(Sort, ", ", ((RatingField)selectField[i]).outputField);
				}

			Debug.WriteLine(SelectCommand);
			System.Data.DataTable SelectTable = new System.Data.DataTable();
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
			foreach (RatingField rf in selectField)
			{
				if (rf.visible)
				{
					dc = res.Columns.Add(rf.outputField, SelectTable.Columns[rf.outputField].DataType);
					dc.Caption = rf.outputCaption;
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

			DataRow newrow;
			try
			{
				res.BeginLoadData();
				foreach (DataRow dr in SelectTable.Rows)
				{
					newrow = res.NewRow();

					foreach (RatingField rf in selectField)
						if (rf.visible)
							newrow[rf.outputField] = dr[rf.outputField];
					newrow["Cost"] = Convert.ToDecimal(dr["Cost"]);
					newrow["PosOrder"] = Convert.ToInt32(dr["PosOrder"]);
					newrow["CostPercent"] = Decimal.Round(((decimal)newrow["Cost"] * 100) / Cost, 2);
					newrow["PosOrderPercent"] = Decimal.Round((Convert.ToDecimal(newrow["PosOrder"]) * 100) / Convert.ToDecimal(PosOrder), 2);

					res.Rows.Add(newrow);
				}
			}
			finally
			{
				res.EndLoadData();
			}

			res.DefaultView.Sort = Sort;
			res = res.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
		}

		public override void ReportToFile(string FileName)
		{
			DataTableToExcel(_dsReport.Tables["Results"], FileName);
			FormatExcel(FileName);
		}

		protected void FormatExcel(string FileName)
		{
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try
			{
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
							ws.Cells[1, i + 1] = res.Columns[i].Caption;
							((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).AutoFit();
						}

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

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
						wb.Save();
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
