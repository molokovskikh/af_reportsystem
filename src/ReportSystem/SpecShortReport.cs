using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	public class SpecShortReport : SpecReport
	{
		public SpecShortReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам";
		}

		public override void ReadReportParams()
		{
			_reportType = (int)getReportParam("ReportType");
			_clientCode = (int)getReportParam("ClientCode");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
			_priceCode = (int)getReportParam("PriceCode");
			_reportIsFull = (bool)getReportParam("ReportIsFull");
		}

		protected override void Calculate()
		{
			base.Calculate();
			DataTable dtNewRes = _dsReport.Tables["Results"].DefaultView.ToTable("Results", false,
				new string[] { "Code", "FullName", "FirmCr", "CustomerCost", "CustomerQuantity", "MinCost", "LeaderName" });
			foreach (DataRow drRes in dtNewRes.Rows)
				if (!drRes["LeaderName"].Equals("+"))
					drRes["LeaderName"] = String.Empty;
			_dsReport.Tables.Remove("Results");
			_dsReport.Tables.Add(dtNewRes);
		}

		protected override void FormatLeaderAndPrices(MSExcel._Worksheet ws)
		{
			//Выравниваем все колонки по ширине
			//ws.Columns.AutoFit();
			//((MSExcel.Range)ws.Columns[1, _dsReport.Tables["Results"].Columns.Count]).AutoFit();
		}
	}
}
