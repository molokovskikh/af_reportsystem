using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Configuration;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem
{
	//����������� ����� �����-������
	public class SpecReport : ProviderReport
	{
		protected int _reportType;
		protected bool _showPercents;
		protected bool _reportIsFull;
		protected bool _reportSortedByPrice;
		//����������� ����� �� �������� (CatalogId, Name, Form), ���� �� �����������, �� ������ ����� ������������ �� ��������� (ProductId)
		protected bool _calculateByCatalog;

		protected int SourcePC, FirmCode;
		protected long SourceRegionCode;
		protected int _priceCode;
		protected string CustomerFirmName;

		protected string reportCaptionPreffix;

		protected string _suppliers;
		protected string _ignoredSuppliers;

		protected string _clientsNames = "";

		public SpecReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			reportCaptionPreffix = "����������� �����";
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_reportType = (int)getReportParam("ReportType");
			_showPercents = (bool)getReportParam("ShowPercents");
			_reportIsFull = (bool)getReportParam("ReportIsFull");
			_reportSortedByPrice = (bool)getReportParam("ReportSortedByPrice");
			_clientCode = (int)getReportParam("ClientCode");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
			_priceCode = (int)getReportParam("PriceCode");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			base.GenerateReport(e);

			ProfileHelper.Next("PreGetOffers");
			//���� �����-���� ����� 0, �� �� �� ����������, ������� ����� �����-���� ������������ �������, ��� �������� �������� �����
			if (_priceCode == 0)
				throw new ReportException("��� ������������ ������ �� ������ �������� \"�����-����\".");

			//��������� ��� ������� �����-����� ��� �������� ��� ������� �������, ������������ �������� �������� �����
			SourceRegionCode = Convert.ToInt64(
				MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
					@"select RegionCode 
	from usersettings.clientsdata 
where FirmCode = ?ClientCode
and not exists(select 1 from future.Clients where Id = ?ClientCode)
union
select RegionCode
	from future.Clients
where Id = ?ClientCode",
					new MySqlParameter("?ClientCode", _clientCode)));

			DataRow drPrice = MySqlHelper.ExecuteDataRow(
				ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
				@"
select 
  concat(clientsdata.ShortName, '(', pricesdata.PriceName, ') - ', regions.Region) as FirmName, 
  pricesdata.PriceCode, 
  clientsdata.RegionCode 
from 
  usersettings.pricesdata, 
  usersettings.clientsdata, 
  farm.regions 
where 
    pricesdata.PriceCode = ?PriceCode
and clientsdata.FirmCode = pricesdata.FirmCode
and regions.RegionCode = clientsdata.RegionCode
limit 1", new MySqlParameter("?PriceCode", _priceCode));

			if (drPrice == null)
				throw new ReportException(String.Format("�� ������ �����-���� � ����� {0}.", _priceCode));

			SourcePC = Convert.ToInt32(drPrice["PriceCode"]);
			CustomerFirmName = drPrice["FirmName"].ToString();

			//�������� ������������ �����-�����
			int ActualPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					@"
select 
  pc.PriceCode 
from 
  usersettings.pricescosts pc,
  usersettings.priceitems pim,
  farm.formrules fr 
where 
    pc.PriceCode = ?SourcePC
and pc.BaseCost = 1
and pim.Id = pc.PriceItemId
and fr.Id = pim.FormRuleId
and (to_days(now())-to_days(pim.PriceDate)) < fr.MaxOld",
					new MySqlParameter("?SourcePC", SourcePC)));
			if (ActualPrice == 0)
				throw new ReportException(String.Format("�����-���� {0} ({1}) �� �������� ����������.", CustomerFirmName, SourcePC));

			ProfileHelper.Next("GetOffers");
			//�������� 
			GetOffers(e, _SupplierNoise);
			ProfileHelper.Next("GetCodes");
			//�������� ����������� ������������� �����-����� � ��������� �������
			GetSourceCodes(e);
			ProfileHelper.Next("GetMinPrices");
			//�������� ������ ����������� �� ���� �����-������ � ������ ����������
			GetMinPrice(e);
			ProfileHelper.Next("Calculate");
			Calculate();
			ProfileHelper.End();

			DoCoreCheck();
		}

		private void DoCoreCheck()
		{
			args.DataAdapter.SelectCommand.CommandText = @"
select c.PriceCode
from Usersettings.Core c
left join farm.core0 c0 on c.Id = c0.Id
where c0.Id is null
group by c.pricecode";
			var data = new DataTable();
			args.DataAdapter.Fill(data);
			if (data.Rows.Count > 0)
			{
				Logger.DebugFormat("����� {1}, ����� ����� {0} ���������� ��� ��� �� ����� �����������",
					data.Rows.Cast<DataRow>().Select(r => Convert.ToUInt32(r["PriceCode"])).Implode(),
					_reportCode);
			}
		}

		protected virtual void Calculate()
		{
			//���-�� ������ ������������� �������
			int FirstColumnCount;

			//todo: ���������� ������ ����� ������������ ������� AllCoreT � Prices
			DataTable dtCore = _dsReport.Tables["AllCoreT"];
			DataTable dtPrices = _dsReport.Tables["Prices"];

			DataTable dtRes = new DataTable("Results");
			_dsReport.Tables.Add(dtRes);
			dtRes.Columns.Add("Code");
			dtRes.Columns["Code"].Caption = "���";
			dtRes.Columns.Add("FullName");
			dtRes.Columns["FullName"].Caption = "������������";
			dtRes.Columns.Add("FirmCr");
			dtRes.Columns["FirmCr"].Caption = "�������������";
			dtRes.Columns.Add("CustomerCost", typeof(decimal));
			dtRes.Columns["CustomerCost"].Caption = CustomerFirmName;
			dtRes.Columns.Add("CustomerQuantity");
			dtRes.Columns["CustomerQuantity"].Caption = "����������";
			dtRes.Columns.Add("MinCost", typeof(decimal));
			dtRes.Columns["MinCost"].Caption = "���. ����";
			dtRes.Columns.Add("LeaderName");
			dtRes.Columns["LeaderName"].Caption = "�����";
			dtRes.Columns.Add("Differ", typeof(decimal));
			dtRes.Columns["Differ"].Caption = "�������";
			dtRes.Columns.Add("DifferPercents", typeof(double));
			dtRes.Columns["DifferPercents"].Caption = "% �������";
			dtRes.Columns.Add("AvgCost", typeof(decimal));
			dtRes.Columns["AvgCost"].Caption = "������� ����";
			dtRes.Columns.Add("MaxCost", typeof(decimal));
			dtRes.Columns["MaxCost"].Caption = "����. ����";
			FirstColumnCount = dtRes.Columns.Count;

			int PriceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				dtRes.Columns.Add("Cost" + PriceIndex.ToString(), typeof(decimal));
				dtRes.Columns["Cost" + PriceIndex.ToString()].Caption = "����";
				if (!_showPercents)
				{
					dtRes.Columns.Add("Quantity" + PriceIndex.ToString());
					dtRes.Columns["Quantity" + PriceIndex.ToString()].Caption = "���-��";
				}
				else
				{
					dtRes.Columns.Add("Percents" + PriceIndex.ToString(), typeof(double));
					dtRes.Columns["Percents" + PriceIndex.ToString()].Caption = "% �������";
				}
				PriceIndex++;
			}

			DataRow newrow;
			DataRow[] drsMin;
			newrow = dtRes.NewRow();
			dtRes.Rows.Add(newrow);
			newrow = dtRes.NewRow();
			dtRes.Rows.Add(newrow);

			foreach (DataRow drCatalog in _dsReport.Tables["Catalog"].Rows)
			{
				newrow = dtRes.NewRow();
				newrow["FullName"] = drCatalog["FullName"];
				newrow["FirmCr"] = drCatalog["FirmCr"];
				newrow["MinCost"] = Convert.ToDecimal(drCatalog["MinCost"]);
				newrow["AvgCost"] = Convert.ToDecimal(drCatalog["AvgCost"]);
				newrow["MaxCost"] = Convert.ToDecimal(drCatalog["MaxCost"]);

				//���� ���� ID, �� �� ����� ��������� ���� Code �, ��������, ��������� ����   ����������� SourcePC ����������
				if (!(drCatalog["ID"] is DBNull))
				{
					newrow["Code"] = drCatalog["Code"];
					//���������� ����� ����������� �� ������ ������� �� ������������� �����-�����
					drsMin = dtCore.Select("ID = " + drCatalog["ID"].ToString());
					//���� � Core ����������� �� ������� SourcePC �� ����������, �� �����-���� ������������� ��� �� ������� �������� � �����
					//� ���� ������ ������ ���� �� ����������� � � ��������� ����� �����-���� �� ���������
					if ((drsMin.Length > 0) && !(drsMin[0]["Cost"] is DBNull))
					{
						newrow["CustomerCost"] = Convert.ToDecimal(drsMin[0]["Cost"]);
						newrow["CustomerQuantity"] = drsMin[0]["Quantity"];
						if (newrow["CustomerCost"].Equals(newrow["MinCost"]))
							newrow["LeaderName"] = "+";
					}
				}

				//���� ��� ������ �������������, �� ���������� ��� ������
				if (newrow["LeaderName"] is DBNull)
				{
					//������������� �������� ����� ����� SourcePC � ����������� �����
					if (!(newrow["CustomerCost"] is DBNull))
					{
						newrow["Differ"] = (decimal)newrow["CustomerCost"] - (decimal)newrow["MinCost"];
						newrow["DifferPercents"] = Convert.ToDouble((((decimal)newrow["CustomerCost"] - (decimal)newrow["MinCost"]) * 100) / (decimal)newrow["CustomerCost"]);
					}

					//�������� ������� � ����������� �����, �������� �� SourcePC
					drsMin = dtCore.Select(string.Format("CatalogCode = {0}{1} and Cost = {2}", 
						drCatalog["CatalogCode"], 
						GetProducerFilter(drCatalog),
						((decimal) drCatalog["MinCost"]).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat)));

					if (drsMin.Length > 0)
					{
						var LeaderNames = new List<string>();
						foreach (DataRow drmin in drsMin)
						{
							DataRow[] drs = dtPrices.Select(
								"PriceCode=" + drmin["PriceCode"] +
								" and RegionCode = " + drmin["RegionCode"]);
							if (drs.Length > 0)
								if (!LeaderNames.Contains(drs[0]["FirmName"].ToString()))
									LeaderNames.Add(drs[0]["FirmName"].ToString());
						}
						newrow["LeaderName"] = String.Join("; ", LeaderNames.ToArray());
					}
				}
				else
				{
					//���� ������ ����, ������� ����� ������ ����������� ����
					drsMin = dtCore.Select(
						"CatalogCode = " + drCatalog["CatalogCode"] +
						" and PriceCode <> " + SourcePC +
						GetProducerFilter(drCatalog) +
						" and Cost > " + ((decimal)drCatalog["MinCost"]).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat),
						"Cost asc");

					if (drsMin.Length > 0)
					{
						newrow["Differ"] = (decimal)newrow["CustomerCost"] - Convert.ToDecimal(drsMin[0]["Cost"]);
						newrow["DifferPercents"] = Convert.ToDouble((((decimal)newrow["CustomerCost"] - Convert.ToDecimal(drsMin[0]["Cost"])) * 100) / (decimal)newrow["CustomerCost"]);
					}
				}

				//�������� ������� � ��������� �� ����������� ��� ��� ����, ����� �� ������� �����-����� ������� ����������� ���� �� ������ � ���� �� CatalogCode
				drsMin = dtCore.Select(
					"CatalogCode = " + drCatalog["CatalogCode"] + GetProducerFilter(drCatalog),
					"Cost asc");

				foreach (DataRow dtPos in drsMin)
				{
					DataRow[] dr = dtPrices.Select("PriceCode=" + dtPos["PriceCode"].ToString() + " and RegionCode = " + dtPos["RegionCode"].ToString());
					//�������� �� ������ ��������� ������ SourcePC, �.�. ���� ����� �� ����� � dtPrices
					if (dr.Length > 0)
					{
						PriceIndex = dtPrices.Rows.IndexOf(dr[0]);

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
				}

				dtRes.Rows.Add(newrow);
			}
		}

		private string GetProducerFilter(DataRow drCatalog)
		{
			if (_reportType <= 2)
				return "";
			if (drCatalog["Cfc"] == DBNull.Value)
				return " and CodeFirmCr is null";
			return " and CodeFirmCr = " + drCatalog["Cfc"];
		}

		protected void GetSourceCodes(ExecuteArgs e)
		{
			int EnabledPrice = Convert.ToInt32(			
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					"select PriceCode from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
					new MySqlParameter("?SourcePC", SourcePC),
					new MySqlParameter("?SourceRegionCode", SourceRegionCode)));

			//��������� � ������� Core ���� CatalogCode � ��������� ���
			e.DataAdapter.SelectCommand.CommandText = "alter table Core add column CatalogCode int unsigned, add key CatalogCode(CatalogCode);";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			if (_calculateByCatalog)
				e.DataAdapter.SelectCommand.CommandText = "update Core, catalogs.products set Core.CatalogCode = products.CatalogId where products.Id = Core.ProductId;";
			else
				e.DataAdapter.SelectCommand.CommandText = "update Core set CatalogCode = ProductId;";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS TmpSourceCodes;
CREATE temporary table TmpSourceCodes( 
  ID int(32) unsigned, 
  PriceCode int(32) unsigned, 
  RegionCode int(32) unsigned, 
  Code char(20), 
  BaseCost decimal(8,2) unsigned, 
  CatalogCode int(32) unsigned, 
  CodeFirmCr int(32) unsigned, 
  SynonymCode int(32) unsigned, 
  SynonymFirmCrCode int(32) unsigned, 
  key ID(ID), 
  key CatalogCode(CatalogCode), 
  key CodeFirmCr(CodeFirmCr), 
  key SynonymFirmCrCode(SynonymFirmCrCode), 
  key SynonymCode(SynonymCode))engine=MEMORY PACK_KEYS = 0;";

			if (EnabledPrice == 0)
			{
				//���� �����-���� �� ������� �������� ��� �����-���� ��������������, �� ��������� ��� � ������� ���������� TmpSourceCodes, �� � ������ NULL
				e.DataAdapter.SelectCommand.CommandText += @"
INSERT INTO TmpSourceCodes 
Select 
  FarmCore.ID, 
  FarmCore.PriceCode,
  ?SourceRegionCode as RegionCode,
  FarmCore.Code,
  NULL,";
				if (_calculateByCatalog)
					e.DataAdapter.SelectCommand.CommandText += "Products.CatalogId, ";
				else
					e.DataAdapter.SelectCommand.CommandText += "Products.Id, ";
				e.DataAdapter.SelectCommand.CommandText += @"
  FarmCore.CodeFirmCr,
  FarmCore.SynonymCode,
  FarmCore.SynonymFirmCrCode
FROM 
  (
  farm.core0 FarmCore,
  catalogs.products
  )
  left join farm.corecosts cc on cc.Core_Id = FarmCore.id and cc.PC_CostCode = FarmCore.PriceCode
WHERE 
    FarmCore.PriceCode = ?SourcePC 
and products.id = FarmCore.ProductId;";
			}
			else
			{
				e.DataAdapter.SelectCommand.CommandText += @"
INSERT INTO TmpSourceCodes 
Select 
  Core.ID, 
  Core.PriceCode,
  Core.RegionCode,
  FarmCore.Code,
  Core.Cost,";
			if (_calculateByCatalog)
				e.DataAdapter.SelectCommand.CommandText += "Products.CatalogId, ";
			else
				e.DataAdapter.SelectCommand.CommandText += "Products.Id, ";
			e.DataAdapter.SelectCommand.CommandText += @"
  FarmCore.CodeFirmCr,
  FarmCore.SynonymCode,
  FarmCore.SynonymFirmCrCode
FROM 
  Core,
  farm.core0 FarmCore,
  catalogs.products
WHERE 
    Core.PriceCode = ?SourcePC 
and FarmCore.id = Core.Id
and products.id = Core.ProductId
and Core.RegionCode = ?SourceRegionCode;";
			}

  			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

e.DataAdapter.SelectCommand.CommandText = @"
select 
  Core.Id,
  Core.CatalogCode,
  FarmCore.CodeFirmCr,
  Core.Cost,
  Core.PriceCode,
  Core.RegionCode,
  FarmCore.Quantity 
from 
  Core,
  farm.core0 FarmCore
where
  FarmCore.Id = core.id";

			//todo: �������� ���������� � ������ �������
			e.DataAdapter.Fill(_dsReport, "AllCoreT");

			e.DataAdapter.SelectCommand.CommandText = @"
select 
  ActivePrices.PriceCode, ActivePrices.RegionCode, ActivePrices.PriceDate, ActivePrices.FirmName
from 
  ActivePrices 
where 
  (ActivePrices.PriceCode <> ?SourcePC or ActivePrices.RegionCode <> ?SourceRegionCode)
order by ActivePrices.PositionCount DESC";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
			e.DataAdapter.Fill(_dsReport, "Prices");
		}

		protected void GetMinPrice(ExecuteArgs e)
		{
			string SqlCommandText = @"
select 
  SourcePrice.ID,
  SourcePrice.Code,
  AllPrices.CatalogCode, ";
			if (_calculateByCatalog)
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetCatalogProductNameSubquery("AllPrices.ProductId"));
			else
				SqlCommandText += String.Format(" ifnull(s.Synonym, {0}) as FullName, ", GetProductNameSubquery("AllPrices.ProductId"));
			SqlCommandText += @"
  min(AllPrices.cost) As MinCost, -- ����� ������ ���� ����������� ����
  avg(AllPrices.cost) As AvgCost, -- ����� ������ ���� ������� ����
  max(AllPrices.cost) As MaxCost, -- ����� ������ ���� ����������� ����";

			//���� ����� ��� ����� �������������, �� ��� �� ��������� � ������� "-"
			if (_reportType <= 2)
				SqlCommandText += @"
  '-' as FirmCr,
  0 As Cfc ";
			else
				SqlCommandText += @"
  ifnull(sfc.Synonym, Cfc.Name) as FirmCr,
  cfc.Id As Cfc ";

			SqlCommandText += @"
from 
 (
  catalogs.products,
  farm.core0 FarmCore,";

			//���� ����� ������, �� ���������� ��� �����-�����, ���� ���, �� ������ SourcePC
			if (_reportIsFull)
			{
				if (_reportType <= 2)
					SqlCommandText += @"
  Core AllPrices 
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
  Core AllPrices 
 )
  left join TmpSourceCodes SourcePrice on SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=FarmCore.codefirmcr";
			}
			else
				SqlCommandText += @"
  Core AllPrices, 
  TmpSourceCodes SourcePrice
 )";
			//���� ����� � ������ �������������, �� ���������� � �������� Producers
			if (_reportType > 2)
				SqlCommandText += @"
  left join catalogs.Producers cfc on cfc.Id = FarmCore.codefirmcr";

				SqlCommandText += @"
  left join farm.synonym s on s.SynonymCode = SourcePrice.SynonymCode 
  left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = SourcePrice.SynonymFirmCrCode
where 
  products.id = AllPrices.ProductId 
  and FarmCore.Id = AllPrices.Id";

				SqlCommandText += @"
  and (( ( (AllPrices.PriceCode <> SourcePrice.PriceCode) or (AllPrices.RegionCode <> SourcePrice.RegionCode) or (SourcePrice.id is null) ) and (FarmCore.Junk =0) and (FarmCore.Await=0) )
      or ( (AllPrices.PriceCode = SourcePrice.PriceCode) and (AllPrices.RegionCode = SourcePrice.RegionCode) and (AllPrices.Id = SourcePrice.id) ) )";

			//���� ����� �� ������, �� �������� ������ ��, ������� ���� � SourcePC
			if (!_reportIsFull)
			{
				if (_reportType <= 2)
					SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode ";
				else
					SqlCommandText += @"
and SourcePrice.CatalogCode=AllPrices.CatalogCode and SourcePrice.codefirmcr=FarmCore.codefirmcr ";
			}
			SqlCommandText += @"
group by SourcePrice.Code, AllPrices.CatalogCode, Cfc";
			if ((!_reportIsFull) && (_reportSortedByPrice))
				SqlCommandText += @"
order by SourcePrice.ID";
			else
				SqlCommandText += @"
order by FullName, FirmCr";
			e.DataAdapter.SelectCommand.CommandText = SqlCommandText;
			e.DataAdapter.Fill(_dsReport, "Catalog");
		}

		protected override void FormatExcel(string FileName)
		{
			UseExcel.Workbook(FileName, wb => {
				var ws = (_Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

				ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);
				ws.Activate();

				var result = _dsReport.Tables["Results"];
				var tableBeginRowIndex = 3;
				var rowCount = result.Rows.Count;
				var columnCount = result.Columns.Count;

				if (!String.IsNullOrEmpty(_clientsNames)) // ��������� ������ ����� �������� ��������� ������
					tableBeginRowIndex = PutHeader(ws, tableBeginRowIndex, 12, String.Format("��������� ������: {0}", _clientsNames));
				if (!String.IsNullOrEmpty(_suppliers))
					tableBeginRowIndex = PutHeader(ws, tableBeginRowIndex, 12, String.Format("������ �����������: {0}", _suppliers));
				if (!String.IsNullOrEmpty(_ignoredSuppliers))
					tableBeginRowIndex = PutHeader(ws, tableBeginRowIndex, 12, String.Format("������������ ����������: {0}", _ignoredSuppliers));

				var lastRowIndex = rowCount + tableBeginRowIndex;

				for (var i = 0; i < result.Columns.Count; i++)
					ws.Cells[tableBeginRowIndex, i + 1] = result.Columns[i].Caption;

				//���
				((Range)ws.Columns[1, Type.Missing]).AutoFit();
				//������������
				((Range)ws.Cells[tableBeginRowIndex, 2]).ColumnWidth = 20;
				//�������������
				((Range)ws.Cells[tableBeginRowIndex, 3]).ColumnWidth = 10;
				//����������
				if ((_reportType == 2) || (_reportType == 4))
					((Range)ws.Cells[tableBeginRowIndex, 5]).ColumnWidth = 4;
				else
					((Range)ws.Cells[tableBeginRowIndex, 5]).ColumnWidth = 0;
				//min
				((Range)ws.Cells[tableBeginRowIndex, 6]).ColumnWidth = 6;
				//�����
				((Range)ws.Cells[tableBeginRowIndex, 7]).ColumnWidth = 9;

				//�������������� ���������� �����-������
				FormatLeaderAndPrices(ws);

				//������ ������� �� ��� �������
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[lastRowIndex, columnCount]].Borders.Weight = XlBorderWeight.xlThin;
				//������������� ���� ������� "min"
				ws.Range["F" + tableBeginRowIndex, "F" + lastRowIndex].Interior.Color = ColorTranslator.ToOle(Color.LightSeaGreen);
				//������������� ���� ������� "�����"
				ws.Range["G" + tableBeginRowIndex, "G" + lastRowIndex].Interior.Color = ColorTranslator.ToOle(Color.LightSkyBlue);

				//������������� ����� �����
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";

				//������������� ���������� �� ��� �������
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[rowCount, columnCount]].Select();
				((Range)wb.Application.Selection).AutoFilter(1, Missing.Value, XlAutoFilterOperator.xlAnd, Missing.Value, true);

				//������������ ��������� ������� � �������
				ws.Range["L4", Missing.Value].Select();
				wb.Application.ActiveWindow.FreezePanes = true;

				//���������� ��������� �����, ����� � ��� �������� �����
				ws.Range["A1:K2", Missing.Value].Select();
				((Range)wb.Application.Selection).Merge(null);
				if (_reportType < 3)
					wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " ��� ����� ������������� �� ������ " + CustomerFirmName + " ������ " + DateTime.Now.ToString();
				else
					wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " � ������ ������������� �� ������ " + CustomerFirmName + " ������ " + DateTime.Now.ToString();
			});
		}

		private int PutHeader(_Worksheet ws, int beginRow, int columnCount, string message)
		{
			((Range) ws.Cells[beginRow + 1, 1]).Select();
			var row = ((Range) ws.Application.Selection).EntireRow;
			row.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);
			row.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);
			row.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);

			beginRow += 3;
			var range = ws.Range[
				ws.Cells[beginRow - 3, 1], 
				ws.Cells[beginRow - 1, columnCount]];
			range.Select();
			((Range)ws.Application.Selection).Merge();
			var activeCell = ws.Application.ActiveCell;
			activeCell.FormulaR1C1 = message;
			activeCell.WrapText = true;
			activeCell.HorizontalAlignment = XlHAlign.xlHAlignLeft;
			activeCell.VerticalAlignment = XlVAlign.xlVAlignTop;
			return beginRow;
		}

		protected virtual void FormatLeaderAndPrices(_Worksheet ws)
		{
			int ColumnPrefix = 12;
			//�������
			((Range)ws.Cells[3, 8]).ColumnWidth = 6;
			ws.Cells[3, 8] = "�������";
			//% �������
			((Range)ws.Cells[3, 9]).ColumnWidth = 4;
			ws.Cells[3, 9] = "% �������";
			//�������
			((Range)ws.Cells[3, 10]).ColumnWidth = 6;
			ws.Cells[3, 10] = "������� ����";
			//max
			((Range)ws.Cells[3, 11]).ColumnWidth = 6;
			ws.Cells[3, 11] = "����. ����";

			int PriceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				//������������� �������� �����
				ws.Cells[1, ColumnPrefix + PriceIndex * 2] = drPrice["FirmName"].ToString();
				((Range)ws.Cells[1, ColumnPrefix + PriceIndex * 2]).ColumnWidth = 6;

				//������������� ���� �����
				ws.Cells[2, ColumnPrefix + PriceIndex * 2] = drPrice["PriceDate"].ToString();
				//((MSExcel.Range)ws.Cells[2, ColumnPrefix + PriceIndex * 2 + 1]).ColumnWidth = 4;

				ws.Cells[3, ColumnPrefix + PriceIndex * 2] = "����";
				if (!_showPercents)
					ws.Cells[3, ColumnPrefix + PriceIndex * 2 + 1] = "���-��";
				else
					ws.Cells[3, ColumnPrefix + PriceIndex * 2 + 1] = "������� � %";

				if ((_reportType == 2) || (_reportType == 4))
					((Range)ws.Cells[3, ColumnPrefix + PriceIndex * 2 + 1]).ColumnWidth = 4;
				else
					((Range)ws.Cells[3, ColumnPrefix + PriceIndex * 2 + 1]).ColumnWidth = 0;

				((Range)ws.Cells[1, ColumnPrefix + PriceIndex * 2 + 1]).Clear();
				((Range)ws.Cells[2, ColumnPrefix + PriceIndex * 2 + 1]).Clear();

				PriceIndex++;
			}
		}
	}
}
