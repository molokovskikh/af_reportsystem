using System;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Data;
using System.Configuration;

namespace Inforoom.ReportSystem
{
	//����������� �����
	public class DefReport : ProviderReport
	{
		protected enum DefReportType
		{
			//����� ������ �� ������������
			ByName = 1,
			//����� �� ������������ � ����� �������
			ByNameAndForm = 2,
			//�� ������������ � ����� ������� � ������ �������������
			ByNameAndFormAndFirmCr = 3,
			//�� ���������
			ByProduct = 4,
			//�� ��������� � ������ �������������
			ByProductAndFirmCr = 5
		}

		protected DefReportType _reportType;
		protected int _priceCode;

		public DefReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
		}

		/// <summary>
		/// �������� ������� ����������� ��� �������� � ������
		/// </summary>
		public DataTable DSResult
		{
			get { return _dsReport.Tables["Results"]; }
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			var tmpReportType = (int)getReportParam("ReportType");
			var v = Enum.GetValues(typeof(DefReportType));
			if (((int)v.GetValue(0) <= tmpReportType) && (tmpReportType <= (int)v.GetValue(v.Length - 1)))
				_reportType = (DefReportType)tmpReportType;
			else
				throw new ArgumentOutOfRangeException("ReportType", tmpReportType, "�������� ��������� �� ������ � ������� ���������� ��������.");

			_priceCode = (int)getReportParam("PriceCode");
			_clientCode = (int)getReportParam("ClientCode");
		}

		private void ProcessWeigth(ExecuteArgs e)
		{
			ProfileHelper.Next("GetOffers");
			GetWeightCostOffers(e);
			ProfileHelper.Next("Processing");
			e.DataAdapter.SelectCommand.Parameters.Clear();

			string SelectCommandText = String.Empty;

			switch (_reportType) {
				case DefReportType.ByName: {
					SelectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices ( 
  NameId int Unsigned, 
  key NameId(NameId))engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices 
select distinct Catalog.NameId 
from 
  reports.averagecosts apt, 
  Core c,
  Catalogs.Assortment,
  Catalogs.Catalog 
where 
	apt.PriceCode <> ?SourcePC 
and apt.PriceCode=c.PriceCode
and Assortment.Id = c.ProductId
and Catalog.Id = Assortment.CatalogId;

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
  inner join Catalogs.Assortment on c.ProductId = Assortment.Id
  inner join Catalogs.Catalog  on Assortment.CatalogId = Catalog.Id
  )
  inner join reports.averagecosts FarmCore on FarmCore.Id = c.Id
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
					SelectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices ( 
  CatalogId int Unsigned, 
  key CatalogId(CatalogId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices 
select distinct Assortment.CatalogId 
from 
  reports.averagecosts apt, 
  Core c, 
  Catalogs.Assortment
where 
	apt.PriceCode <> ?SourcePC 
and apt.PriceCode=c.PriceCode
and Assortment.Id = c.ProductId;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice ( 
  CatalogId int Unsigned,
  Code VARCHAR(20) not NULL, 
  key CatalogId(CatalogId) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct Assortment.CatalogId, '' Code
from 
  (
  Core c 
  inner join Catalogs.Assortment on c.ProductId = Assortment.Id
  )
  inner join reports.averagecosts FarmCore on FarmCore.Id = c.Id
  left join SummaryByPrices st on st.CatalogId = Assortment.CatalogId 
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
					SelectCommandText = @"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices ( 
  CatalogId int Unsigned, 
  CodeFirmCr int Unsigned, 
  key CatalogId(CatalogId),
  key CodeFirmCr(CodeFirmCr)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices 
select distinct Products.CatalogId, Assortment.ProducerId as CodeFirmCr 
from 
  reports.averagecosts apt, 
  Core c,
  Catalogs.Assortment
where 
	apt.PriceCode <> ?SourcePC 
and apt.PriceCode=c.PriceCode
and Assortment.Id = c.ProductId


drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice ( 
  CatalogId int Unsigned, 
  CodeFirmCr int Unsigned,
  Code VARCHAR(20) not NULL, 
  key CatalogId(CatalogId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct Assortment.CatalogId, Assortment.ProducerId as CodeFirmCr, '' as Code
from 
  (
  Core c
  inner join reports.averagecosts FarmCore on c.Id = FarmCore.Id
  inner join Catalogs.Assortment on c.ProductId = Assortment.Id
  )
  left join SummaryByPrices st on st.CatalogId = Assortment.CatalogId and st.CodeFirmCr = Assortment.ProducerId
where 
	c.PriceCode=?SourcePC 
and st.CatalogId is NULL;

select distinct OtherByPrice.Code, CatalogNames.Name, CatalogForms.Form, Producers.Name as FirmCr
from 
 (
  OtherByPrice
  inner join catalogs.catalog on OtherByPrice.CatalogId = catalog.Id
  inner join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
  inner join catalogs.CatalogForms on catalog.FormId = CatalogForms.Id  
 )
  left join Catalogs.Producers on Producers.Id = OtherByPrice.CodeFirmCr
where catalog.Pharmacie = 1
order by CatalogNames.Name, CatalogForms.Form, Producers.Name;";
					break;
				}

				case DefReportType.ByProduct: {
					SelectCommandText = String.Format(@"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices ( 
  ProductId int Unsigned, 
  key ProductId(ProductId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices 
select distinct c.AssortmentId
from 
  Core c
where 
	c.PriceCode <> ?SourcePC 


drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice ( 
  ProductId int Unsigned,
  Code VARCHAR(20) not NULL,  
  key ProductId(ProductId)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct c.AssortmentId, '' as Code
from 
  Core c
  inner join reports.averagecosts FarmCore on FarmCore.Id = c.Id
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
					SelectCommandText = String.Format(@"
drop temporary table IF EXISTS SummaryByPrices;
CREATE temporary table SummaryByPrices ( 
  ProductId int Unsigned, 
  CodeFirmCr int Unsigned, 
  key ProductId(ProductId),
  key CodeFirmCr(CodeFirmCr)) engine=MEMORY PACK_KEYS = 0;
INSERT INTO SummaryByPrices 
select distinct c.AssortmentId, FarmCore.ProducerId as CodeFirmCr 
from 
  reports.averagecosts apt, 
  Core c,
  catalogs.Assortment FarmCore
where 
	apt.PriceCode <> ?SourcePC 
and apt.PriceCode=c.PriceCode
and FarmCore.Id = c.ProductId;

drop temporary table IF EXISTS OtherByPrice;
CREATE temporary table OtherByPrice ( 
  ProductId int Unsigned, 
  CodeFirmCr int Unsigned,
  Code VARCHAR(20) not NULL, 
  key ProductId(ProductId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct c.ProductId, assortment.ProducerId as CodeFirmCr, '' as Code
from 
  (
  Core c
  inner join reports.averagecosts FarmCore on FarmCore.Id = c.Id
  inner join catalogs.assortment on assortment.id = c.ProductId
  )
  left join SummaryByPrices st on st.ProductId = c.ProductId and st.CodeFirmCr = FarmCore.CodeFirmCr
where
	c.PriceCode=?SourcePC 
and st.ProductId is NULL;

select 
  distinct 
  OtherByPrice.Code, 
  CatalogNames.Name,
  {0} as FullForm,
  Producers.Name as FirmCr 
from 
 (
  OtherByPrice
  inner join catalogs.products on OtherByPrice.ProductId = products.Id
  inner join catalogs.catalog on products.CatalogId = catalog.Id
  inner join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
 )
  left join Catalogs.Producers on Producers.Id = OtherByPrice.CodeFirmCr
where catalog.Pharmacie = 1
order by CatalogNames.Name, FullForm, Producers.Name;
", GetFullFormSubquery("OtherByPrice.ProductId"));
					break;
				}
			}
			e.DataAdapter.SelectCommand.CommandText = SelectCommandText;
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", _priceCode);
			e.DataAdapter.Fill(_dsReport, "Results");
			ProfileHelper.End();
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			base.GenerateReport(e);

			ProfileHelper.Next("PreGetOffers");
			if (_priceCode == 0)
				throw new ReportException("� ������ �� ���������� �������� \"�����-����\".");

			var CustomerFirmName = GetSupplierName(_priceCode);

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
					new MySqlParameter("?SourcePC", _priceCode)));
#if !DEBUG
			if (ActualPrice == 0)
				throw new ReportException(String.Format("�����-���� {0} ({1}) �� �������� ����������.", CustomerFirmName, _priceCode));
#endif
			if(_byWeightCosts) {
				ProcessWeigth(e);
				return;
			}
			ProfileHelper.Next("GetOffers");
			//�������� 
			GetOffers(_SupplierNoise);
			ProfileHelper.Next("Processing");
			int EnabledPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					"select PriceCode from ActivePrices where PriceCode = ?PriceCode",
					new MySqlParameter("?PriceCode", _priceCode)));
			if (EnabledPrice == 0 && !_byBaseCosts) {
				string ClientShortName = Convert.ToString(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						@"select Name from Customers.Clients where Id = ?FirmCode",
						new MySqlParameter("?FirmCode", _clientCode)));
				throw new ReportException(String.Format("��� ������� {0} ({1}) �� �������� �����-���� {2} ({3}).", ClientShortName, _clientCode, CustomerFirmName, _priceCode));
			}

			e.DataAdapter.SelectCommand.Parameters.Clear();

			string SelectCommandText = String.Empty;

			switch (_reportType) {
				case DefReportType.ByName: {
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
  Code VARCHAR(20) not NULL, 
  key CatalogId(CatalogId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct Products.CatalogId, FarmCore.CodeFirmCr, FarmCore.Code
from 
  (
  Core c
  inner join farm.Core0 FarmCore on c.Id = FarmCore.Id
  inner join Catalogs.Products on c.ProductId = Products.Id
  )
  left join SummaryByPrices st on st.CatalogId = Products.CatalogId and st.CodeFirmCr = FarmCore.CodeFirmCr
where 
	c.PriceCode=?SourcePC 
and st.CatalogId is NULL;

select distinct OtherByPrice.Code, CatalogNames.Name, CatalogForms.Form, Producers.Name as FirmCr
from 
 (
  OtherByPrice
  inner join catalogs.catalog on OtherByPrice.CatalogId = catalog.Id
  inner join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
  inner join catalogs.CatalogForms on catalog.FormId = CatalogForms.Id  
 )
  left join Catalogs.Producers on Producers.Id = OtherByPrice.CodeFirmCr
where catalog.Pharmacie = 1
order by CatalogNames.Name, CatalogForms.Form, Producers.Name;";
					break;
				}

				case DefReportType.ByProduct: {
					SelectCommandText = String.Format(@"
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
					SelectCommandText = String.Format(@"
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
  Code VARCHAR(20) not NULL, 
  key ProductId(ProductId),
  key CodeFirmCr(CodeFirmCr) ) engine=MEMORY PACK_KEYS = 0;
INSERT INTO OtherByPrice 
select distinct c.ProductId, FarmCore.CodeFirmCr, FarmCore.Code
from 
  (
  Core c
  inner join farm.Core0 FarmCore on FarmCore.Id = c.Id
  )
  left join SummaryByPrices st on st.ProductId = c.ProductId and st.CodeFirmCr = FarmCore.CodeFirmCr
where
	c.PriceCode=?SourcePC 
and st.ProductId is NULL;

select 
  distinct 
  OtherByPrice.Code, 
  CatalogNames.Name,
  {0} as FullForm,
  Producers.Name as FirmCr 
from 
 (
  OtherByPrice
  inner join catalogs.products on OtherByPrice.ProductId = products.Id
  inner join catalogs.catalog on products.CatalogId = catalog.Id
  inner join catalogs.CatalogNames on catalog.NameId = CatalogNames.Id
 )
  left join Catalogs.Producers on Producers.Id = OtherByPrice.CodeFirmCr
where catalog.Pharmacie = 1
order by CatalogNames.Name, FullForm, Producers.Name;
", GetFullFormSubquery("OtherByPrice.ProductId"));
					break;
				}
			}
			e.DataAdapter.SelectCommand.CommandText = SelectCommandText;
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SourcePC", _priceCode);
			e.DataAdapter.Fill(_dsReport, "Results");
			ProfileHelper.End();
		}

		public override void ReportToFile(string FileName)
		{
			DataTableToExcel(_dsReport.Tables["Results"].DefaultView.ToTable(), FileName);
			FormatExcel(FileName);
		}

		protected void FormatExcel(string FileName)
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

						//����������� ��������� ������
						ws.Cells[1, 1] = "���";
						((MSExcel.Range)ws.Columns[1, Type.Missing]).AutoFit();

						ws.Cells[1, 2] = "������������";
						((MSExcel.Range)ws.Columns[2, Type.Missing]).AutoFit();

						switch (_reportType) {
							case DefReportType.ByNameAndForm: {
								ws.Cells[1, 3] = "����� �������";
								((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
								break;
							}
							case DefReportType.ByNameAndFormAndFirmCr: {
								ws.Cells[1, 3] = "����� �������";
								((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
								ws.Cells[1, 4] = "�������������";
								((MSExcel.Range)ws.Columns[4, Type.Missing]).AutoFit();
								break;
							}
							case DefReportType.ByProduct: {
								ws.Cells[1, 3] = "����� �������";
								((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
								break;
							}
							case DefReportType.ByProductAndFirmCr: {
								ws.Cells[1, 3] = "����� �������";
								((MSExcel.Range)ws.Columns[3, Type.Missing]).AutoFit();
								ws.Cells[1, 4] = "�������������";
								((MSExcel.Range)ws.Columns[4, Type.Missing]).AutoFit();
								break;
							}
						}

						//������ ������� �� ��������� �������
						ws.get_Range(ws.Cells[1, 1], ws.Cells[1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThick;

						//������ ������� �� ��� �������
						ws.get_Range(ws.Cells[2, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						//������������� ����� �����
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//������������� ���������� �� ��� �������
						((MSExcel.Range)ws.get_Range(ws.Cells[1, 1], ws.Cells[_dsReport.Tables["Results"].Rows.Count + 1, _dsReport.Tables["Results"].Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						//������������ ��������� ������� � �������
						((MSExcel.Range)ws.get_Range("A2", System.Reflection.Missing.Value)).Select();
						exApp.ActiveWindow.FreezePanes = true;
						MSExcel.Range rng = (MSExcel.Range)ws.Rows[1];
						rng.Insert();
						ws.Cells[1, 1] = "�� ������ ��������� (� ����� ��������� �������� ��� ����������) ������, ����������� � ��� ���������� \"����������������\"";
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