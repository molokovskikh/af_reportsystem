using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
	//Специальный отчет прайс-листов
	public class SpecReport : ProviderReport
	{
		//1 - без учета производителя и с колчеством
		//2 - без учета производителя и колчеством
		//3 - с учетом производителя и без колчеством
		//4 - с учетом производителя и с колчеством
		protected int _reportType;
		protected bool _showPercents;
		protected bool _reportIsFull;
		protected bool _reportSortedByPrice;
		//Расчитывать отчет по каталогу (CatalogId, Name, Form), если не установлено, то расчет будет производится по продуктам (ProductId)
		protected bool _calculateByCatalog;

		protected int SourcePC, FirmCode;
		protected ulong SourceRegionCode;
		protected int _priceCode;
		protected string CustomerFirmName;

		protected string reportCaptionPreffix;

		protected string _suppliers;
		protected string _ignoredSuppliers;

		protected string _clientsNames = "";

		protected bool WithoutAssortmentPrice;

		protected bool _showCodeCr;

		protected bool _codesWithoutProducer;
		//количество столбцпв до начала блоков прайс листов
		private int firstColumnCount;
		//количество столбцов в блоке прайс листа
		private int priceBlockSize;

		protected SpecReport()// конструктор для возможности тестирования
		{}

		public SpecReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			reportCaptionPreffix = "Специальный отчет";
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_reportType = (int)getReportParam("ReportType");
			_showPercents = (bool)getReportParam("ShowPercents");
			_reportIsFull = (bool)getReportParam("ReportIsFull");
			_reportSortedByPrice = (bool)getReportParam("ReportSortedByPrice");
			if(!_byBaseCosts && !_isRetail)
				_clientCode = (int)getReportParam("ClientCode");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
			_priceCode = (int)getReportParam("PriceCode");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			base.GenerateReport(e);

			ProfileHelper.Next("PreGetOffers");
			//Если прайс-лист равен 0, то он не установлен, поэтому берем прайс-лист относительно клиента, для которого делается отчет
			if (_priceCode == 0)
				throw new ReportException("Для специального отчета не указан параметр \"Прайс-лист\".");
			if (_byBaseCosts)
			{   // Отчет готовится по базовым ценам
				//Заполняем код региона прайс-листа как домашний код поставщика этого прайс-листа
				SourceRegionCode = Convert.ToUInt64(
					MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
											  @"select s.HomeRegion
	from usersettings.PricesData pd
	inner join future.suppliers s on pd.FirmCode = s.Id
	and pd.PriceCode = ?PriceCode;",
											  new MySqlParameter("?PriceCode", _priceCode)));
			}
			else
			{   // отчет готовится по клиенту
				//Заполняем код региона прайс-листа как домашний код региона клиента, относительно которого строится отчет			
				SourceRegionCode = Convert.ToUInt64(
					MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
											  @"select RegionCode
	from future.Clients
where Id = ?ClientCode",
											  new MySqlParameter("?ClientCode", _clientCode)));
			}

			SourcePC = _priceCode;
			CustomerFirmName = GetSupplierName(_priceCode);

			//Проверка актуальности прайс-листа
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
#if !DEBUG
			if (ActualPrice == 0)
				throw new ReportException(String.Format("Прайс-лист {0} ({1}) не является актуальным.", CustomerFirmName, SourcePC));
#endif

			ProfileHelper.Next("GetOffers");
			//Выбираем 
			GetOffers(_SupplierNoise);
			ProfileHelper.Next("GetCodes");
			//Получили предложения интересующего прайс-листа в отдельную таблицу
			GetSourceCodes(e);
			ProfileHelper.Next("GetMinPrices");
			//Получили лучшие предложения из всех прайс-листов с учетом требований
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
				Logger.DebugFormat("Отчет {1}, Прайс листы {0} обновились для них не будет предложений",
					data.Rows.Cast<DataRow>().Select(r => Convert.ToUInt32(r["PriceCode"])).Implode(),
					_reportCode);
			}
		}

		protected virtual void Calculate()
		{
			//todo: посмотреть почему здесь используется таблицы AllCoreT и Prices
			var dtCore = _dsReport.Tables["AllCoreT"];
			var dtPrices = _dsReport.Tables["Prices"];

			var dtRes = new DataTable("Results");
			_dsReport.Tables.Add(dtRes);

			var column = dtRes.Columns.Add("Code");
			column.Caption = "Код";

			column = dtRes.Columns.Add("FullName");
			column.ExtendedProperties.Add("Width", 20);
			column.Caption = "Наименование";

			column = dtRes.Columns.Add("FirmCr");
			column.Caption = "Производитель";
			column.ExtendedProperties.Add("Width", 10);

			column = dtRes.Columns.Add("CustomerCost", typeof(decimal));
			column.Caption = CustomerFirmName;
			column.ExtendedProperties.Add("Width", 6);

			column = dtRes.Columns.Add("CustomerQuantity");
			column.Caption = "Количество";
			column.ExtendedProperties.Add("Width", 4);

			column = dtRes.Columns.Add("MinCost", typeof(decimal));
			column.Caption = "Мин. цена";
			column.ExtendedProperties.Add("Width", 6);
			column.ExtendedProperties.Add("Color", Color.LightSeaGreen);

			column = dtRes.Columns.Add("LeaderName");
			column.Caption = "Лидер";
			column.ExtendedProperties.Add("Width", 9);
			column.ExtendedProperties.Add("Color", Color.LightSkyBlue);

			dtRes.Columns.Add("Differ", typeof(decimal));
			dtRes.Columns["Differ"].Caption = "Разница";

			column = dtRes.Columns.Add("DifferPercents", typeof(decimal));
			column.Caption = "% разницы";
			column.ExtendedProperties.Add("AsDecimal", "");

			column = dtRes.Columns.Add("AvgCost", typeof(decimal));
			column.Caption = "Средняя цена";
			column.ExtendedProperties.Add("Width", 6);

			column = dtRes.Columns.Add("MaxCost", typeof(decimal));
			column.Caption = "Макс. цена";
			column.ExtendedProperties.Add("Width", 6);

			firstColumnCount = dtRes.Columns.Count;

			var priceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				column = dtRes.Columns.Add("Cost" + priceIndex.ToString(), typeof(decimal));
				column.Caption = "Цена";
				column.ExtendedProperties.Add("Width", 6);

				if (ShowQuantity)
				{
					column = dtRes.Columns.Add("Quantity" + priceIndex.ToString());
					column.Caption = "Кол-во";
					column.ExtendedProperties.Add("Width", 4);
				}
				if (_showPercents)
				{
					column = dtRes.Columns.Add("Percents" + priceIndex.ToString(), typeof(decimal));
					column.Caption = "% разницы";
					column.ExtendedProperties.Add("AsDecimal", "");
				}
				priceIndex++;
			}
			priceBlockSize = (dtRes.Columns.Count - firstColumnCount) / priceIndex;
			var newrow = dtRes.NewRow();
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

				//Если есть ID, то мы можем заполнить поле Code и, возможно, остальные поля   предложение SourcePC существует
				DataRow[] drsMin;
				if (!(drCatalog["ID"] is DBNull))
				{
					newrow["Code"] = drCatalog["Code"];
					//Производим поиск предложения по данной позиции по интересующему прайс-листу
					drsMin = dtCore.Select("ID = " + drCatalog["ID"]);
					//Если в Core предложений по данному SourcePC не существует, то прайс-лист асортиментный или не включен клиентом в обзор
					//В этом случае данные поля не заполняется и в сравнении такой прайс-лист не участвует
					if ((drsMin.Length > 0) && !(drsMin[0]["Cost"] is DBNull))
					{
						newrow["CustomerCost"] = Convert.ToDecimal(drsMin[0]["Cost"]);
						newrow["CustomerQuantity"] = drsMin[0]["Quantity"];
						if (newrow["CustomerCost"].Equals(newrow["MinCost"]))
							newrow["LeaderName"] = "+";
					}
				}

				//Если имя лидера неустановлено, то выставляем имя лидера
				if (newrow["LeaderName"] is DBNull)
				{
					//Устанавливаем разность между ценой SourcePC и минимальной ценой
					if (!(newrow["CustomerCost"] is DBNull))
					{
						var minCost = (decimal) newrow["MinCost"];
						var customerCost = (decimal)newrow["CustomerCost"];
						newrow["Differ"] = customerCost - minCost;
						newrow["DifferPercents"] = Math.Round((customerCost - minCost) / customerCost * 100, 0);
					}

					//Выбираем позиции с минимальной ценой, отличные от SourcePC
					drsMin = dtCore.Select(string.Format("CatalogCode = {0}{1} and Cost = {2}", 
						drCatalog["CatalogCode"], 
						GetProducerFilter(drCatalog),
						((decimal) drCatalog["MinCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat)));

					if (drsMin.Length > 0)
					{
						var leaderNames = new List<string>();
						foreach (var drmin in drsMin)
						{
							var drs = dtPrices.Select(
								"PriceCode=" + drmin["PriceCode"] +
								" and RegionCode = " + drmin["RegionCode"]);
							if (drs.Length > 0)
								if (!leaderNames.Contains(drs[0]["FirmName"].ToString()))
									leaderNames.Add(drs[0]["FirmName"].ToString());
						}
						newrow["LeaderName"] = String.Join("; ", leaderNames.ToArray());
					}
				}
				else
				{
					//Ищем первую цену, которая будет больше минимальной цены
					drsMin = dtCore.Select(
						"CatalogCode = " + drCatalog["CatalogCode"] +
						" and PriceCode <> " + SourcePC +
						GetProducerFilter(drCatalog) +
						" and Cost > " + ((decimal)drCatalog["MinCost"]).ToString(CultureInfo.InvariantCulture.NumberFormat),
						"Cost asc");

					if (drsMin.Length > 0)
					{
						var customerCost = Convert.ToDecimal(newrow["CustomerCost"]);
						var cost = Convert.ToDecimal(drsMin[0]["Cost"]);
						newrow["Differ"] = customerCost - cost;
						newrow["DifferPercents"] = Math.Round((customerCost - cost) / customerCost * 100, 0);
					}
				}

				//Выбираем позиции и сортируем по возрастанию цен для того, чтобы по каждому прайс-листы выбрать минимальную цену по одному и тому же CatalogCode
				drsMin = dtCore.Select("CatalogCode = " + drCatalog["CatalogCode"] + GetProducerFilter(drCatalog), "Cost asc");
				foreach (var dtPos in drsMin)
				{
					var dr = dtPrices.Select("PriceCode=" + dtPos["PriceCode"] + " and RegionCode = " + dtPos["RegionCode"]);
					//Проверка на случай получения прайса SourcePC, т.к. этот прайс не будет в dtPrices
					if (dr.Length > 0)
					{
						priceIndex = dtPrices.Rows.IndexOf(dr[0]);

						//Если мы еще не установили значение у поставщика, то делаем это
						//раньше вставляли последнее значение, которое было максимальным
						if (newrow["Cost" + priceIndex] is DBNull)
						{
							newrow["Cost" + priceIndex] = dtPos["Cost"];

							var quantityColumn = dtRes.Columns["Quantity" + priceIndex];
							if (quantityColumn != null)
								newrow[quantityColumn] = dtPos["Quantity"];

							var percentColumn = dtRes.Columns["Percents" + priceIndex];
							if (percentColumn != null)
							{
								var mincost = Convert.ToDouble(newrow["MinCost"]);
								var pricecost = Convert.ToDouble(dtPos["Cost"]);
								newrow[percentColumn] = Math.Round(((pricecost - mincost) * 100) / pricecost, 0);
							}
						}
					}
				}

				dtRes.Rows.Add(newrow);
			}
		}

		private bool ShowQuantity
		{
			get { return (_reportType == 2) || (_reportType == 4); }
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
			var EnabledPrice = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						"select PriceCode from ActivePrices where PriceCode = ?SourcePC and RegionCode = ?SourceRegionCode",
						new MySqlParameter("?SourcePC", SourcePC),
						new MySqlParameter("?SourceRegionCode", SourceRegionCode)));
			if(EnabledPrice == 0 && _byBaseCosts)
			{
				EnabledPrice = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						"select PriceCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
						new MySqlParameter("?SourcePC", SourcePC)));
				if(EnabledPrice != 0)
				{
					SourceRegionCode = Convert.ToUInt64(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						"select RegionCode from ActivePrices where PriceCode = ?SourcePC limit 1;",
						new MySqlParameter("?SourcePC", SourcePC)));
				}
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
  ID bigint unsigned,
  PriceCode int(32) unsigned,
  RegionCode bigint unsigned,
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
  key SynonymCode(SynonymCode)
) engine = MEMORY PACK_KEYS = 0;";

			if (EnabledPrice == 0)
			{
				//Если прайс-лист не включен клиентом или прайс-лист ассортиментный, то добавляем его в таблицу источников TmpSourceCodes, но с ценами NULL
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

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif

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

#if DEBUG
	Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif

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
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", SourcePC);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourceRegionCode", SourceRegionCode);
#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
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
  min(AllPrices.cost) As MinCost, -- здесь должна быть минимальная цена
  avg(AllPrices.cost) As AvgCost, -- здесь должна быть средняя цена
  max(AllPrices.cost) As MaxCost, -- здесь должна быть минимальная цена";

			//Если отчет без учета производителя, то код не учитываем и выводим "-"
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
			//Если отчет с учетом производителя, то пересекаем с таблицой Producers
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

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
			var cnt = _dsReport.Tables["Catalog"].Rows.Count;
#endif

		}

		protected override void FormatExcel(string fileName)
		{
			UseExcel.Workbook(fileName, wb => {
				var ws = (_Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

				ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);
				ws.Activate();

				var result = _dsReport.Tables["Results"];
				//очищаем заголовки
				for(var i = 0; i < result.Columns.Count; i++)
					ws.Cells[1, i + 1] = "";

				var tableBeginRowIndex = 3;
				var rowCount = result.Rows.Count;
				var columnCount = result.Columns.Count;

				if (!String.IsNullOrEmpty(_clientsNames)) // Добавляем строку чтобы вставить выбранные аптеки
					tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, 12, String.Format("Выбранные аптеки: {0}", _clientsNames));
				if (!String.IsNullOrEmpty(_suppliers))
					tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, 12, String.Format("Список поставщиков: {0}", _suppliers));
				if (!String.IsNullOrEmpty(_ignoredSuppliers))
					tableBeginRowIndex = ExcelHelper.PutHeader(ws, tableBeginRowIndex, 12, String.Format("Игнорируемые поставщики: {0}", _ignoredSuppliers));

				var lastRowIndex = rowCount + tableBeginRowIndex;

				ExcelHelper.FormatHeader(ws, tableBeginRowIndex, result);

				//Форматирование заголовков прайс-листов
				FormatLeaderAndPrices(ws);

				//рисуем границы на всю таблицу
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[lastRowIndex, columnCount]].Borders.Weight = XlBorderWeight.xlThin;
				//Устанавливаем шрифт листа
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";

				//Устанавливаем АвтоФильтр на все колонки
				ws.Range[ws.Cells[tableBeginRowIndex, 1], ws.Cells[rowCount, columnCount]].Select();
				((Range)wb.Application.Selection).AutoFilter(1, Missing.Value, XlAutoFilterOperator.xlAnd, Missing.Value, true);

				//Замораживаем некоторые колонки и столбцы
				ws.Range["L4", Missing.Value].Select();
				wb.Application.ActiveWindow.FreezePanes = true;

				//Объединяем несколько ячеек, чтобы в них написать текст
				ws.Range["A1:K2", Missing.Value].Select();
				((Range)wb.Application.Selection).Merge(null);
				if (!WithoutAssortmentPrice)
				{
					if (_reportType < 3)
						wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " без учета производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();
					else
						wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " с учетом производителя по прайсу " + CustomerFirmName + " создан " + DateTime.Now.ToString();
				}
				else
				{
					if (_reportType < 3)
						wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " без учета производителя создан " + DateTime.Now.ToString();
					else
						wb.Application.ActiveCell.FormulaR1C1 = reportCaptionPreffix + " с учетом производителя создан " + DateTime.Now.ToString();
				}
			});
		}

		protected virtual void FormatLeaderAndPrices(_Worksheet ws)
		{
			var columnPrefix = firstColumnCount + 1;
			var priceIndex = 0;
			foreach (DataRow drPrice in _dsReport.Tables["Prices"].Rows)
			{
				var columnIndex = columnPrefix + priceIndex*priceBlockSize;
				//Устанавливаем название фирмы
				ws.Cells[1, columnIndex] = drPrice["FirmName"].ToString();
				//Устанавливаем дату фирмы
				ws.Cells[2, columnIndex] = drPrice["PriceDate"].ToString();
				priceIndex++;
			}
		}
	}
}
