using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using Microsoft.Office.Interop.Excel;
using System.Data;
using System.IO;

namespace Inforoom.ReportSystem
{
	//Отчет для вывода контактов. Это вспомогательный отчет, явно нигде не вызывается
	public class ContactsReport : ProviderReport
	{
		public ContactsReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{ 
		}

		public override void ReadReportParams()
		{
			_clientCode = (int)getReportParam("ClientCode");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			GetActivePrices(e);

			e.DataAdapter.SelectCommand.CommandText = @"
select at.FirmName, at.PublicUpCost, regions.Region, rd.ContactInfo 
from 
  ActivePrices at,
  farm.regions,
  usersettings.Regionaldata rd
where
    at.FirmCode = rd.FirmCode
and regions.RegionCode = at.RegionCode
and at.RegionCode = rd.RegionCode
order by PositionCount DESC";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(_dsReport, "Contacts");
		}

		public override void ReportToFile(string FileName)
		{
			Application exApp = new ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				Workbook wb = exApp.Workbooks.Open(FileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				Worksheet ws;
				try
				{
					ws = (Worksheet)wb.Worksheets.Add(System.Reflection.Missing.Value, wb.Worksheets[wb.Worksheets.Count], System.Reflection.Missing.Value, System.Reflection.Missing.Value);
					ContactsToExcel(_dsReport.Tables["Contacts"], ws);
					((_Worksheet)wb.Worksheets[1]).Activate();
					wb.SaveAs(FileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
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

		private void ContactsToExcel(System.Data.DataTable tbContacts, Worksheet wsContacts)
		{
			wsContacts.Name = "Контакты";
			//wsContacts.Move(null, )
			//wsContacts.Application.ActiveWorkbook.Worksheets["Контакты"].Move(After = wsContacts.Application.ActiveWorkbook.Worksheets(2));
			wsContacts.Rows.Font.Size = 8;
			wsContacts.Rows.Font.Name = "Arial Narrow";
			wsContacts.Cells[1, 1] = "Поставщик";
			((Range)wsContacts.Cells[1, 1]).Font.Bold = true;
			((Range)wsContacts.Cells[1, 1]).ColumnWidth = 10;
			wsContacts.Cells[1, 2] = "Контактная информация";
			((Range)wsContacts.Cells[1, 2]).Font.Bold = true;
			((Range)wsContacts.Cells[1, 2]).ColumnWidth = 20;
			wsContacts.get_Range(wsContacts.Cells[1, 1], wsContacts.Cells[2, 2]).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightBlue);
			wsContacts.get_Range(wsContacts.Cells[1, 1], wsContacts.Cells[2, 2]).HorizontalAlignment = XlHAlign.xlHAlignCenter;
			string TmpFirmName;
			int LastIndex;
			int SplitCount;
			int EndPosition;
			string[] ContactInfo;
			int StartPosition = 3;
			foreach (DataRow SrcRow in tbContacts.Rows)
			{
				TmpFirmName = SrcRow["FirmName"].ToString();
				LastIndex = TmpFirmName.LastIndexOf("- ");
				if (LastIndex > 0)
				{
					TmpFirmName = TmpFirmName.Substring(0, LastIndex);
				}
				wsContacts.Cells[StartPosition, 1] = TmpFirmName;
				wsContacts.Cells[StartPosition + 1, 1] = SrcRow["Region"].ToString();
				wsContacts.Cells[StartPosition + 2, 1] = "Скидка = " + SrcRow["PublicUpCost"].ToString();
				SplitCount = 0;
				if (!(SrcRow["ContactInfo"] is DBNull))
				{
					ContactInfo = ((string)(SrcRow["ContactInfo"])).Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
					for (int I = 0; I <= ContactInfo.Length - 1; I++)
					{
						if (!String.IsNullOrEmpty(ContactInfo[I]))
						{
							wsContacts.Cells[StartPosition + SplitCount, 2] = ContactInfo[I];
							SplitCount = SplitCount + 1;
						}
					}
				}
				if (SplitCount > 3)
				{
					EndPosition = StartPosition + SplitCount - 1;
				}
				else
				{
					EndPosition = StartPosition + 2;
				}
				SetBorderStyle((Range)wsContacts.get_Range(wsContacts.Cells[StartPosition, 1], wsContacts.Cells[EndPosition, 1]));
				SetBorderStyle((Range)wsContacts.get_Range(wsContacts.Cells[StartPosition, 2], wsContacts.Cells[EndPosition, 2]));
				((Range)wsContacts.get_Range(wsContacts.Cells[StartPosition, 1], wsContacts.Cells[EndPosition, 1])).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightGray);
				((Range)wsContacts.get_Range(wsContacts.Cells[StartPosition, 2], wsContacts.Cells[EndPosition, 2])).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSeaGreen);
				StartPosition = EndPosition + 2;
			}
			((Range)wsContacts.Columns[1, Type.Missing]).AutoFit();
			((Range)wsContacts.Columns[2, Type.Missing]).AutoFit();
		}

		private void SetBorderStyle(Range Selection)
		{
			//TODO:Здесь могут быть проблемы
			Selection.Borders[XlBordersIndex.xlDiagonalDown].LineStyle = XlLineStyle.xlLineStyleNone;
			Selection.Borders[XlBordersIndex.xlDiagonalUp].LineStyle = XlLineStyle.xlLineStyleNone;
			Selection.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
			Selection.Borders[XlBordersIndex.xlEdgeLeft].Weight = XlBorderWeight.xlMedium;
			Selection.Borders[XlBordersIndex.xlEdgeLeft].ColorIndex = XlColorIndex.xlColorIndexAutomatic;
			Selection.Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
			Selection.Borders[XlBordersIndex.xlEdgeTop].Weight = XlBorderWeight.xlMedium;
			Selection.Borders[XlBordersIndex.xlEdgeTop].ColorIndex = XlColorIndex.xlColorIndexAutomatic;
			Selection.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
			Selection.Borders[XlBordersIndex.xlEdgeBottom].Weight = XlBorderWeight.xlMedium;
			Selection.Borders[XlBordersIndex.xlEdgeBottom].ColorIndex = XlColorIndex.xlColorIndexAutomatic;
			Selection.Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
			Selection.Borders[XlBordersIndex.xlEdgeRight].Weight = XlBorderWeight.xlMedium;
			Selection.Borders[XlBordersIndex.xlEdgeRight].ColorIndex = XlColorIndex.xlColorIndexAutomatic;
			Selection.Borders[XlBordersIndex.xlInsideVertical].LineStyle = XlLineStyle.xlLineStyleNone;
			Selection.Borders[XlBordersIndex.xlInsideHorizontal].LineStyle = XlLineStyle.xlLineStyleNone;
		}

	}
}
