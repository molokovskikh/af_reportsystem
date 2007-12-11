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
	//Специальный отчет прайс-листов
	public class SpecReport : ProviderReport
	{
		protected int _reportType;
		protected bool _showPercents;
		protected bool _reportIsFull;
		protected bool _reportSortedByPrice;
		//Расчитывать отчет по каталогу (CatalogId, Name, Form), если не установлено, то расчет будет производится по продуктам (ProductId)
		protected bool _calculateByCatalog;

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
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			//Выбираем 
			GetOffers(e);

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
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("ReportCode", _reportCode);
			FirmCode = Convert.ToInt32(e.DataAdapter.SelectCommand.ExecuteScalar());

			//Получаем код прайс-листа, регион и название поставщика, для которого делаем отчет
			e.DataAdapter.SelectCommand.CommandText = @"select FirmName, PriceCode, RegionCode from ActivePrices where FirmCode = ?FirmCode limit 1";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("FirmCode", FirmCode);
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

			//todo: посмотреть почему здесь используется таблицы AllCoreT и Prices
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
					drsMin = dtCore.Select("ID = " + drCatalog["ID"].ToString());
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
						"CatalogCode = " + drCatalog["CatalogCode"].ToString() +
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
						"CatalogCode = " + drCatalog["CatalogCode"].ToString() +
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
					"CatalogCode = " + drCatalog["CatalogCode"].ToString() +
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
					"select PriceCode from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
					new MySqlParameter("SourcePC", SourcePC),
					new MySqlParameter("SourceRegionCode", SourceRegionCode)));
			if (EnabledPrice == 0)
			{
				e.DataAdapter.SelectCommand.CommandText = @"
insert into Core
(Id, PriceCode, RegionCode, ProductId, Cost)
select
  FarmCore.Id,
  ActivePrices.PriceCode,
  ActivePrices.Regioncode,
  FarmCore.ProductId,
  if(FarmCore.MinBoundCost is null, round(FarmCore.BaseCost*ActivePrices.UpCost,2), if(FarmCore.MinBoundCost > round(FarmCore.BaseCost*ActivePrices.UpCost,2), FarmCore.MinBoundCost, round(FarmCore.BaseCost*ActivePrices.UpCost,2)))
FROM    
  farm.core0 FarmCore,
  ActivePrices
WHERE   
    FarmCore.Pricecode = ActivePrices.CostCode
and ActivePrices.PriceCode = ?SourcePC 
and ActivePrices.RegionCode = ?SourceRegionCode 
AND FarmCore.BaseCost is not null
AND ActivePrices.CostType=1;

insert into Core
(Id, PriceCode, RegionCode, ProductId, Cost)
select
  FarmCore.Id,
  ActivePrices.PriceCode,
  ActivePrices.Regioncode,
  FarmCore.ProductId,
  if(FarmCore.MinBoundCost is null, round(corecosts.cost*ActivePrices.UpCost,2), if(FarmCore.MinBoundCost > round(corecosts.cost*ActivePrices.UpCost,2), FarmCore.MinBoundCost, round(corecosts.cost*ActivePrices.UpCost,2)))
FROM    
  farm.core0 FarmCore,
  ActivePrices,
  farm.corecosts
WHERE   
    FarmCore.Pricecode = ActivePrices.CostCode
and ActivePrices.PriceCode = ?SourcePC 
and ActivePrices.RegionCode = ?SourceRegionCode 
AND corecosts.cost is not null
AND corecosts.Core_Id=FarmCore.id
and corecosts.PC_CostCode=ActivePrices.CostCode
AND ActivePrices.CostType=0;
";

				e.DataAdapter.SelectCommand.Parameters.Clear();
				e.DataAdapter.SelectCommand.Parameters.AddWithValue("SourcePC", SourcePC);
				e.DataAdapter.SelectCommand.Parameters.AddWithValue("SourceRegionCode", SourceRegionCode);
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
			}

			//Добавляем к таблице Core поле CatalogCode и заполняем его
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
  key SynonymCode(SynonymCode))engine=MEMORY PACK_KEYS = 0;
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
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("SourceRegionCode", SourceRegionCode);
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

			//todo: изменить заполнение в другую таблицу
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
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("SourceRegionCode", SourceRegionCode);
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
				SqlCommandText += " ifnull(s.Synonym, concat(catalognames.Name, ' ', catalogforms.Form)) as FullName, ";
			else
				SqlCommandText += " ifnull(s.Synonym, concat(catalognames.Name, ' ', catalogs.GetFullForm(AllPrices.productid))) as FullName, ";
			SqlCommandText += @"
  min(AllPrices.cost) As MinCost, -- здесь должна быть минимальная цена
  max(AllPrices.cost) As MaxCost, -- здесь должна быть минимальная цена";

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
  catalogs.products,
  catalogs.catalog,
  catalogs.catalognames,
  catalogs.catalogforms,
  farm.core0 FarmCore,
  farm.CatalogFirmCr cfc,";

			//Если отчет полный, то интересуют все прайс-листы, если нет, то только SourcePC
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
				SqlCommandText += @"
  left join farm.synonym s on s.SynonymCode = SourcePrice.SynonymCode 
  left join farm.synonymfirmcr sfc on sfc.SynonymFirmCrCode = SourcePrice.SynonymFirmCrCode
where 
      products.id = AllPrices.ProductId 
  and catalog.id = products.catalogid
  and catalognames.id = catalog.nameid
  and catalogforms.id = catalog.formid
  and FarmCore.Id = AllPrices.Id
  and cfc.codefirmcr=FarmCore.codefirmcr
  and (( ( (AllPrices.PriceCode <> SourcePrice.PriceCode) or (AllPrices.RegionCode <> SourcePrice.RegionCode) or (SourcePrice.id is null) ) and (FarmCore.Junk =0) and (FarmCore.Await=0) )
      or ( (AllPrices.PriceCode = SourcePrice.PriceCode) and (AllPrices.RegionCode = SourcePrice.RegionCode) and (AllPrices.Id = SourcePrice.id) ) )";

			//Если отчет не полный, то выбираем только те, которые есть в SourcePC
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
				exApp.DisplayAlerts = false;
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
						wb.SaveAs(FileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
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
				ws.Cells[2, ColumnPrefix + PriceIndex * 2] = drPrice["PriceDate"].ToString();
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
