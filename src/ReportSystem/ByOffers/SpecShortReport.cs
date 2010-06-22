using System;
using ExecuteTemplate;
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

		public override void GenerateReport(ExecuteArgs e)
		{
			base.GenerateReport(e);

			_suppliers = GetSuppliers(e);
			_ignoredSuppliers = GetIgnoredSuppliers(e);
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
				new[] { "Code", "FullName", "FirmCr", "CustomerCost", "CustomerQuantity", "MinCost", "LeaderName" });
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

		public override bool DbfSupported
		{
			get
			{
				return true;
			}
		}

		protected override void DataTableToDbf(DataTable dtExport, string fileName)
		{
			dtExport.Rows[0].Delete(); // обрезаем две первые строчки
			dtExport.Rows[0].Delete(); // ибо они пустые, ибо оставлены под шапку в Excel

			dtExport.Columns[0].ColumnName = "CODE";
			dtExport.Columns[1].ColumnName = "PRODUCT";
			dtExport.Columns[2].ColumnName = "PRODUCER";
			dtExport.Columns[3].ColumnName = "PRICECOST";
			dtExport.Columns[4].ColumnName = "QUANTITY";
			dtExport.Columns[5].ColumnName = "MINCOST";
			dtExport.Columns[6].ColumnName = "LEADER";

			if ((_reportType != 2) && (_reportType != 4))
				dtExport.Columns.Remove(dtExport.Columns[4]);

			base.DataTableToDbf(dtExport, fileName);
		}
	}
}
