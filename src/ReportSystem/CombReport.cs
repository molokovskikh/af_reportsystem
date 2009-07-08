using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.IO;

namespace Inforoom.ReportSystem
{
	//��������������� ����� �����-������
	public class CombReport : ProviderReport
	{
		/*
		 * ReportType 
		 *   1 - ��� ����� ������������� � ��� ���-��
		 *   2 - ��� ����� ������������� � � ���-���
		 *   3 - � ������ ������������� � ��� ���-��
		 *   4 - � ������ ������������� � � ���-���
		 * 
		 * ShowPercents
		 *   0 - ���������� ���-��
		 *   1 - ������ ���-�� ���������� ��������
		 * 
		 */

		protected int _reportType;
		protected bool _showPercents;
		//����������� ����� �� �������� (CatalogId, Name, Form), ���� �� �����������, �� ������ ����� ������������ �� ��������� (ProductId)
		protected bool _calculateByCatalog;

		protected string reportCaptionPreffix;

		public CombReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary)
			: base(ReportCode, ReportCaption, Conn, Temporary)
		{
			reportCaptionPreffix = "��������������� �����";
		}

		public override void ReadReportParams()
		{
			_reportType = (int)getReportParam("ReportType");
			_showPercents = (bool)getReportParam("ShowPercents");
			_clientCode = (int)getReportParam("ClientCode");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			//�������� 
			GetOffers(e);

			e.DataAdapter.SelectCommand.CommandText = "select " ;

			if (_calculateByCatalog)
				e.DataAdapter.SelectCommand.CommandText += "catalog.Id as CatalogCode, ";
			else
				e.DataAdapter.SelectCommand.CommandText += "products.Id as CatalogCode, ";

			e.DataAdapter.SelectCommand.CommandText += @"
  Core.Cost as Cost,
  ActivePrices.FirmName,
  FarmCore.Quantity, 
  Core.RegionCode, 
  Core.PriceCode, ";
			if (_reportType > 2)
			{
				e.DataAdapter.SelectCommand.CommandText += "FarmCore.codefirmcr";
			}
			else
			{
				e.DataAdapter.SelectCommand.CommandText += "0";
			}
			e.DataAdapter.SelectCommand.CommandText += @"
As Cfc 
from 
  Core, 
  farm.core0 FarmCore,
  catalogs.products,
  catalogs.catalog,
  catalogs.catalognames,
  catalogs.catalogforms,
  ActivePrices
where 
    FarmCore.id = Core.Id
and products.id = core.productid
and catalog.id = products.catalogid
and catalognames.id = catalog.NameId
and catalogforms.id = catalog.FormId
and Core.pricecode = ActivePrices.pricecode 
and Core.RegionCode = ActivePrices.RegionCode 
order by CatalogCode, Cfc, PositionCount DESC";
			e.DataAdapter.Fill(_dsReport, "Core");

			e.DataAdapter.SelectCommand.CommandText = "select  ";   
			if (_calculateByCatalog)
				e.DataAdapter.SelectCommand.CommandText += "catalog.Id as CatalogCode, left(concat(catalognames.Name, ' ', catalogforms.Form), 250) as Name, ";
			else
				e.DataAdapter.SelectCommand.CommandText += "products.Id as CatalogCode, left(concat(catalognames.Name, ' ', catalogs.GetFullForm(products.Id)), 250) as Name, ";

			e.DataAdapter.SelectCommand.CommandText += @"
  min(Core.Cost) as MinCost, 
  avg(Core.Cost) as AvgCost, 
  max(Core.Cost) as MaxCost, ";
			if (_reportType > 2)
			{
				e.DataAdapter.SelectCommand.CommandText += "FarmCore.codefirmcr as Cfc, left(farm.CatalogFirmCr.FirmCr, 250) as FirmCr ";
			}
			else
			{
				e.DataAdapter.SelectCommand.CommandText += "0 As Cfc, '-' as FirmCr ";
			}
			e.DataAdapter.SelectCommand.CommandText += @"
from 
  Core, 
  farm.core0 FarmCore,
  catalogs.products,
  catalogs.catalog,
  catalogs.catalognames,
  catalogs.catalogforms,
  ActivePrices"; 

			//���� ����� � ������ �������������, �� ���������� � �������� CatalogFirmCr
			if (_reportType > 2)
				e.DataAdapter.SelectCommand.CommandText += @",
  farm.CatalogFirmCr";
 
			e.DataAdapter.SelectCommand.CommandText += @"
where 
    FarmCore.id = Core.Id
and products.id = core.productid
and catalog.id = products.catalogid
and catalognames.id = catalog.NameId
and catalogforms.id = catalog.FormId
and Core.pricecode = ActivePrices.pricecode 
and Core.RegionCode = ActivePrices.RegionCode ";

			//���� ����� � ������ �������������, �� ���������� � �������� CatalogFirmCr
			if (_reportType > 2)
				e.DataAdapter.SelectCommand.CommandText += @"
and catalogfirmcr.codefirmcr = FarmCore.codefirmcr ";


			e.DataAdapter.SelectCommand.CommandText += @"
group by CatalogCode, Cfc
order by 2, 5";
			e.DataAdapter.Fill(_dsReport, "Catalog");

			e.DataAdapter.SelectCommand.CommandText = @"select PriceCode, RegionCode, PriceDate, FirmName from ActivePrices order by PositionCount DESC";
			e.DataAdapter.Fill(_dsReport, "Prices");

			Calculate();
		}

		public override void ReportToFile(string FileName)
		{
			DataTableToExcel(_dsReport.Tables["Results"], FileName);
			FormatExcel(FileName);
		}

		protected virtual void Calculate()
		{
			//���-�� ������ ������������� �������
			int FirstColumnCount;
			DataTable dtCore = _dsReport.Tables["Core"];
			DataTable dtPrices = _dsReport.Tables["Prices"];

			DataTable dtRes = new DataTable("Results");
			_dsReport.Tables.Add(dtRes);
			dtRes.Columns.Add("FullName");
			dtRes.Columns.Add("FirmCr");
			dtRes.Columns.Add("MinCost", typeof(decimal));
			dtRes.Columns.Add("AvgCost", typeof(decimal));
			dtRes.Columns.Add("MaxCost", typeof(decimal));
			dtRes.Columns.Add("LeaderName");
			FirstColumnCount = dtRes.Columns.Count;

			int PriceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				dtRes.Columns.Add("Cost" + PriceIndex.ToString(), typeof(decimal));
				if (!_showPercents)
					dtRes.Columns.Add("Quantity" + PriceIndex.ToString());
				else
					dtRes.Columns.Add("Percents" + PriceIndex.ToString(), typeof(double));
				PriceIndex++;
			}

			DataRow newrow;
			DataRow[] drsMin;
			newrow = dtRes.NewRow();
			dtRes.Rows.Add(newrow);

			foreach (DataRow drCatalog in _dsReport.Tables["Catalog"].Rows)
			{
				newrow = dtRes.NewRow();
				newrow["FullName"] = drCatalog["Name"];
				newrow["FirmCr"] = drCatalog["FirmCr"];
				newrow["MinCost"] = Convert.ToDecimal(drCatalog["MinCost"]);
				newrow["AvgCost"] = Convert.ToDecimal(drCatalog["AvgCost"]);
				newrow["MaxCost"] = Convert.ToDecimal(drCatalog["MaxCost"]);

				drsMin = dtCore.Select(
					"CatalogCode = " + drCatalog["CatalogCode"].ToString() +
					" and Cfc = " + drCatalog["Cfc"].ToString() + 
					" and Cost = " + ((decimal)drCatalog["MinCost"]).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
				if (drsMin.Length > 0)
					newrow["LeaderName"] = drsMin[0]["FirmName"];

				//�������� ������� � ��������� �� ����������� ���
				drsMin = dtCore.Select("CatalogCode = " + drCatalog["CatalogCode"].ToString() + "and Cfc = " + drCatalog["Cfc"].ToString(), "Cost asc");
				foreach (DataRow dtPos in drsMin)
				{
					DataRow dr = dtPrices.Select("PriceCode=" + dtPos["PriceCode"].ToString() + " and RegionCode = " + dtPos["RegionCode"].ToString())[0];
					PriceIndex = dtPrices.Rows.IndexOf(dr);

					//���� �� ��� �� ���������� �������� � ����������, �� ������ ���
					//������ ��������� ��������� ��������, ������� ���� ������������
					if (newrow[FirstColumnCount + PriceIndex * 2] is DBNull)
					{
						newrow[FirstColumnCount + PriceIndex * 2] = dtPos["Cost"];
						if ((_reportType == 2) || (_reportType == 4))
						{
							if (!_showPercents)
								newrow[FirstColumnCount + PriceIndex * 2 + 1] = dtPos["Quantity"];
							else
							{
								double mincost = Convert.ToDouble(newrow["MinCost"]), pricecost = Convert.ToDouble(dtPos["Cost"]);
								newrow[FirstColumnCount + PriceIndex * 2 + 1] = Math.Round(((pricecost - mincost) * 100) / pricecost, 0);
							}
						}
					}
				}

				dtRes.Rows.Add(newrow);
			}
		}

		protected void FormatExcel(string FileName)
		{
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				MSExcel.Workbook wb = exApp.Workbooks.Open(FileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try
				{
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

					try
					{
						ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);

						//����������� ��������� ������
						ws.Cells[2, 1] = "������������";
						((MSExcel.Range)ws.Cells[2, 1]).ColumnWidth = 20;
						ws.Cells[2, 2] = "�������������";
						((MSExcel.Range)ws.Cells[2, 2]).ColumnWidth = 10;
						ws.Cells[2, 3] = "���. ����";
						((MSExcel.Range)ws.Cells[2, 3]).ColumnWidth = 6;
						((MSExcel.Range)ws.Cells[1, 1]).Clear();
						((MSExcel.Range)ws.Cells[1, 2]).Clear();
						((MSExcel.Range)ws.Cells[1, 3]).Clear();
						
						//����������� ������� "�����" � ����� ��� ����
						FormatLeaderAndPrices(ws);

						//������ ������� �� ��� �������
						ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;
						//������������� ���� ������� "��� ����"
						ws.get_Range("C2", "C" + (_dsReport.Tables["Results"].Rows.Count + 1).ToString()).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSeaGreen);

						//������������� ����� �����
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//������������� ���������� �� ��� �������
						((MSExcel.Range)ws.get_Range(ws.Cells[2, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//������������ ��������� ������� � �������
						((MSExcel.Range)ws.get_Range("G3", System.Reflection.Missing.Value)).Select();
						exApp.ActiveWindow.FreezePanes = true;

						//���������� ��������� �����, ����� � ��� �������� �����
						((MSExcel.Range)ws.get_Range("A1:F1", System.Reflection.Missing.Value)).Select();
						((MSExcel.Range)exApp.Selection).Merge(null);
						if (_reportType < 3)
							exApp.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " ��� ����� ������������� ������ " + DateTime.Now.ToString();
						else
							exApp.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " � ������ ������������� ������ " + DateTime.Now.ToString();
					}
					finally
					{
						wb.SaveAs(FileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, MSExcel.XlSaveConflictResolution.xlLocalSessionChanges, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
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

		protected virtual void FormatLeaderAndPrices(MSExcel._Worksheet ws)
		{
			int ColumnPrefix = 7;

			ws.Cells[2, 4] = "������� ����";
			((MSExcel.Range)ws.Cells[2, 4]).ColumnWidth = 6;
			((MSExcel.Range)ws.Cells[1, 4]).Clear();
			ws.Cells[2, 5] = "����. ����";
			((MSExcel.Range)ws.Cells[2, 5]).ColumnWidth = 6;
			((MSExcel.Range)ws.Cells[1, 5]).Clear();
			ws.Cells[2, 6] = "�����";
			((MSExcel.Range)ws.Cells[2, 6]).ColumnWidth = 9;
			((MSExcel.Range)ws.Cells[1, 6]).Clear();

			int PriceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				//������������� �������� �����
				ws.Cells[1, ColumnPrefix + PriceIndex * 2] = drPrice["FirmName"].ToString();
				((MSExcel.Range)ws.Cells[1, ColumnPrefix + PriceIndex * 2]).ColumnWidth = 8;

				//������������� ���� �����
				ws.Cells[1, ColumnPrefix + PriceIndex * 2 + 1] = drPrice["PriceDate"].ToString();
				((MSExcel.Range)ws.Cells[1, ColumnPrefix + PriceIndex * 2 + 1]).ColumnWidth = 4;

				ws.Cells[2, ColumnPrefix + PriceIndex * 2] = "����";
				if (!_showPercents)
					ws.Cells[2, ColumnPrefix + PriceIndex * 2 + 1] = "���-��";
				else
					ws.Cells[2, ColumnPrefix + PriceIndex * 2 + 1] = "������� � %";

				PriceIndex++;
			}
			//������������� ���� ������� "�����"
			ws.get_Range("F2", "F" + (_dsReport.Tables["Results"].Rows.Count + 1).ToString()).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSkyBlue);
		}

	}
}
