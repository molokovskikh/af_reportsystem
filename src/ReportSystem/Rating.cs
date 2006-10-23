using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Data;
using MySql.Data.MySqlClient;
using Aspose.Excel;
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
		public const string colReportCode = "ReportCode";
		public const string colCombineReportCode = "CombineReports_CombineReportCode";
		public const string colReportCaption = "ReportCaption";

		public const string fromProperty = "FromDate";
		public const string toProperty = "ToDate";
		public const string junkProperty = "JunkState";

		public const string MySQLDateFormat = "yyyy-MM-dd";


		public int reportID;
		public int clientCode;
		public string reportCaption;
		public ArrayList allField;
		public ArrayList selectField;

		public DateTime dtFrom;
		public DateTime dtTo;
		public int JunkState;

		MySqlConnection conn;
		System.Data.DataTable dtProperties;


		public RatingReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{ 
		}

		//public RatingReport(int GeneralReportID, int FirmCode, string ReportCaption, MySqlConnection Conn)
		//{
		//    reportID = GeneralReportID;
		//    clientCode = FirmCode;
		//    reportCaption = ReportCaption;
		//    _conn = Conn;

		//    dtProperties = new System.Data.DataTable();
		//    MySqlDataAdapter da = new MySqlDataAdapter(
		//        String.Format("select * from usersettings.ReportProperties where {0} = ?{1}", RatingField.colReportCode, RatingField.colReportCode), _conn);
		//    da.SelectCommand.Parameters.Add(RatingField.colReportCode, reportID);
		//    da.Fill(dtProperties);

		//    DataRow[] dr;

		//    dr = dtProperties.Select(String.Format("{0} = '{1}'", RatingField.colPropertyName, fromProperty));
		//    if (1 == dr.Length)
		//        dtFrom = DateTime.ParseExact(dr[0][RatingField.colPropertyValue].ToString(), MySQLDateFormat, null);
		//    else
		//        throw new Exception(String.Format("���-�� �������� {0} �� ����� 1 ({1})", fromProperty, dr.Length));

		//    dr = dtProperties.Select(String.Format("{0} = '{1}'", RatingField.colPropertyName, toProperty));
		//    if (1 == dr.Length)
		//        dtTo = DateTime.ParseExact(dr[0][RatingField.colPropertyValue].ToString(), MySQLDateFormat, null);
		//    else
		//        throw new Exception(String.Format("���-�� �������� {0} �� ����� 1 ({1})", toProperty, dr.Length));

		//    dr = dtProperties.Select(String.Format("{0} = '{1}'", RatingField.colPropertyName, junkProperty));
		//    if (1 == dr.Length)
		//        JunkState = Convert.ToInt32(dr[0][RatingField.colPropertyValue]);
		//    else
		//        throw new Exception(String.Format("���-�� �������� {0} �� ����� 1 ({1})", junkProperty, dr.Length));

		//    allField = new ArrayList(9);
		//    selectField = new ArrayList(9);
		//    allField.Add(new RatingField("c.FullCode", "concat(c.Name, ' ', c.Form) as FullName", "FullName", "FullName", "������������ � ����� �������"));
		//    allField.Add(new RatingField("c.ShortCode", "c.Name as PosName", "PosName", "ShortName", "������������"));
		//    allField.Add(new RatingField("cfc.CodeFirmCr", "cfc.FirmCr as FirmCr", "FirmCr", "FirmCr", "�������������"));
		//    allField.Add(new RatingField("rg.RegionCode", "rg.Region as RegionName", "RegionName", "Region", "������"));
		//    allField.Add(new RatingField("ftg.ftg", "ftg.Name as FTGName", "FTGName", "FTG", "����������"));

		//    foreach(RatingField rf in allField)
		//    {
		//        if (rf.LoadFromDB(dtProperties))
		//            selectField.Add(rf);
		//    }

		//    selectField.Sort(new RatingComparer());

		//}

		public System.Data.DataTable GetReport()
		{
			string SelectCommand = "select ";
			foreach(RatingField rf in selectField)
				if (rf.visible)
					SelectCommand = String.Concat(SelectCommand, rf.primaryField, ", ", rf.viewField, ", ");

			SelectCommand = String.Concat(SelectCommand, "Sum(ol.cost*ol.Quantity) as Cost, Sum(ol.Quantity) as PosOrder ");
			SelectCommand = String.Concat(
				SelectCommand, "from orders.OrdersHead oh, orders.OrdersList ol, farm.Catalog c, farm.CatalogFirmCr cfc, usersettings.clientsdata cd, farm.regions rg, usersettings.pricesdata pd, usersettings.clientsdata prov where ol.OrderID = oh.RowID and c.FullCode = ol.FullCode and cfc.CodeFirmCr = if(ol.CodeFirmCr,  ol.CodeFirmCr, 1) and cd.FirmCode = oh.FirmCode and rg.RegionCode = oh.RegionCode and pd.PriceCode = oh.PriceCode and prov.FirmCode = pd.FirmCode");

			foreach(RatingField rf in selectField)
			{
				if ((null != rf.equalValues) && (rf.equalValues.Length > 0))
					SelectCommand = String.Concat(SelectCommand,  " and ", rf.GetEqualValues());
				if ((null != rf.nonEqualValues) && (rf.nonEqualValues.Length > 0))
					SelectCommand = String.Concat(SelectCommand,  " and ", rf.GetNonEqualValues());
			}

			if (1 == JunkState)
				SelectCommand = String.Concat(SelectCommand,  " and (ol.Junk = 0)");
			else
				if (2 == JunkState)
					SelectCommand = String.Concat(SelectCommand,  " and (ol.Junk = 1)");

			SelectCommand = String.Concat(SelectCommand,  String.Format(" and (oh.WriteTime > '{0}')", dtFrom.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand,  String.Format(" and (oh.WriteTime < '{0}')", dtTo.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand,  " group by ", ((RatingField)selectField[0]).primaryField);
			string Sort = ((RatingField)selectField[0]).outputField;
			for(int i = 1; i < selectField.Count; i++)
				if (((RatingField)selectField[i]).visible)
				{
					SelectCommand = String.Concat(SelectCommand, ", ", ((RatingField)selectField[i]).primaryField);
					Sort = String.Concat(Sort, ", ", ((RatingField)selectField[i]).outputField);
				}

			Debug.WriteLine(SelectCommand);
			MySqlDataAdapter daSelect = new MySqlDataAdapter(SelectCommand, conn);
			System.Data.DataTable SelectTable = new System.Data.DataTable();
			daSelect.Fill(SelectTable);

			decimal Cost=0m;
			int PosOrder=0;
			foreach(DataRow dr in SelectTable.Rows)
			{
				Cost+=Convert.ToDecimal(dr["Cost"]);
				PosOrder+=Convert.ToInt32(dr["PosOrder"]);
			}

			System.Data.DataTable res = new System.Data.DataTable();
			DataColumn dc;
			foreach(RatingField rf in selectField)
			{
				if (rf.visible)
				{
					dc = res.Columns.Add(rf.outputField, SelectTable.Columns[rf.outputField].DataType);
					dc.Caption = rf.outputCaption;
				}
			}
			dc = res.Columns.Add("Cost", typeof(System.Decimal));
			dc.Caption = "�����";
			dc = res.Columns.Add("CostPercent", typeof(System.Decimal));
			dc.Caption = "���� ����� � %";
			dc = res.Columns.Add("PosOrder", typeof(System.Int32));
			dc.Caption = "�����";
			dc = res.Columns.Add("PosOrderPercent", typeof(System.Decimal));
			dc.Caption = "���� �� ������ ������ � %";

			DataRow newrow;
			try
			{
				res.BeginLoadData();
				foreach(DataRow dr in SelectTable.Rows)
				{
					newrow = res.NewRow();

					foreach(RatingField rf in selectField)
						if (rf.visible)
							newrow[rf.outputField] = dr[rf.outputField];
					newrow["Cost"] = Convert.ToDecimal(dr["Cost"]);
					newrow["PosOrder"] = Convert.ToInt32(dr["PosOrder"]);
					newrow["CostPercent"] = Decimal.Round(((decimal)newrow["Cost"]*100)/Cost, 2);
					newrow["PosOrderPercent"] = Decimal.Round((Convert.ToDecimal(newrow["PosOrder"])*100)/Convert.ToDecimal(PosOrder), 2);

					res.Rows.Add(newrow);
				}
			}
			finally
			{
				res.EndLoadData();
			}

			res.DefaultView.Sort = Sort;
			return res;
			
		}

		public void ExportToExcel(System.Data.DataTable dtRes)
		{
			Aspose.Excel.Excel ex = new Excel();
			while (ex.Worksheets.Count > 1)
				ex.Worksheets.RemoveAt(0);
			Aspose.Excel.Worksheet ws = ex.Worksheets[0];
			ws.Name = "�����";
			ws.Cells.Clear();
			ws.Cells.ImportDataTable(dtRes, true, "A3");
			for(int i = 0; i<dtRes.Columns.Count; i++)
			  ws.AutoFitColumn(i);
			string ShortName =  "RatingReport" + clientCode.ToString() + ".xls";
			string FileName = System.IO.Path.GetTempPath() + ShortName;
			ex.Save(FileName);
			ws = null;
			ex = null;

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
//					if (exWS.Name != "�����")
//					{
//						exWS.Delete();
//					}

//					((MSExcel._Worksheet)exWB.Worksheets[1]).Delete();


					exWS = (MSExcel._Worksheet)exWB.Worksheets[1];
//					exWS = (MSExcel.Worksheet)exWB.Sheets[1];
					exWS.Cells.Clear();
					exWS.Delete();
					exWS = null;
//					if (exWS.Name == "�����")
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

		}

		public override void GenerateReport(ExecuteArgs e)
		{ 
		}

		public override void ReportToFile(string FileName)
		{ 
		}
	}
}
