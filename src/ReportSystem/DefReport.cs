using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.IO;

namespace Inforoom.ReportSystem
{
	//Дефектурный отчет
	public class DefReport : ProviderReport
	{
		protected enum DefReportType
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

		protected DefReportType _reportType;
		protected int _priceCode;

		public DefReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
		}

		public override void ReadReportParams()
		{

			int tmpReportType = (int)getReportParam("ReportType");
			Array v = Enum.GetValues(typeof(DefReportType));
			if (((int)v.GetValue(0) <= tmpReportType) && (tmpReportType <= (int)v.GetValue(v.Length - 1)))
				_reportType = (DefReportType)tmpReportType;
			else
				throw new ArgumentOutOfRangeException("ReportType", tmpReportType, "Значение параметра не входит в область допустимых значений.");

			_priceCode = (int)getReportParam("PriceCode");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
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
			int ClientCode = Convert.ToInt32(e.DataAdapter.SelectCommand.ExecuteScalar());
			//Устанавливаем код клиента, как код фирмы, относительно которой генерируется отчет
			_clientCode = ClientCode;

			//Выбираем 
			GetOffers(e);

			e.DataAdapter.SelectCommand.Parameters.Clear();

			string SelectCommandText = String.Empty;

			switch (_reportType)
			{

				case DefReportType.ByName:
					{
						SelectCommandText = @"
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
  key NameId(NameId))engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct Catalog.NameId 
from 
  (
  Core c, 
  Catalogs.Products,
  Catalogs.Catalog 
  )
  left join SummaryByPrices st on st.NameId = Catalog.NameId 
where 
    c.PriceCode=?SourcePC 
and st.NameId is NULL
and Products.Id = c.ProductId
and Catalog.Id = products.CatalogId;

select distinct FarmCore.Code, CatalogNames.Name 
from 
 (
  OtherByPrice,
  catalogs.CatalogNames,
  catalogs.catalog,
  catalogs.products
 )
  left join Core c on c.ProductId = products.Id and c.PriceCode = ?SourcePC 
  left join farm.Core0 FarmCore on FarmCore.Id = c.Id 
where 
    CatalogNames.Id = OtherByPrice.NameId
and catalog.NameId = CatalogNames.Id
and products.CatalogId = catalog.Id
order by CatalogNames.Name;";
						break;
					}

				case DefReportType.ByNameAndForm:
					{
						SelectCommandText = @"
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
  key CatalogId(CatalogId) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct Products.CatalogId
from 
  (
  Core c, 
  Catalogs.Products
  )
  left join SummaryByPrices st on st.CatalogId = Products.CatalogId 
where 
    c.PriceCode=?SourcePC 
and st.CatalogId is NULL
and Products.Id = c.ProductId;

select distinct FarmCore.Code, CatalogNames.Name, CatalogForms.Form 
from 
 (
  OtherByPrice,
  catalogs.catalog,
  catalogs.CatalogNames,
  catalogs.CatalogForms,
  catalogs.products
 )
  left join Core c on c.ProductId = products.Id and c.PriceCode = ?SourcePC 
  left join farm.Core0 FarmCore on FarmCore.Id = c.Id 
where 
    catalog.Id = OtherByPrice.CatalogId
and CatalogNames.Id = catalog.NameId
and CatalogForms.Id = catalog.FormId
and products.CatalogId = catalog.Id
order by CatalogNames.Name, CatalogForms.Form;";
						break;
					}

				case DefReportType.ByNameAndFormAndFirmCr:
					{
						SelectCommandText = @"
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
and apt.PriceCode=c.PriceCode
and Products.Id = c.ProductId
and FarmCore.Id = c.Id;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice ( 
  CatalogId int Unsigned, 
  CodeFirmCr int Unsigned, 
  key CatalogId(CatalogId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct Products.CatalogId, FarmCore.CodeFirmCr
from 
  (
  Core c, 
  farm.Core0 FarmCore, 
  Catalogs.Products
  )
  left join SummaryByPrices st on st.CatalogId = Products.CatalogId and st.CodeFirmCr = FarmCore.CodeFirmCr
where 
    c.PriceCode=?SourcePC 
and st.CatalogId is NULL
and Products.Id = c.ProductId
and FarmCore.Id = c.Id;

select distinct FarmCore.Code, CatalogNames.Name, CatalogForms.Form, CatalogFirmCr.FirmCr 
from 
 (
  OtherByPrice,
  catalogs.catalog,
  catalogs.CatalogNames,
  catalogs.CatalogForms,
  farm.CatalogFirmCr,
  catalogs.products
 )
  left join Core c on c.ProductId = products.Id and c.PriceCode = ?SourcePC 
  left join farm.Core0 FarmCore on FarmCore.Id = c.Id and FarmCore.CodeFirmCr = OtherByPrice.CodeFirmCr
where 
    catalog.Id = OtherByPrice.CatalogId
and CatalogNames.Id = catalog.NameId
and CatalogForms.Id = catalog.FormId
and products.CatalogId = catalog.Id
and CatalogFirmCr.CodeFirmCr = OtherByPrice.CodeFirmCr
order by CatalogNames.Name, CatalogForms.Form, CatalogFirmCr.FirmCr;";
						break;
					}

				case DefReportType.ByProduct:
					{
						SelectCommandText = @"
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
  key ProductId(ProductId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct c.ProductId
from 
  Core c
  left join SummaryByPrices st on st.ProductId = c.ProductId
where 
    c.PriceCode=?SourcePC 
and st.ProductId is NULL;

select 
  distinct 
  FarmCore.Code, 
  CatalogNames.Name,
  catalogs.GetFullForm(OtherByPrice.ProductId) as FullForm
from 
 (
  OtherByPrice,
  catalogs.catalog,
  catalogs.CatalogNames,
  catalogs.products
 )
  left join Core c on c.ProductId = products.Id and c.PriceCode = ?SourcePC 
  left join farm.Core0 FarmCore on FarmCore.Id = c.Id
where 
    products.Id = OtherByPrice.ProductId
and catalog.Id = products.CatalogId
and CatalogNames.Id = catalog.NameId
order by CatalogNames.Name, FullForm;
";
						break;
					}

				case DefReportType.ByProductAndFirmCr:
					{
						SelectCommandText = @"
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
and FarmCore.Id = c.Id;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice ( 
  ProductId int Unsigned, 
  CodeFirmCr int Unsigned, 
  key ProductId(ProductId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct c.ProductId, FarmCore.CodeFirmCr
from 
  (
  Core c, 
  farm.Core0 FarmCore
  )
  left join SummaryByPrices st on st.ProductId = c.ProductId and st.CodeFirmCr = FarmCore.CodeFirmCr
where 
    c.PriceCode=?SourcePC 
and st.ProductId is NULL
and FarmCore.Id = c.Id;

select 
  distinct 
  FarmCore.Code, 
  CatalogNames.Name,
  catalogs.GetFullForm(OtherByPrice.ProductId) as FullForm,
  CatalogFirmCr.FirmCr 
from 
 (
  OtherByPrice,
  catalogs.catalog,
  catalogs.CatalogNames,
  farm.CatalogFirmCr,
  catalogs.products
 )
  left join Core c on c.ProductId = products.Id and c.PriceCode = ?SourcePC 
  left join farm.Core0 FarmCore on FarmCore.Id = c.Id and FarmCore.CodeFirmCr = OtherByPrice.CodeFirmCr
where 
    products.Id = OtherByPrice.ProductId
and catalog.Id = products.CatalogId
and CatalogNames.Id = catalog.NameId
and CatalogFirmCr.CodeFirmCr = OtherByPrice.CodeFirmCr
order by CatalogNames.Name, FullForm, CatalogFirmCr.FirmCr;
";
						break;
					}

			}
			e.DataAdapter.SelectCommand.CommandText = SelectCommandText;
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("SourcePC", _priceCode);
			e.DataAdapter.Fill(_dsReport, "Results");
		}

		public override void ReportToFile(string FileName)
		{
			DataTableToExcel(_dsReport.Tables["Results"].DefaultView.ToTable(), FileName);
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

						//Форматируем заголовок отчета
						ws.Cells[1, 1] = "Код";
						((MSExcel.Range)ws.Columns[1, Type.Missing]).AutoFit();

						ws.Cells[1, 2] = "Наименование";
						((MSExcel.Range)ws.Columns[2, Type.Missing]).AutoFit();

						switch (_reportType)
						{
							case DefReportType.ByNameAndForm:
								{
									ws.Cells[1, 3] = "Форма выпуска";
									((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
									break;
								}
							case DefReportType.ByNameAndFormAndFirmCr:
								{
									ws.Cells[1, 3] = "Форма выпуска";
									((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
									ws.Cells[1, 4] = "Производитель";
									((MSExcel.Range)ws.Columns[4, Type.Missing]).AutoFit();
									break;
								}
							case DefReportType.ByProduct:
								{
									ws.Cells[1, 3] = "Форма выпуска";
									((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
									break;
								}
							case DefReportType.ByProductAndFirmCr:
								{
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
						((MSExcel.Range)ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count+1, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//Замораживаем некоторые колонки и столбцы
						((MSExcel.Range)ws.get_Range("A2", System.Reflection.Missing.Value)).Select();
						exApp.ActiveWindow.FreezePanes = true;
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

	}
}
