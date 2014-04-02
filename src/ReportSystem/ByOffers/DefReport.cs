using System;
using System.Diagnostics;
using Common.Tools.Helpers;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Data;
using System.Configuration;
using Common.MySql;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.ReportSystem
{
	public enum DefReportType
	{
		//Отчет только по наименованию
		ByName = 1,
		//Отчет по наименованию и форме выпуска
		ByNameAndForm = 2,
		//по наименованию и форме выпуска с учетом производителя
		ByNameAndFormAndFirmCr = 3,
		//по продуктам
		ByProduct = 4,
		//по продуктам с учетом производителя
		ByProductAndFirmCr = 5
	}

	//Дефектурный отчет
	public class DefReport : ProviderReport
	{
		protected DefReportType _reportType;
		protected int _priceCode;

		public DefReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			var tmpReportType = (int)getReportParam("ReportType");
			var v = Enum.GetValues(typeof(DefReportType));
			if (((int)v.GetValue(0) <= tmpReportType) && (tmpReportType <= (int)v.GetValue(v.Length - 1)))
				_reportType = (DefReportType)tmpReportType;
			else
				throw new ArgumentOutOfRangeException("ReportType", tmpReportType, "Значение параметра не входит в область допустимых значений.");

			_priceCode = (int)getReportParam("PriceCode");
			_clientCode = (int)getReportParam("ClientCode");
		}

		private void ProcessWeigth(ExecuteArgs e)
		{
			GetWeightCostOffers(e);
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
			e.DataAdapter.SelectCommand.Parameters.Clear();

			var selectCommandText = String.Empty;

			switch (_reportType) {
				case DefReportType.ByName: {
					selectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  NameId int Unsigned,
  key NameId(NameId))engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct Catalog.NameId
from
  Core c,
  Catalogs.Products pr,
  Catalogs.Assortment,
  Catalogs.Catalog
where
	c.PriceCode <> ?SourcePC
and pr.Id=c.ProductId
and Catalog.Id = pr.CatalogId
and Assortment.catalogId = Catalog.Id;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  NameId int Unsigned,
  Code VARCHAR(20) not NULL,
  key NameId(NameId))engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct Catalog.NameId, '' Code
from
  (
  Core c
  inner join Catalogs.Products pr on pr.Id=c.ProductId
inner join Catalogs.Catalog  on pr.CatalogId = Catalog.Id
  )
  left join SummaryByPrices st on st.NameId = Catalog.NameId
where
  c.PriceCode=?SourcePC
  and st.NameId is NULL
  and Catalog.Pharmacie = 1;

select distinct OtherByPrice.Code, CatalogNames.Name
from
  OtherByPrice
  inner join catalogs.CatalogNames on OtherByPrice.NameId = CatalogNames.Id
order by CatalogNames.Name;";
					break;
				}

				case DefReportType.ByNameAndForm: {
					selectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  CatalogId int Unsigned,
  key CatalogId(CatalogId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct pr.CatalogId
from
  Core c,
  Catalogs.Products pr
where
	c.PriceCode <> ?SourcePC
and pr.Id = c.ProductId;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  CatalogId int Unsigned,
  Code VARCHAR(20) not NULL,
  key CatalogId(CatalogId) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct pr.CatalogId, '' Code
from
  (
  Core c
  inner join Catalogs.Products pr on c.ProductId = pr.Id
  )
  left join SummaryByPrices st on st.CatalogId = pr.CatalogId
where
  c.PriceCode=?SourcePC
  and st.CatalogId is NULL;

select distinct OtherByPrice.Code, CatalogNames.Name, CatalogForms.Form
from
  OtherByPrice
  inner join catalogs.catalog on OtherByPrice.CatalogId = catalog.Id
  inner join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
  inner join catalogs.CatalogForms on catalog.FormId = CatalogForms.Id
where catalog.Pharmacie = 1
order by CatalogNames.Name, CatalogForms.Form;";
					break;
				}

				case DefReportType.ByNameAndFormAndFirmCr: {
					selectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  CatalogId int Unsigned,
  CodeFirmCr int Unsigned,
  key CatalogId(CatalogId),
  key CodeFirmCr(CodeFirmCr)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct Products.CatalogId, c.ProducerId as CodeFirmCr
from
  Core c,
  Catalogs.Products
where
	c.PriceCode <> ?SourcePC
	and Products.Id = c.ProductId
	and c.ProducerId is not null;


drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  CatalogId int Unsigned,
  CodeFirmCr int Unsigned,
  Code VARCHAR(20) not NULL,
  key CatalogId(CatalogId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct Products.CatalogId, c.ProducerId as CodeFirmCr, '' as Code
from
	Core c
	inner join Catalogs.Products on c.ProductId = Products.Id
	left join SummaryByPrices st on st.CatalogId = Products.CatalogId and st.CodeFirmCr = c.ProducerId
where
	c.PriceCode=?SourcePC
	and c.ProducerId is not null
	and st.CatalogId is NULL;

select distinct OtherByPrice.Code, CatalogNames.Name, CatalogForms.Form, Producers.Name as FirmCr
from OtherByPrice
	join catalogs.catalog on OtherByPrice.CatalogId = catalog.Id
	join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
	join catalogs.CatalogForms on catalog.FormId = CatalogForms.Id
	join Catalogs.Producers on Producers.Id = OtherByPrice.CodeFirmCr
where catalog.Pharmacie = 1
order by CatalogNames.Name, CatalogForms.Form, Producers.Name;";
					break;
				}

				case DefReportType.ByProduct: {
					selectCommandText = String.Format(@"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  ProductId int Unsigned,
  key ProductId(ProductId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct c.ProductId
from
  Core c
where
	c.PriceCode <> ?SourcePC;


drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  ProductId int Unsigned,
  Code VARCHAR(20) not NULL,
  key ProductId(ProductId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct c.ProductId, '' as Code
from
  Core c
  left join SummaryByPrices st on st.ProductId = c.ProductId
where
	c.PriceCode=?SourcePC
and st.ProductId is NULL;

select
  distinct
  OtherByPrice.Code,
  CatalogNames.Name,
  CatalogForms.Form as FullForm
from
 (
  OtherByPrice
  inner join catalogs.products on OtherByPrice.ProductId = products.Id
  inner join catalogs.catalog on products.CatalogId = catalog.Id
  inner join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
  inner join catalogs.CatalogForms on CatalogForms.Id = catalog.FormId
 )
where catalog.Pharmacie = 1
order by CatalogNames.Name, FullForm;
");
					break;
				}

				case DefReportType.ByProductAndFirmCr: {
					selectCommandText = String.Format(@"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  ProductId int Unsigned,
  CodeFirmCr int Unsigned,
  key ProductId(ProductId),
  key CodeFirmCr(CodeFirmCr)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct c.ProductId, c.ProducerId as CodeFirmCr
from Core c
where c.PriceCode <> ?SourcePC
	c.ProducerId is not null;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  ProductId int Unsigned,
  CodeFirmCr int Unsigned,
  Code VARCHAR(20) not NULL,
  key ProductId(ProductId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct c.ProductId, c.ProducerId as CodeFirmCr, '' as Code
from
	Core c
	join catalogs.products on products.id = c.ProductId
	left join SummaryByPrices st on st.ProductId = c.ProductId and st.CodeFirmCr = c.ProducerId
where
	c.PriceCode = ?SourcePC
	and c.ProducerId is not null
	and st.ProductId is NULL;

select
  distinct
  OtherByPrice.Code,
  CatalogNames.Name,
  CatalogForms.Form as FullForm,
  Producers.Name as FirmCr
from
	OtherByPrice
	join catalogs.products on OtherByPrice.ProductId = products.Id
	join catalogs.catalog on products.CatalogId = catalog.Id
	join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
	join catalogs.CatalogForms on CatalogForms.Id = catalog.FormId
	join Catalogs.Producers on Producers.Id = OtherByPrice.CodeFirmCr
where catalog.Pharmacie = 1
order by CatalogNames.Name, FullForm, Producers.Name;
");
					break;
				}
			}

			var sourcePc = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(e.DataAdapter.SelectCommand.Connection,
						@"
select
  pricesdata.FirmCode
from
  usersettings.pricesdata
where
	pricesdata.PriceCode = ?PriceCode;",
					new MySqlParameter("?PriceCode", _priceCode)));
			e.DataAdapter.SelectCommand.CommandText = selectCommandText;
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", sourcePc);
			e.DataAdapter.Fill(_dsReport, "Results");
			ProfileHelper.End();
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			base.GenerateReport(e);

			ProfileHelper.Next("PreGetOffers");
			if (_priceCode == 0)
				throw new ReportException("В отчете не установлен параметр \"Прайс-лист\".");

			var customerFirmName = GetSupplierName(_priceCode);

			//Проверка актуальности прайс-листа
			var actualPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					@"
select distinct
  pc.PriceCode
from
  usersettings.pricescosts pc,
  usersettings.priceitems pim,
  farm.formrules fr
where
	pc.PriceCode = ?SourcePC
and exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pc.PriceCode and prd.BaseCost=pc.CostCode limit 1)
and pim.Id = pc.PriceItemId
and fr.Id = pim.FormRuleId
and (to_days(now())-to_days(pim.PriceDate)) < fr.MaxOld",
					new MySqlParameter("?SourcePC", _priceCode)));
#if !DEBUG
			if (ActualPrice == 0)
				throw new ReportException(String.Format("Прайс-лист {0} ({1}) не является актуальным.", CustomerFirmName, _priceCode));
#endif
			if(_byWeightCosts) {
				ProcessWeigth(e);
				return;
			}

			GetOffers(_SupplierNoise);
			var enabledPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					"select PriceCode from ActivePrices where PriceCode = ?PriceCode",
					new MySqlParameter("?PriceCode", _priceCode)));
			if (enabledPrice == 0 && !_byBaseCosts) {
				var clientShortName = Convert.ToString(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						@"select Name from Customers.Clients where Id = ?FirmCode",
						new MySqlParameter("?FirmCode", _clientCode)));
				throw new ReportException(String.Format("Для клиента {0} ({1}) не доступен прайс-лист {2} ({3}).", clientShortName, _clientCode, customerFirmName, _priceCode));
			}

			e.DataAdapter.SelectCommand.Parameters.Clear();

			var selectCommandText = String.Empty;

			switch (_reportType) {
				case DefReportType.ByName: {
					selectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  NameId int Unsigned,
  key NameId(NameId))engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct Catalog.NameId
from
  ActivePrices apt,
  Core c,
  Catalogs.Products,
  Catalogs.Catalog
where
	apt.PriceCode <> ?SourcePC
and apt.PriceCode=c.PriceCode
and Products.Id = c.ProductId
and Catalog.Id = products.CatalogId;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  NameId int Unsigned,
  Code VARCHAR(20) not NULL,
  key NameId(NameId))engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct Catalog.NameId, FarmCore.Code
from
  (
  Core c
  inner join Catalogs.Products on c.ProductId = Products.Id
  inner join Catalogs.Catalog  on Products.CatalogId = Catalog.Id
  )
  inner join farm.Core0 FarmCore on FarmCore.Id = c.Id
  left join SummaryByPrices st on st.NameId = Catalog.NameId
where
  c.PriceCode=?SourcePC
  and st.NameId is NULL
  and Catalog.Pharmacie = 1;

select distinct OtherByPrice.Code, CatalogNames.Name
from
  OtherByPrice
  inner join catalogs.CatalogNames on OtherByPrice.NameId = CatalogNames.Id
order by CatalogNames.Name;";
					break;
				}

				case DefReportType.ByNameAndForm: {
					selectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  CatalogId int Unsigned,
  key CatalogId(CatalogId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct Products.CatalogId
from
  ActivePrices apt,
  Core c,
  Catalogs.Products
where
	apt.PriceCode <> ?SourcePC
and apt.PriceCode=c.PriceCode
and Products.Id = c.ProductId;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  CatalogId int Unsigned,
  Code VARCHAR(20) not NULL,
  key CatalogId(CatalogId) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct Products.CatalogId, FarmCore.Code
from
  (
  Core c
  inner join Catalogs.Products on c.ProductId = Products.Id
  )
  inner join farm.Core0 FarmCore on FarmCore.Id = c.Id
  left join SummaryByPrices st on st.CatalogId = Products.CatalogId
where
  c.PriceCode=?SourcePC
  and st.CatalogId is NULL;

select distinct OtherByPrice.Code, CatalogNames.Name, CatalogForms.Form
from
  OtherByPrice
  inner join catalogs.catalog on OtherByPrice.CatalogId = catalog.Id
  inner join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
  inner join catalogs.CatalogForms on catalog.FormId = CatalogForms.Id
where catalog.Pharmacie = 1
order by CatalogNames.Name, CatalogForms.Form;";
					break;
				}

				case DefReportType.ByNameAndFormAndFirmCr: {
					selectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  CatalogId int Unsigned,
  CodeFirmCr int Unsigned,
  key CatalogId(CatalogId),
  key CodeFirmCr(CodeFirmCr)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct Products.CatalogId, FarmCore.CodeFirmCr
from
  ActivePrices apt,
  Core c,
  farm.Core0 FarmCore,
  Catalogs.Products
where
	apt.PriceCode <> ?SourcePC
	and apt.PriceCode = c.PriceCode
	and Products.Id = c.ProductId
	and FarmCore.Id = c.Id
	and FarmCore.CodeFirmCr is not null;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  CatalogId int Unsigned,
  CodeFirmCr int Unsigned,
  Code VARCHAR(20) not NULL,
  key CatalogId(CatalogId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct Products.CatalogId, FarmCore.CodeFirmCr, FarmCore.Code
from
	Core c
	inner join farm.Core0 FarmCore on c.Id = FarmCore.Id
	inner join Catalogs.Products on c.ProductId = Products.Id
	left join SummaryByPrices st on st.CatalogId = Products.CatalogId and st.CodeFirmCr = FarmCore.CodeFirmCr
where
	c.PriceCode=?SourcePC
	and FarmCore.CodeFirmCr is not null
	and st.CatalogId is NULL;

select distinct OtherByPrice.Code, CatalogNames.Name, CatalogForms.Form, Producers.Name as FirmCr
from
	OtherByPrice
	join catalogs.catalog on OtherByPrice.CatalogId = catalog.Id
	join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
	join catalogs.CatalogForms on catalog.FormId = CatalogForms.Id
	join Catalogs.Producers on Producers.Id = OtherByPrice.CodeFirmCr
where catalog.Pharmacie = 1
order by CatalogNames.Name, CatalogForms.Form, Producers.Name;";
					break;
				}

				case DefReportType.ByProduct: {
					selectCommandText = String.Format(@"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  ProductId int Unsigned,
  key ProductId(ProductId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct c.ProductId
from
  ActivePrices apt,
  Core c
where
	apt.PriceCode <> ?SourcePC
and apt.PriceCode=c.PriceCode;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  ProductId int Unsigned,
  Code VARCHAR(20) not NULL,
  key ProductId(ProductId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct c.ProductId, FarmCore.Code
from
  Core c
  inner join farm.Core0 FarmCore on FarmCore.Id = c.Id
  left join SummaryByPrices st on st.ProductId = c.ProductId
where
	c.PriceCode=?SourcePC
and st.ProductId is NULL;

select
  distinct
  OtherByPrice.Code,
  CatalogNames.Name,
  {0} as FullForm
from
 (
  OtherByPrice
  inner join catalogs.products on OtherByPrice.ProductId = products.Id
  inner join catalogs.catalog on products.CatalogId = catalog.Id
  inner join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
 )
where catalog.Pharmacie = 1
order by CatalogNames.Name, FullForm;
", GetFullFormSubquery("OtherByPrice.ProductId"));
					break;
				}

				case DefReportType.ByProductAndFirmCr: {
					selectCommandText = String.Format(@"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices (
  ProductId int Unsigned,
  CodeFirmCr int Unsigned,
  key ProductId(ProductId),
  key CodeFirmCr(CodeFirmCr)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices
select distinct c.ProductId, FarmCore.CodeFirmCr
from
  ActivePrices apt,
  Core c,
  farm.Core0 FarmCore
where
	apt.PriceCode <> ?SourcePC
	and apt.PriceCode=c.PriceCode
	and FarmCore.Id = c.Id
	and FarmCore.CodeFirmCr is not null;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice (
  ProductId int Unsigned,
  CodeFirmCr int Unsigned,
  Code VARCHAR(20) not NULL,
  key ProductId(ProductId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice
select distinct c.ProductId, FarmCore.CodeFirmCr, FarmCore.Code
from
  Core c
  inner join farm.Core0 FarmCore on FarmCore.Id = c.Id
  left join SummaryByPrices st on st.ProductId = c.ProductId and st.CodeFirmCr = FarmCore.CodeFirmCr
where
	c.PriceCode=?SourcePC
	FarmCore.CodeFirmCr is not null
	and st.ProductId is NULL;

select
  distinct
  OtherByPrice.Code,
  CatalogNames.Name,
  {0} as FullForm,
  Producers.Name as FirmCr
from
	OtherByPrice
	join catalogs.products on OtherByPrice.ProductId = products.Id
	join catalogs.catalog on products.CatalogId = catalog.Id
	join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
	join Catalogs.Producers on Producers.Id = OtherByPrice.CodeFirmCr
where catalog.Pharmacie = 1
order by CatalogNames.Name, FullForm, Producers.Name;
", GetFullFormSubquery("OtherByPrice.ProductId"));
					break;
				}
			}

			e.DataAdapter.SelectCommand.CommandText = selectCommandText;
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", _priceCode);
			e.DataAdapter.Fill(_dsReport, "Results");
		}

		public override DataTable GetReportTable()
		{
			return _dsReport.Tables["Results"].DefaultView.ToTable();
		}

		protected override void FormatExcel(string FileName)
		{
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try {
				exApp.DisplayAlerts = false;
				MSExcel.Workbook wb = exApp.Workbooks.Open(FileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try {
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + ReportCode.ToString()];

					try {
						ws.Name = ReportCaption.Substring(0, (ReportCaption.Length < MaxListName) ? ReportCaption.Length : MaxListName);

						//Форматируем заголовок отчета
						ws.Cells[1, 1] = "Код";
						((MSExcel.Range)ws.Columns[1, Type.Missing]).AutoFit();

						ws.Cells[1, 2] = "Наименование";
						((MSExcel.Range)ws.Columns[2, Type.Missing]).AutoFit();

						switch (_reportType) {
							case DefReportType.ByNameAndForm: {
								ws.Cells[1, 3] = "Форма выпуска";
								((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
								break;
							}
							case DefReportType.ByNameAndFormAndFirmCr: {
								ws.Cells[1, 3] = "Форма выпуска";
								((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
								ws.Cells[1, 4] = "Производитель";
								((MSExcel.Range)ws.Columns[4, Type.Missing]).AutoFit();
								break;
							}
							case DefReportType.ByProduct: {
								ws.Cells[1, 3] = "Форма выпуска";
								((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
								break;
							}
							case DefReportType.ByProductAndFirmCr: {
								ws.Cells[1, 3] = "Форма выпуска";
								((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
								ws.Cells[1, 4] = "Производитель";
								((MSExcel.Range)ws.Columns[4, Type.Missing]).AutoFit();
								break;
							}
						}

						//рисуем границы на заголовок таблицы
						ws.get_Range(ws.Cells[1, 1], ws.Cells[1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThick;

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[2, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//Замораживаем некоторые колонки и столбцы
						((MSExcel.Range)ws.get_Range("A2", System.Reflection.Missing.Value)).Select();
						exApp.ActiveWindow.FreezePanes = true;
						MSExcel.Range rng = (MSExcel.Range)ws.Rows[1];
						rng.Insert();
						var caption = "Из отчета исключены (в целях повышения удобства его восприятия) товары, относящиеся к так называемой \"парафармацевтике\"";
						if(_byWeightCosts)
							caption += ". Отчет построен по взвешенным ценам";
						else if(_byBaseCosts)
							caption += ". Отчет построен по базовым ценам";
						ws.Cells[1, 1] = caption;
					}
					finally {
						wb.SaveAs(FileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
					}
				}
				finally {
					ws = null;
					wb = null;
					try {
						exApp.Workbooks.Close();
					}
					catch {
					}
				}
			}
			finally {
				try {
					exApp.Quit();
				}
				catch {
				}
				exApp = null;
			}
		}
	}
}