using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;


namespace Inforoom.ReportSystem
{
	class CombShortReport : CombReport
	{
		public CombShortReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам";
		}

		public override void ReadReportParams()
		{
			_reportType = (int)getReportParam("ReportType");
			_clientCode = (int)getReportParam("ClientCode");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
		}

		protected override void Calculate()
		{
			base.Calculate();
			DataTable dtNewRes = _dsReport.Tables["Results"].DefaultView.ToTable("Results", false, new string[] { "FullName", "FirmCr", "MinCost" });
			_dsReport.Tables.Remove("Results");
			_dsReport.Tables.Add(dtNewRes);
		}

		protected override void FormatLeaderAndPrices(MSExcel._Worksheet ws)
		{
			//Выравниваем все колонки по ширине
			//for (int i = 1; i <= _dsReport.Tables["Results"].Columns.Count; i++)
			//    ((MSExcel.Range)ws.Columns[i, Type.Missing]).AutoFit();
			//((MSExcel.Range)ws.get_Range(ws.Cells[1, 1], ws.Cells[1, _dsReport.Tables["Results"].Columns.Count])).EntireColumn.AutoFit();
			//((MSExcel.Range)ws.Columns.get_Range(ws.Columns[1, Type.Missing], ws.Columns[_dsReport.Tables["Results"].Columns.Count, Type.Missing])).EntireColumn.AutoFit();
			//ws.Columns.AutoFit();
			//((MSExcel.Range)ws.Columns[1, _dsReport.Tables["Results"].Columns.Count]).AutoFit();
		}

	}
}
