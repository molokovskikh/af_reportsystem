using System;
using System.Diagnostics;
using System.Data;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using Inforoom.ReportSystem.Filters;
using ExecuteTemplate;
using System.Collections.Generic;
using System.Drawing;

namespace Inforoom.ReportSystem
{
	public class MixedReport : OrdersReport
	{
		protected const string sourceFirmCodeProperty = "SourceFirmCode";
		protected const string businessRivalsProperty = "BusinessRivals";
		protected const string showCodeProperty = "ShowCode";
		protected const string showCodeCrProperty = "ShowCodeCr";

		//Поставщик, по которому будет производиться отчет
		protected int sourceFirmCode;
		//Список конкурентов данного поставщика
		protected List<ulong> businessRivals;
		//Список постащиков-конкурентов в виде строки
		protected string businessRivalsList;

		//Отображать поле Code из прайс-листа поставщика?
		protected bool showCode;
		//Отображать поле CodeCr из прайс-листа поставщика?
		protected bool showCodeCr;

		//Одно из полей "Наименование продукта", "Полное наименование", "Наименование"
		protected FilterField nameField;
		//Поле производитель
		protected FilterField firmCrField;

		public MixedReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			SupportProductNameOptimization = true;
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			showCode = reportParamExists(showCodeProperty) ? (bool)getReportParam(showCodeProperty) : false; // показывать код поставщика
			showCodeCr = reportParamExists(showCodeCrProperty) ? (bool) getReportParam(showCodeCrProperty) : false; // показывать код изготовителя

			sourceFirmCode = (int)getReportParam(sourceFirmCodeProperty); // поставщик
			businessRivals = (List<ulong>)getReportParam(businessRivalsProperty); // список конкурентов

			if (sourceFirmCode == 0)
				throw new ReportException("Не установлен параметр \"Поставщик\".");

			if (businessRivals.Count == 0)
				throw new ReportException("Не установлен параметр \"Список конкурентов\".");

			List<string> s = businessRivals.ConvertAll(value => value.ToString());
			businessRivalsList = String.Join(", ", s.ToArray());

			//Пытаемся найти список ограничений по постащику
			var firmCodeField = selectedField.Find(value => value.reportPropertyPreffix == "FirmCode");
			if ((firmCodeField != null) && (firmCodeField.equalValues != null))
			{
				//Если в списке выбранных значений нет интересующего поставщика, то добавляем его туда
				if (!firmCodeField.equalValues.Contains(Convert.ToUInt64(sourceFirmCode)))
					firmCodeField.equalValues.Add(Convert.ToUInt64(sourceFirmCode));

				//Для каждого поставщика из списка конкурентов проверяем: есть ли он в списке выбранных значений, если нет, то добавляем его
				businessRivals.ForEach(delegate(ulong value) { if (!firmCodeField.equalValues.Contains(value)) firmCodeField.equalValues.Add(value); });
			}

		}

		protected override void CheckAfterLoadFields()
		{
			ProfileHelper.Next("BaseCheckAfterLoad");
			base.CheckAfterLoadFields();
			ProfileHelper.Next("CheckAfterLoad");
			//Выбирем поле "Производитель", если в настройке отчета есть соответствующий параметр
			firmCrField = selectedField.Find(value => value.reportPropertyPreffix == "FirmCr");

			//Проверяем, что выбран один из параметров для отображения: Наименование, Полное Наименование, Продукт
			var nameFields = selectedField.FindAll(
				value => (value.reportPropertyPreffix == "ProductName") || value.reportPropertyPreffix == "FullName" || value.reportPropertyPreffix == "ShortName");
			if (nameFields.Count == 0)
				throw new ReportException("Из полей \"Наименование продукта\", \"Полное наименование\", \"Наименование\" не выбрано ни одно поле.");
			if (nameFields.Count > 1)
				throw new ReportException("Из полей \"Наименование продукта\", \"Полное наименование\", \"Наименование\" должно быть выбрано только одно поле.");
			nameField = nameFields[0];
		}

		void FillProviderCodes(ExecuteArgs e)
		{
			ProfileHelper.Next("FillCodes");
			e.DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS ProviderCodes;
create temporary table ProviderCodes (" +
									((showCode) ? "Code varchar(20), " : String.Empty) +
									((showCodeCr) ? "CodeCr varchar(20), " : String.Empty) +
									"CatalogCode int unsigned, codefirmcr int unsigned," +
									((showCode) ? "key Code(Code), " : String.Empty) +
									((showCodeCr) ? "key CodeCr(CodeCr), " : String.Empty) +
@"key CatalogCode(CatalogCode), key CodeFirmCr(CodeFirmCr)) engine=MEMORY;
insert into ProviderCodes "
				+
				"select " +
					((showCode) ? "CoreCodes.Code, " : String.Empty) +
					((showCodeCr) ? "CoreCodes.CodeCr, " : String.Empty) +
					nameField.primaryField + ((firmCrField != null) ? ", " + firmCrField.primaryField : ", null ") +
				@" from ((
(
select
distinct " +
					((showCode) ? "ol.Code, " : String.Empty) +
					((showCodeCr) ? "ol.CodeCr, " : String.Empty) +
@"ol.ProductId, 
  ol.CodeFirmCr
from " +
#if DEBUG
  @"orders.OrdersHead oh,
  orders.OrdersList ol," +
#else
  @"ordersold.OrdersHead oh,
  ordersold.OrdersList ol," +
#endif
 @" usersettings.pricesdata pd
where
	ol.OrderID = oh.RowID
and ol.Junk = 0
#and ol.Await = 0
and pd.PriceCode = oh.PriceCode
and pd.FirmCode = " + sourceFirmCode.ToString() +
				" and oh.WriteTime > '" + dtFrom.ToString(MySQLDateFormat) + "' " +
				" and oh.WriteTime < '" + dtTo.ToString(MySQLDateFormat) + "' " +
@")
union
(
select
distinct " +
					((showCode) ? "core.Code, " : String.Empty) +
					((showCodeCr) ? "core.CodeCr, " : String.Empty) +
@"core.ProductId,
core.CodeFirmCr
from
  usersettings.Pricesdata pd,
  farm.Core0 core
where
	pd.FirmCode = " + sourceFirmCode.ToString()
+ @" and core.PriceCode = pd.PriceCode
)) CoreCodes,
  catalogs.products p,
  catalogs.catalog c,
  catalogs.catalognames cn)
  left join catalogs.Producers cfc on CoreCodes.CodeFirmCr = cfc.Id
where
	p.Id = CoreCodes.ProductId
and c.Id = p.CatalogId
and cn.id = c.NameId
group by " + nameField.primaryField + ((firmCrField != null) ? ", " + firmCrField.primaryField : String.Empty);

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("GenerateReport");
			filterDescriptions.Add(String.Format("Выбранный поставщик : {0}", GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from future.suppliers supps, farm.regions rg where rg.RegionCode = supps.HomeRegion and supps.Id = " + sourceFirmCode)));
			filterDescriptions.Add(String.Format("Список поставщиков-конкурентов : {0}", GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from future.suppliers supps, farm.regions rg  where rg.RegionCode = supps.HomeRegion and supps.Id in (" + businessRivalsList + ") order by supps.Name")));

			if (showCode || showCodeCr)
				FillProviderCodes(e);

			ProfileHelper.Next("GenerateReport2");

			var selectCommand = BuildSelect();

			if (firmCrPosition)
				selectCommand = selectCommand.Replace("cfc.Id", "if(c.Pharmacie = 1, cfc.Id, 0) as cfc_id")
											 .Replace("cfc.Name", "if(c.Pharmacie = 1, cfc.Name, 'Нелекарственный ассортимент')");

			if (showCode)
				selectCommand += " ProviderCodes.Code, ";
			if (showCodeCr)
				selectCommand += " ProviderCodes.CodeCr, ";

			selectCommand = String.Concat(selectCommand, String.Format(@"
sum(if(pd.firmcode = {0}, ol.cost*ol.quantity, NULL)) as SourceFirmCodeSum,
sum(if(pd.firmcode = {0}, ol.quantity, NULL)) SourceFirmCodeRows,
Min(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeMinCost,
Avg(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeAvgCost,
Max(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeMaxCost,
Count(distinct if(pd.firmcode = {0}, oh.RowId, NULL)) as SourceFirmDistinctOrderId,
Count(distinct if(pd.firmcode = {0}, oh.AddressId, NULL)) as SourceFirmDistinctAddressId,
Count(distinct if(pd.firmcode = {0}, pd.FirmCode, NULL)) as SourceSuppliersSoldPosition,

sum(if(pd.firmcode in ({1}), ol.cost*ol.quantity, NULL)) as RivalsSum,
sum(if(pd.firmcode in ({1}), ol.quantity, NULL)) RivalsRows,
Min(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsMinCost,
Avg(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsAvgCost,
Max(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsMaxCost,
Count(distinct if(pd.firmcode in ({1}), oh.RowId, NULL)) as RivalsDistinctOrderId,
Count(distinct if(pd.firmcode in ({1}), oh.AddressId, NULL)) as RivalsDistinctAddressId,
Count(distinct if(pd.firmcode in ({1}), pd.FirmCode, NULL)) as RivalsSuppliersSoldPosition,

sum(ol.cost*ol.quantity) as AllSum,
sum(ol.quantity) AllRows,
Min(ol.cost) as AllMinCost,
Avg(ol.cost) as AllAvgCost,
Max(ol.cost) as AllMaxCost,
Count(distinct oh.RowId) as AllDistinctOrderId,
Count(distinct pd.firmcode) as AllSuppliersSoldPosition,
Count(distinct oh.AddressId) as AllDistinctAddressId ", sourceFirmCode, businessRivalsList));
			selectCommand +=
@"from " +
#if DEBUG
@"orders.OrdersHead oh
  join orders.OrdersList ol on ol.OrderID = oh.RowID ";
#else
@"ordersold.OrdersHead oh
  join ordersold.OrdersList ol on ol.OrderID = oh.RowID ";
#endif 

	if(!includeProductName || !isProductName || firmCrPosition)
		selectCommand +=
@"
  join catalogs.products p on p.Id = ol.ProductId";
	if(!includeProductName || firmCrPosition)
		selectCommand +=
@"
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId";

	selectCommand +=
@"
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
  left join future.Clients cl on cl.Id = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join future.suppliers prov on prov.Id = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join future.addresses adr on oh.AddressId = adr.Id
  join billing.LegalEntities le on adr.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId" +
	((showCode || showCodeCr) ? " left join ProviderCodes on ProviderCodes.CatalogCode = " + nameField.primaryField + 
	((firmCrField != null ? String.Format(" and ifnull(ProviderCodes.CodeFirmCr, 0) = ifnull({0}, 0)", firmCrField.primaryField): String.Empty)) : String.Empty) +
@"
where 
ol.Junk = 0
#and ol.Await = 0";

			selectCommand = ApplyFilters(selectCommand);
			selectCommand = ApplyGroupAndSort(selectCommand, "AllSum desc");

			if(firmCrPosition)
			{
				var groupPart = selectCommand.Substring(selectCommand.IndexOf("group by"));
				var new_groupPart = groupPart.Replace("cfc.Id", "cfc_id");
				selectCommand = selectCommand.Replace(groupPart, new_groupPart);
			}

		   

			if(includeProductName)
				if(isProductName)
					selectCommand += @"; select
				(select concat(c.name, ' ', 
							cast(GROUP_CONCAT(ifnull(PropertyValues.Value, '')
								  order by Properties.PropertyName, PropertyValues.Value
								  SEPARATOR ', '
								 ) as char))
				  from catalogs.products p
					join catalogs.catalog c on c.Id = p.CatalogId
					left join catalogs.ProductProperties on ProductProperties.ProductId = p.Id
					left join catalogs.PropertyValues on PropertyValues.Id = ProductProperties.PropertyValueId
					left join catalogs.Properties on Properties.Id = PropertyValues.PropertyId
				  where
					p.Id = md.pid) ProductName,
				  md.*
				from MixedData md";
				else
					selectCommand += @"; select
				(select c.name
				  from catalogs.catalog c
				  where
					c.Id = md.pid) CatalogName,
				  md.*
				from MixedData md";
#if DEBUG
			Debug.WriteLine(selectCommand);
#endif

			var selectTable = new DataTable();
			e.DataAdapter.SelectCommand.CommandText = selectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(selectTable);

			ProfileHelper.Next("GenerateReport3");
			var res = BuildResultTable(selectTable);

			DataColumn dc;
			if (showCode)
			{
				dc = res.Columns.Add("Code", typeof (String));
				dc.Caption = "Код";
				dc.SetOrdinal(0);
			}

			if (showCodeCr)
			{
				dc = res.Columns.Add("CodeCr", typeof (String));
				dc.Caption = "Код изготовителя";
				dc.SetOrdinal(1);
			}

			dc = res.Columns.Add("SourceFirmCodeSum", typeof (Decimal));
			dc.Caption = "Сумма по поставщику";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("SourceFirmCodeRows", typeof (Int32));
			dc.Caption = "Кол-во по постащику";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 4);
			dc = res.Columns.Add("SourceFirmCodeMinCost", typeof (Decimal));
			dc.Caption = "Минимальная цена по поставщику";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("SourceFirmCodeAvgCost", typeof (Decimal));
			dc.Caption = "Средняя цена по поставщику";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("SourceFirmCodeMaxCost", typeof (Decimal));
			dc.Caption = "Максимальная цена по поставщику";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("SourceFirmDistinctOrderId", typeof (Int32));
			dc.Caption = "Кол-во заявок по препарату по поставщику";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 4);			
			dc = res.Columns.Add("SourceFirmDistinctAddressId", typeof (Int32));
			dc.Caption = "Кол-во адресов доставки, заказавших препарат, по постащику";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 4);
			dc = res.Columns.Add("SourceSuppliersSoldPosition", typeof(Int32));
			dc.Caption = "Кол-во поставщиков";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("RivalsSum", typeof (Decimal));
			dc.Caption = "Сумма по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("RivalsRows", typeof (Int32));
			dc.Caption = "Кол-во по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 4);
			dc = res.Columns.Add("RivalsMinCost", typeof (Decimal));
			dc.Caption = "Минимальная цена по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("RivalsAvgCost", typeof (Decimal));
			dc.Caption = "Средняя цена по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("RivalsMaxCost", typeof (Decimal));
			dc.Caption = "Максимальная цена по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("RivalsDistinctOrderId", typeof (Int32));
			dc.Caption = "Кол-во заявок по препарату по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 4);			
			dc = res.Columns.Add("RivalsDistinctAddressId", typeof(Int32));
			dc.Caption = "Кол-во адресов доставки, заказавших препарат, по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 4);
			dc = res.Columns.Add("RivalsSuppliersSoldPosition", typeof(Int32));
			dc.Caption = "Кол-во поставщиков";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("AllSum", typeof (Decimal));
			dc.Caption = "Сумма по всем";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("AllRows", typeof (Int32));
			dc.Caption = "Кол-во по всем";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 4);
			dc = res.Columns.Add("AllMinCost", typeof (Decimal));
			dc.Caption = "Минимальная цена по всем";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("AllAvgCost", typeof (Decimal));
			dc.Caption = "Средняя цена по всем";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("AllMaxCost", typeof (Decimal));
			dc.Caption = "Максимальная цена по всем";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 8);
			dc = res.Columns.Add("AllDistinctOrderId", typeof (Int32));
			dc.Caption = "Кол-во заявок по препарату по всем";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 4);
			dc = res.Columns.Add("AllDistinctAddressId", typeof(Int32));
			dc.Caption = "Кол-во адресов доставки, заказавших препарат, по всем";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 4);
			dc = res.Columns.Add("AllSuppliersSoldPosition", typeof(Int32));
			dc.Caption = "Кол-во поставщиков";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)4);
			CopyData(selectTable, res);
		}

		protected override void PostProcessing(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{
			int freezeCount = selectedField.FindAll(x => x.visible).Count;
			if (showCode)
				freezeCount++;
			if (showCodeCr)
				freezeCount++;

			//Замораживаем некоторые колонки и столбцы
			ws.Range[ws.Cells[2 + filterDescriptions.Count, freezeCount + 1], ws.Cells[2 + filterDescriptions.Count, freezeCount + 1]].Select();
			exApp.ActiveWindow.FreezePanes = true;
		}

	}
}
