using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	//Специальный отчет прайс-листов
	public class SpecReport : ProviderReport
	{
		protected int _reportType;
		protected bool _showPercents;
		protected bool _reportIsFull;
		protected bool _reportSortedByPrice;

		protected int SourcePC, SourceRegionCode, FirmCode;
		protected string CustomerFirmName;

		protected string reportCaptionPreffix;

		public SpecReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
			reportCaptionPreffix = "Специальный отчет";
		}

		public override void ReadReportParams()
		{
			_reportType = (int)getReportParam("ReportType");
			_showPercents = (bool)getReportParam("ShowPercents");
			_reportIsFull = (bool)getReportParam("ReportIsFull");
			_reportSortedByPrice = (bool)getReportParam("ReportSortedByPrice");
			_clientCode = (int)getReportParam("ClientCode");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			//Выбираем 
			GetActivePricesT(e);
			GetAllCoreT(e);

			//Получили код поставщика, для которого делается отчет
			e.DataAdapter.SelectCommand.CommandText = @"
select 
  gr.FirmCode 
from 
  testreports.reports r,
  testreports.general_reports gr
where
    r.ReportCode = ?ReportCode
and gr.GeneralReportCode = r.GeneralReportCode";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.Add("ReportCode", _reportCode);
			FirmCode = Convert.ToInt32(e.DataAdapter.SelectCommand.ExecuteScalar());

			//Получаем код прайс-листа, регион и название поставщика, для которого делаем отчет
			e.DataAdapter.SelectCommand.CommandText = @"select FirmName, PriceCode, RegionCode from ActivePricesT where FirmCode = ?FirmCode limit 1";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.Add("FirmCode", FirmCode);
			DataTable dtCustomer = new DataTable();
			e.DataAdapter.Fill(dtCustomer);
			SourcePC = Convert.ToInt32(dtCustomer.Rows[0]["PriceCode"]);
			SourceRegionCode = Convert.ToInt32(dtCustomer.Rows[0]["RegionCode"]);
			CustomerFirmName = dtCustomer.Rows[0]["FirmName"].ToString();

			//Получили предложения интересующего прайс-листа в отдельную таблицу
			GetSourceCodes(e);

			//Получили лучшие предложения из всех прайс-листов с учетом требований
			GetMinPrice(e);

			Calculate();
		}

		protected virtual void Calculate()
		{
			//Кол-во первых фиксированных колонок
			int FirstColumnCount;

			DataTable dtCore = _dsReport.Tables["AllCoreT"];
			DataTable dtPrices = _dsReport.Tables["Prices"];

			DataTable dtRes = new DataTable("Results");
			_dsReport.Tables.Add(dtRes);
			dtRes.Columns.Add("Code");
			dtRes.Columns["Code"].Caption = "Код";
			dtRes.Columns.Add("FullName");
			dtRes.Columns["FullName"].Caption = "Наименование";
			dtRes.Columns.Add("FirmCr");
			dtRes.Columns["FirmCr"].Caption = "Производитель";
			dtRes.Columns.Add("CustomerCost", typeof(decimal));
			dtRes.Columns["CustomerCost"].Caption = CustomerFirmName;
			dtRes.Columns.Add("CustomerQuantity");
			dtRes.Columns["CustomerQuantity"].Caption = "Количество";
			dtRes.Columns.Add("MinCost", typeof(decimal));
			dtRes.Columns["MinCost"].Caption = "min";
			dtRes.Columns.Add("LeaderName");
			dtRes.Columns["LeaderName"].Caption = "Лидер";
			dtRes.Columns.Add("Differ", typeof(decimal));
			dtRes.Columns["Differ"].Caption = "Разница";
			dtRes.Columns.Add("DifferPercents", typeof(double));
			dtRes.Columns["DifferPercents"].Caption = "% разницы";
			dtRes.Columns.Add("MaxCost", typeof(decimal));
			dtRes.Columns["MaxCost"].Caption = "max";
			FirstColumnCount = dtRes.Columns.Count;

			int PriceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				dtRes.Columns.Add("Cost" + PriceIndex.ToString(), typeof(decimal));
				dtRes.Columns["Cost" + PriceIndex.ToString()].Caption = "Цена";
				if (!_showPercents)
				{
					dtRes.Columns.Add("Quantity" + PriceIndex.ToString());
					dtRes.Columns["Quantity" + PriceIndex.ToString()].Caption = "Кол-во";
				}
				else
				{
					dtRes.Columns.Add("Percents" + PriceIndex.ToString(), typeof(double));
					dtRes.Columns["Percents" + PriceIndex.ToString()].Caption = "% разницы";
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
				newrow["MaxCost"] = Convert.ToDecimal(drCatalog["MaxCost"]);

				//Если есть ID, то предложение SourcePC существует
				if (!(drCatalog["ID"] is DBNull))
				{
					newrow["Code"] = drCatalog["Code"];
					drsMin = dtCore.Select("RowID = " + drCatalog["ID"].ToString());
					newrow["CustomerCost"] = Convert.ToDecimal(drsMin[0]["Cost"]);
					newrow["CustomerQuantity"] = drsMin[0]["Quantity"];
					if (newrow["CustomerCost"].Equals(newrow["MinCost"]))
						newrow["LeaderName"] = "+";
				}

				//Если имя лидера неустановлено, то выставляем имя лидера
				if (newrow["LeaderName"] is DBNull)
				{
					//Устанавливаем разность между ценой SourcePC и минимальной ценой
					if (!(newrow["CustomerCost"] is DBNull))
					{
						newrow["Differ"] = (decimal)newrow["CustomerCost"] - (decimal)newrow["MinCost"];
						newrow["DifferPercents"] = Convert.ToDouble((((decimal)newrow["CustomerCost"] - (decimal)newrow["MinCost"]) * 100) / (decimal)newrow["CustomerCost"]);
					}

					//Выбираем позиции с минимальной ценой, отличные от SourcePC
					drsMin = dtCore.Select(
						"FullCode = " + drCatalog["FullCode"].ToString() +
						((_reportType <= 2) ? String.Empty : " and CodeFirmCr = " + drCatalog["Cfc"].ToString()) +
						" and Cost = " + ((decimal)drCatalog["MinCost"]).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
					if (drsMin.Length > 0)
					{
						List<string> LeaderNames = new List<string>();
						foreach (DataRow drmin in drsMin)
						{
							DataRow[] drs = dtPrices.Select(
								"PriceCode=" + drmin["PriceCode"].ToString() +
								" and RegionCode = " + drmin["RegionCode"].ToString());
							if (drs.Length > 0)
								if (!LeaderNames.Contains(drs[0]["FirmName"].ToString()))
									LeaderNames.Add(drs[0]["FirmName"].ToString());
						}
						newrow["LeaderName"] = String.Join("; ", LeaderNames.ToArray());
					}
				}
				else
				{
					//Ищем первую цену, которая будет больше минимальной цены
					drsMin = dtCore.Select(
						"FullCode = " + drCatalog["FullCode"].ToString() +
						" and PriceCode <> " + SourcePC.ToString() +
						((_reportType <= 2) ? String.Empty : " and CodeFirmCr = " + drCatalog["Cfc"].ToString()) +
						" and Cost > " + ((decimal)drCatalog["MinCost"]).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat),
						"Cost asc");
					if (drsMin.Length > 0)
					{
						newrow["Differ"] = (decimal)newrow["CustomerCost"] - Convert.ToDecimal(drsMin[0]["Cost"]);
						newrow["DifferPercents"] = Convert.ToDouble((((decimal)newrow["CustomerCost"] - Convert.ToDecimal(drsMin[0]["Cost"])) * 100) / (decimal)newrow["CustomerCost"]);
					}
				}

				//Выбираем позиции и сортируем по возрастанию цен
				drsMin = dtCore.Select(
					"FullCode = " + drCatalog["FullCode"].ToString() +
					((_reportType <= 2) ? String.Empty : "and CodeFirmCr = " + drCatalog["Cfc"].ToString()), 
					"Cost asc");
				foreach (DataRow dtPos in drsMin)
				{
					DataRow[] dr = dtPrices.Select("PriceCode=" + dtPos["PriceCode"].ToString() + " and RegionCode = " + dtPos["RegionCode"].ToString());
					//Проверка на случай получения прайса SourcePC, т.к. этот прайс не будет в dtPrices
					if (dr.Length > 0)
					{
						PriceIndex = dtPrices.Rows.IndexOf(dr[0]);

						//Если мы еще не установили значение у поставщика, то делаем это
						//раньше вставляли последнее значение, которое было максимальным
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


		protected void GetSourceCodes(ExecuteArgs e)
		{
			int EnabledPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					"select not ActivePricesT.DisabledByClient from ActivePricesT where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
					new MySqlParameter("SourcePC", SourcePC),
					new MySqlParameter("SourceRegionCode", SourceRegionCode)));
			if (EnabledPrice == 0)
			{
				e.DataAdapter.SelectCommand.CommandText = @"select FirmSegment from usersettings.clientsdata where FirmCode = ?ClientCode";
				e.DataAdapter.SelectCommand.Parameters.Clear();
				e.DataAdapter.SelectCommand.Parameters.Add("ClientCode", _clientCode);
				int ClientSegment = Convert.ToInt32(e.DataAdapter.SelectCommand.ExecuteScalar());
				if (ClientSegment == 0)
				{
					e.DataAdapter.SelectCommand.CommandText = @"
INSERT
INTO    AllCoreT
SELECT
        core0.id,
        ActivePricesT.PriceCode,
        ActivePricesT.regioncode,
        core0.fullcode,
        core0.Shortcode,
        codefirmcr,
        synonymcode,
        SynonymFirmCrCode,
        code,
        codecr,
        unit,
        volume,
        length(junk) >0,
        length(Await)>0,
        quantity,
        note,
        period,
        doc,
        RegistryCost,
        VitallyImportant,
        RequestRatio,
        MinBoundCost,
        round(BaseCost*ActivePricesT.UpCost,2)
FROM    farm.core0,
        ActivePricesT
WHERE   core0.firmcode = ActivePricesT.CostCode
        AND not ActivePricesT.AlowInt
        and ActivePricesT.PriceCode = ?SourcePC 
        and ActivePricesT.RegionCode = ?SourceRegionCode 
        AND ActivePricesT.Actual
        AND BaseCost is not null
        AND ActivePricesT.CostType=1;
INSERT
INTO    AllCoreT
SELECT
        core0.id,
        ActivePricesT.PriceCode,
        ActivePricesT.regioncode,
        core0.fullcode,
        core0.Shortcode,
        codefirmcr,
        synonymcode,
        SynonymFirmCrCode,
        code,
        codecr,
        unit,
        volume,
        length(junk) >0,
        length(Await)>0,
        quantity,
        note,
        period,
        doc,
        RegistryCost,
        VitallyImportant,
        RequestRatio,
        MinBoundCost,
        round(corecosts.cost*ActivePricesT.UpCost,2)
FROM    farm.core0,
        ActivePricesT,
        farm.corecosts
WHERE   core0.firmcode = ActivePricesT.PriceCode
        AND not ActivePricesT.AlowInt
        and ActivePricesT.PriceCode = ?SourcePC 
        and ActivePricesT.RegionCode = ?SourceRegionCode 
        AND ActivePricesT.Actual
        AND corecosts.cost is not null
        AND corecosts.Core_Id=core0.id
        and corecosts.PC_CostCode=ActivePricesT.CostCode
        AND ActivePricesT.CostType=0;
";
				}
				else
				{
					e.DataAdapter.SelectCommand.CommandText = @"
INSERT
INTO    AllCoreT
SELECT  core1.id,
        ActivePricesT.PriceCode,
        ActivePricesT.regioncode,
        core1.fullcode,
        core1.Shortcode,
        codefirmcr,
        synonymcode,
        SynonymFirmCrCode,
        code,
        codecr,
        unit,
        volume,
        length(junk) >0,
        length(Await)>0,
        quantity,
        note,
        period,
        doc,
        RegistryCost,
        VitallyImportant,
        RequestRatio,
        MinBoundCost,
        round(BaseCost*ActivePricesT.UpCost,2)
FROM    farm.core1,
        ActivePricesT
WHERE   core1.firmcode = ActivePricesT.CostCode
        AND not ActivePricesT.AlowInt
        AND not ActivePricesT.DisabledByClient
        and ActivePricesT.PriceCode = ?SourcePC 
        and ActivePricesT.RegionCode = ?SourceRegionCode 
        AND ActivePricesT.Actual
        AND BaseCost is not null;
";
				}
				e.DataAdapter.SelectCommand.CommandText += @"
UPDATE AllCoreT
        SET cost =MinCost
WHERE   MinCost  >cost
        AND MinCost is not null
        and PriceCode = ?SourcePC
        and RegionCode = ?SourceRegionCode;
";
				e.DataAdapter.SelectCommand.Parameters.Clear();
				e.DataAdapter.SelectCommand.Parameters.Add("SourcePC", SourcePC);
				e.DataAdapter.SelectCommand.Parameters.Add("SourceRegionCode", SourceRegionCode);
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
			}

			e.DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS TmpSourceCodes;
CREATE temporary table TmpSourceCodes( 
  ID int(32) unsigned, 
  PriceCode int(32) unsigned, 
  RegionCode int(32) unsigned, 
  Code char(20), 
  BaseCost decimal(8,2) unsigned, 
  FullCode int(32) unsigned, 
  CodeFirmCr int(32) unsigned, 
  SynonymCode int(32) unsigned, 
  SynonymFirmCrCode int(32) unsigned, 
  key ID(ID), 
  key FullCode(FullCode), 
  key CodeFirmCr(CodeFirmCr), 
  key SynonymFirmCrCode(SynonymFirmCrCode), 
  key SynonymCode(SynonymCode))engine=MEMORY PACK_KEYS = 0;
  INSERT INTO TmpSourceCodes 
Select 
  RowID, 
  PriceCode,
  RegionCode,
  Code, Cost, FullCode, Codefirmcr, SynonymCode, SynonymFirmCrCode 
FROM 
  AllCoreT 
WHERE 
    PriceCode = ?SourcePC 
and RegionCode = ?SourceRegionCode;";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.Add("SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.Add("SourceRegionCode", SourceRegionCode);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			e.DataAdapter.SelectCommand.CommandText = "select * from AllCoreT";
			e.DataAdapter.Fill(_dsReport, "AllCoreT");

			e.DataAdapter.SelectCommand.CommandText = @"
select 
  PriceCode, RegionCode, DateCurPrice, FirmName, Region 
from 
  ActivePricesT 
where 
  (PriceCode <> ?SourcePC or RegionCode <> ?SourceRegionCode)
  and not ActivePricesT.DisabledByClient
order by PosCount DESC";
			e.DataAdapter.SelectCommand.Parameters.Add("SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.Add("SourceRegionCode", SourceRegionCode);
			e.DataAdapter.Fill(_dsReport, "Prices");
		}

		protected void GetMinPrice(ExecuteArgs e)
		{
			/*
					Dim CommandText As String
					CommandText = "select " & _
								"c.FullCode as FullCode," & _
								"if(c1.fullcode=c.fullcode and c1.codefirmcr=c0.codefirmcr, s.Synonym, concat(C.Name, ' ', C.Form)) as Name," & _
								"concat(C.Name, ' ', C.Form) as OldName," & _
								"if(c1.fullcode=c.fullcode and c1.codefirmcr=c0.codefirmcr and c0.firmcode=?SourcePC, c1.basecost,round(if(c0.basecost*apt.UpCost<c0.minboundcost, c0.minboundcost, c0.basecost*apt.UpCost*exchange), 2)) as Cost," & _
								"c0.Quantity as Quantity," & _
								"c0.Period as Period," & _
								"apt.RegionCode as RegionCode," & _
								"apt.PriceCode as PriceCode," & _
								"if(c1.fullcode=c.fullcode and c1.codefirmcr=c0.codefirmcr, sfc.Synonym, Cfc.FirmCr) as FirmCr," & _
								"FirmCr as OldFirmCr,"

					If ReportType > 2 Then
						CommandText &= "cfc.codefirmcr As Cfc,"
					Else
						CommandText &= "0 As Cfc,"
					End If 

					'"c1.firmcode as FirmCode," & _
					CommandText &= "c1.code as Code," & _
						"if(c0.firmcode=?SourcePC, 0, 1) as SP," & _
						"apt.PosCount as PosCount," & _
						"c1.Id as CoreId" & _
					" from " & _
						"farm.catalogcurrency cc," & _
						"farm.catalog c," & _
						"farm.CatalogFirmCr cfc," & _
						"ActivePricesT apt,"

					If IsFull Then
						If ReportType > 2 Then
							CommandText &= "farm.core" + ClientSegment + " c0 left join TmpSourceCodes c1 on c1.fullcode=c0.fullcode "
						Else
							CommandText &= "farm.core" + ClientSegment + " c0 left join TmpSourceCodes c1 on c1.fullcode=c0.fullcode and c1.codefirmcr=c0.codefirmcr "
						End If
					Else
						CommandText &= "farm.core" + ClientSegment + " c0, TmpSourceCodes c1"
					End If

					CommandText &= " " & _
						"left join farm.synonym s on s.SynonymCode = c1.SynonymCode " & _
						"left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = c1.SynonymFirmCrCode" & _
					" where " & _
					"c.fullcode = c0.fullcode"

					If Not IsFull Then
						If ReportType > 2 Then
							CommandText &= " and c1.fullcode=c0.fullcode and c1.codefirmcr=c0.codefirmcr "
						Else
							CommandText &= " and c1.fullcode=c0.fullcode "
						End If
					End If

					CommandText &= "   and c0.firmcode=apt.pricecode" & _
						"   and cfc.codefirmcr=c0.codefirmcr" & _
						"   and cc.currency=c0.currency" '& _
					'"   and (((c0.firmcode=@SourcePC) and (c0.id=c1.id)))"

					
			        If ByClient Then
						CommandText &= "   and (((c0.firmcode=?SourcePC) and (c0.id=c1.id)))"
					Else
						CommandText &= "   and (((c0.firmcode=?AlienPrice)and(c0.Junk='')and(c0.Await='')))"
					End If
					'CommandText &= "   and (c0.FullCode = 29864)"
					'"   and s.SynonymCode = c1.SynonymCode" & _
					'"   and sfc.SynonymFirmCrCode = c1.SynonymFirmCrCode;" ' & _
					'" limit 1000;"
					Return CommandText
 
			 */
			string SqlCommandText = @"
select 
  c1.ID,
  c1.Code,
  c.FullCode as FullCode,
  ifnull(s.Synonym, concat(C.Name, ' ', C.Form)) as FullName,
  -- c0.cost, -- здесь должна быть минимальная цена
  -- c0.cost, -- здесь должна быть минимальная цена
  min(c0.cost) As MinCost, -- здесь должна быть минимальная цена
  max(c0.cost) As MaxCost, -- здесь должна быть минимальная цена";

			//Если отчет без учета производителя, то код не учитываем и выводим "-"
			if (_reportType <= 2)
				SqlCommandText += @"
  '-' as FirmCr,
  0 As Cfc ";
			else
				SqlCommandText += @"
  ifnull(sfc.Synonym, Cfc.FirmCr) as FirmCr,
  cfc.codefirmcr As Cfc ";

			SqlCommandText += @"
from 
 (
  farm.catalog c,
  farm.CatalogFirmCr cfc,";

			//Если отчет полный, то интересуют все прайс-листы, если нет, то только SourcePC
			if (_reportIsFull)
			{
				if (_reportType <= 2)
					SqlCommandText += @"
  AllCoreT c0 
 )
  left join TmpSourceCodes c1 on c1.fullcode=c0.fullcode ";
				else
					SqlCommandText += @"
  AllCoreT c0 
 )
  left join TmpSourceCodes c1 on c1.fullcode=c0.fullcode and c1.codefirmcr=c0.codefirmcr";
			}
			else
					SqlCommandText += @"
  AllCoreT c0, 
  TmpSourceCodes c1
 )";
				SqlCommandText += @"
  left join farm.synonym s on s.SynonymCode = c1.SynonymCode 
  left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = c1.SynonymFirmCrCode
where 
      c.fullcode = c0.fullcode
  and cfc.codefirmcr=c0.codefirmcr
  and (( ( (c0.PriceCode <> c1.PriceCode) or (c0.RegionCode <> c1.RegionCode) or (c1.id is null) ) and (c0.Junk =0) and (c0.Await=0) )
      or ( (c0.PriceCode = c1.PriceCode) and (c0.RegionCode = c1.RegionCode) and (c0.RowId = c1.id) ) )";

			//Если отчет не полный, то выбираем только те, которые есть в SourcePC
			if (!_reportIsFull)
			{
				if (_reportType <= 2)
					SqlCommandText += @"
and c1.fullcode=c0.fullcode ";
				else
					SqlCommandText += @"
and c1.fullcode=c0.fullcode and c1.codefirmcr=c0.codefirmcr ";
			}
			SqlCommandText += @"
group by c1.Code, c.FullCode, Cfc";
			if ((!_reportIsFull) && (_reportSortedByPrice))
				SqlCommandText += @"
order by c1.ID";
			else
				SqlCommandText += @"
order by c.FullCode, Cfc, c1.Code";
			e.DataAdapter.SelectCommand.CommandText = SqlCommandText;
			e.DataAdapter.Fill(_dsReport, "Catalog");
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
						for (int i = 0; i < 7; i++)
						{
							ws.Cells[3, i + 1] = res.Columns[i].Caption;
						}

						for (int i = 1; i <= 2; i++)
							for (int j = 1; j <= 10;j++ )
								((MSExcel.Range)ws.Cells[i, j]).Clear();

						//Код
						((MSExcel.Range)ws.Columns[1, Type.Missing]).AutoFit();
						//Наименование
						((MSExcel.Range)ws.Cells[3, 2]).ColumnWidth = 20;
						//Производитель
						((MSExcel.Range)ws.Cells[3, 3]).ColumnWidth = 10;
						//Количество
						if ((_reportType == 2) || (_reportType == 4))
							((MSExcel.Range)ws.Cells[3, 5]).ColumnWidth = 4;
						else
							((MSExcel.Range)ws.Cells[3, 5]).ColumnWidth = 0;
						//min
						((MSExcel.Range)ws.Cells[3, 6]).ColumnWidth = 6;
						//Лидер
						((MSExcel.Range)ws.Cells[3, 7]).ColumnWidth = 9;

						//Форматирование заголовков прайс-листов
						FormatLeaderAndPrices(ws);

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;
						//Устанавливаем цвет колонки "min"
						ws.get_Range("F3", "F" + (_dsReport.Tables["Results"].Rows.Count + 1).ToString()).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSeaGreen);
						//Устанавливаем цвет колонки "Лидер"
						ws.get_Range("G3", "G" + (_dsReport.Tables["Results"].Rows.Count + 1).ToString()).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSkyBlue);


						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[3, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//Замораживаем некоторые колонки и столбцы
						((MSExcel.Range)ws.get_Range("K4", System.Reflection.Missing.Value)).Select();
						exApp.ActiveWindow.FreezePanes = true;

						//Объединяем несколько ячеек, чтобы в них написать текст
						((MSExcel.Range)ws.get_Range("A1:J2", System.Reflection.Missing.Value)).Select();
						((MSExcel.Range)exApp.Selection).Merge(null);
						if (_reportType < 3)
							exApp.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " без учета производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();
						else
							exApp.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " с учетом производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();

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

		protected virtual void FormatLeaderAndPrices(MSExcel._Worksheet ws)
		{
			int ColumnPrefix = 11;
			//Разница
			((MSExcel.Range)ws.Cells[3, 8]).ColumnWidth = 6;
			ws.Cells[3, 8] = "Разница";
			//% разницы
			((MSExcel.Range)ws.Cells[3, 9]).ColumnWidth = 4;
			ws.Cells[3, 9] = "% разницы";
			//max
			((MSExcel.Range)ws.Cells[3, 10]).ColumnWidth = 6;
			ws.Cells[3, 10] = "max";

			int PriceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				//Устанавливаем название фирмы
				ws.Cells[1, ColumnPrefix + PriceIndex * 2] = drPrice["FirmName"].ToString();
				((MSExcel.Range)ws.Cells[1, ColumnPrefix + PriceIndex * 2]).ColumnWidth = 6;

				//Устанавливаем дату фирмы
				ws.Cells[2, ColumnPrefix + PriceIndex * 2] = drPrice["DateCurPrice"].ToString();
				//((MSExcel.Range)ws.Cells[2, ColumnPrefix + PriceIndex * 2 + 1]).ColumnWidth = 4;

				ws.Cells[3, ColumnPrefix + PriceIndex * 2] = "Цена";
				if (!_showPercents)
					ws.Cells[3, ColumnPrefix + PriceIndex * 2 + 1] = "Кол-во";
				else
					ws.Cells[3, ColumnPrefix + PriceIndex * 2 + 1] = "Разница в %";

				if ((_reportType == 2) || (_reportType == 4))
					((MSExcel.Range)ws.Cells[3, ColumnPrefix + PriceIndex * 2 + 1]).ColumnWidth = 4;
				else
					((MSExcel.Range)ws.Cells[3, ColumnPrefix + PriceIndex * 2 + 1]).ColumnWidth = 0;

				((MSExcel.Range)ws.Cells[1, ColumnPrefix + PriceIndex * 2 + 1]).Clear();
				((MSExcel.Range)ws.Cells[2, ColumnPrefix + PriceIndex * 2 + 1]).Clear();

				PriceIndex++;
			}
		}



	}
}
