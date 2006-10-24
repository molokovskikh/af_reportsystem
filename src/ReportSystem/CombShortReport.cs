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
		}

	}
}
