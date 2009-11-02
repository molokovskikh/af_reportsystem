using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using MSExcel = Microsoft.Office.Interop.Excel;


namespace Inforoom.ReportSystem
{
	class CombShortReport : CombReport
	{
		private bool _needProcessing = false; // Надо ли группировать и вычислять минимальные в Calculate

		public override void GenerateReport(ExecuteTemplate.ExecuteArgs e)
		{
			_needProcessing = false;

			base.GenerateReport(e);

			if (_reportParams.ContainsKey("ClientCodeEqual") &&
				((List<ulong>)_reportParams["ClientCodeEqual"]).Count > 0)
			{
				var clients = (List<ulong>)_reportParams["ClientCodeEqual"];
				foreach (ulong client in clients)
				{
					DataTable dtRes = _dsReport.Tables["Results"].Clone();
					_dsReport.Tables.Remove("Results");
					_clientCode = (int)client;

					base.GenerateReport(e);

					_dsReport.Tables["Results"].Merge(dtRes);
				}
				_needProcessing = true;
			}
		}

		public CombShortReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary)
			: base(ReportCode, ReportCaption, Conn, Temporary)
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

			if (_needProcessing)
				_dsReport.Tables.Add(dtNewRes);
			else
			{
				var rows = dtNewRes.Rows.Cast<DataRow>();
				var resTable = new DataTable("Results");
				resTable.Columns.Add("FullName");
				resTable.Columns.Add("FirmCr");
				resTable.Columns.Add("MinCost");

				var processedRows = from r in rows
									group r by new { name = r[0], producer = r[1] } into myGroup
									select resTable.Rows.Add(new object[] { myGroup.Key.name, myGroup.Key.producer, myGroup.Min(r => r[2]) });

				foreach (var row in processedRows)
				{ /* обработка данных (нужно перебрать все записи чтобы Linq сработал)*/}

				_dsReport.Tables.Add(resTable);
			}
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
