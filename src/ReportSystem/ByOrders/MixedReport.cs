using System;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Text;
using Common.MySql;
using Common.Tools;
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

		//Список постащиков-конкурентов в виде строки
		protected List<List<ulong>> concurrentGroups = new List<List<ulong>>();

		//Отображать поле Code из прайс-листа поставщика?
		protected bool showCode;
		//Отображать поле CodeCr из прайс-листа поставщика?
		protected bool showCodeCr;

		//Одно из полей "Наименование продукта", "Полное наименование", "Наименование"
		protected FilterField nameField;
		//Поле производитель
		protected FilterField firmCrField;

		private string _supplierName;

		/// <summary>
		/// Получаем таблицу результатов для проверки в тестах
		/// </summary>
		public DataTable DSResult
		{
			get { return _dsReport.Tables["Results"]; }
		}

		public MixedReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
			SupportProductNameOptimization = true;
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			showCode = reportParamExists(showCodeProperty) && (bool)getReportParam(showCodeProperty); // показывать код поставщика
			showCodeCr = reportParamExists(showCodeCrProperty) && (bool)getReportParam(showCodeCrProperty); // показывать код изготовителя

			sourceFirmCode = (int)getReportParam(sourceFirmCodeProperty); // поставщик
			if (sourceFirmCode == 0)
				throw new ReportException("Не установлен параметр \"Поставщик\".");

			foreach (var reportParam in _reportParams) {
				if (reportParam.Key.StartsWith(businessRivalsProperty)) {
					var items = reportParam.Value as List<ulong>;
					if (items != null && items.Count > 0) {
						concurrentGroups.Add(items);
					}
				}
			}

			if (concurrentGroups.Count == 0)
				throw new ReportException("Не установлен параметр \"Список конкурентов\".");

			//Пытаемся найти список ограничений по постащику
			var firmCodeField = selectedField.Find(value => value.reportPropertyPreffix == "FirmCode");
			if (firmCodeField != null && firmCodeField.equalValues != null) {
				//Если в списке выбранных значений нет интересующего поставщика, то добавляем его туда
				//Для каждого поставщика из списка конкурентов проверяем: есть ли он в списке выбранных значений, если нет, то добавляем его
				firmCodeField.equalValues = firmCodeField.equalValues
					.Concat(concurrentGroups.SelectMany(l => l))
					.Concat(new[] { Convert.ToUInt64(sourceFirmCode) })
					.Distinct()
					.ToList();
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

		private void FillProviderCodes(ExecuteArgs e)
		{
			ProfileHelper.Next("FillCodes");
			var groupExpression = nameField.primaryField + ((firmCrField != null) ? ", " + String.Format("if (c.Pharmacie = 1, {0}, 0)", firmCrField.primaryField) : String.Empty);
			var selectExpression = nameField.primaryField + ((firmCrField != null) ? ", " + String.Format("if (c.Pharmacie = 1, {0}, 0)", firmCrField.primaryField) : ", null ");

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
											((showCode) ? "group_concat(CoreCodes.Code), " : String.Empty) +
												((showCodeCr) ? "CoreCodes.CodeCr, " : String.Empty) +
													selectExpression +
														@" from ((
(
select
distinct " +
															((showCode) ? "ol.Code, " : String.Empty) +
																((showCodeCr) ? "ol.CodeCr, " : String.Empty) +
																	String.Format(@"
  ol.ProductId,
  ol.CodeFirmCr
from {0}.OrdersHead oh,
  {0}.OrdersList ol,
  usersettings.pricesdata pd
where
	ol.OrderID = oh.RowID
	and ol.Junk = 0
	and pd.PriceCode = oh.PriceCode
and pd.Enabled = 1
and exists (select
  *
from
  usersettings.pricescosts pc1,
  usersettings.priceitems pim1,
  farm.formrules fr1
where
	pc1.PriceCode = pd.PriceCode
and pc1.BaseCost = 1
and pim1.Id = pc1.PriceItemId
and fr1.Id = pim1.FormRuleId
and (to_days(now())-to_days(pim1.PriceDate)) < fr1.MaxOld)
	and pd.FirmCode = ", OrdersSchema) + sourceFirmCode.ToString() +
																		" and oh.WriteTime > '" + dtFrom.ToString(MySqlConsts.MySQLDateFormat) + "' " +
																			" and oh.WriteTime < '" + dtTo.ToString(MySqlConsts.MySQLDateFormat) + "' " +
																				@")
union
(
select
distinct " +
																					((showCode) ? "core.Code, " : String.Empty) +
																						((showCodeCr) ? "core.CodeCr, " : String.Empty) +
																							@"
  core.ProductId,
  core.CodeFirmCr
from
  usersettings.Pricesdata pd,
  farm.Core0 core
where
	pd.FirmCode = " + sourceFirmCode.ToString() + @"
	and core.PriceCode = pd.PriceCode
and pd.Enabled = 1
and exists (select
  *
from
  usersettings.pricescosts pc1,
  usersettings.priceitems pim1,
  farm.formrules fr1
where
	pc1.PriceCode = pd.PriceCode
and pc1.BaseCost = 1
and pim1.Id = pc1.PriceItemId
and fr1.Id = pim1.FormRuleId
and (to_days(now())-to_days(pim1.PriceDate)) < fr1.MaxOld)
)) CoreCodes)
  join catalogs.products p on p.Id = CoreCodes.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  left join catalogs.Producers cfc on CoreCodes.CodeFirmCr = cfc.Id
group by " + groupExpression;

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("GenerateReport");
			_supplierName = String.Format("Выбранный поставщик: {0}", GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from Customers.suppliers supps, farm.regions rg where rg.RegionCode = supps.HomeRegion and supps.Id = " + sourceFirmCode));
			FilterDescriptions.Add(_supplierName);
			for (var i = 0; i < concurrentGroups.Count; i++) {
				var ids = concurrentGroups[i];
				FilterDescriptions.Add(String.Format("Список поставщиков-конкурентов №{1}: {0}",
					GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from Customers.suppliers supps, farm.regions rg  where rg.RegionCode = supps.HomeRegion and supps.Id in (" + ids.Implode() + ") order by supps.Name"),
					i + 1));
			}

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

			var concurrentSqlBlock = new StringBuilder();

			for (var i = 0; i < concurrentGroups.Count; i++) {
				concurrentSqlBlock.AppendFormat(@"
sum(if(pd.firmcode in ({1}), ol.cost*ol.quantity, NULL)) as RivalsSum{0},
sum(if(pd.firmcode in ({1}), ol.quantity, NULL)) as RivalsRows{0},
Min(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsMinCost{0},
Avg(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsAvgCost{0},
Max(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsMaxCost{0},
Count(distinct if(pd.firmcode in ({1}), oh.RowId, NULL)) as RivalsDistinctOrderId{0},
Count(distinct if(pd.firmcode in ({1}), oh.AddressId, NULL)) as RivalsDistinctAddressId{0},
Count(distinct if(pd.firmcode in ({1}), pd.FirmCode, NULL)) as RivalsSuppliersSoldPosition{0},", i, concurrentGroups[i].Implode());
			}

			selectCommand = String.Concat(selectCommand, String.Format(@"
sum(if(pd.firmcode = {0}, ol.cost*ol.quantity, NULL)) as SourceFirmCodeSum,
sum(if(pd.firmcode = {0}, ol.quantity, NULL)) SourceFirmCodeRows,
Min(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeMinCost,
Avg(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeAvgCost,
Max(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeMaxCost,
Count(distinct if(pd.firmcode = {0}, oh.RowId, NULL)) as SourceFirmDistinctOrderId,
Count(distinct if(pd.firmcode = {0}, oh.AddressId, NULL)) as SourceFirmDistinctAddressId,
Count(distinct if(pd.firmcode = {0}, pd.FirmCode, NULL)) as SourceSuppliersSoldPosition,

{1}

sum(ol.cost*ol.quantity) as AllSum,
sum(ol.quantity) AllRows,
Min(ol.cost) as AllMinCost,
Avg(ol.cost) as AllAvgCost,
Max(ol.cost) as AllMaxCost,
Count(distinct oh.RowId) as AllDistinctOrderId,
Count(distinct pd.firmcode) as AllSuppliersSoldPosition,
Count(distinct oh.AddressId) as AllDistinctAddressId ", sourceFirmCode, concurrentSqlBlock));
			selectCommand +=
				@"from " +
#if DEBUG
					@"orders.OrdersHead oh
  join orders.OrdersList ol on ol.OrderID = oh.RowID ";
#else
@"ordersold.OrdersHead oh
  join ordersold.OrdersList ol on ol.OrderID = oh.RowID ";
#endif

			if (!includeProductName || !isProductName || firmCrPosition)
				selectCommand += @"
  join catalogs.products p on p.Id = ol.ProductId";

			if (!includeProductName || firmCrPosition)
				selectCommand += @"
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId";

			selectCommand += @"
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
  left join Customers.Clients cl on cl.Id = oh.ClientCode
  join customers.addresses ad on ad.Id = oh.AddressId
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join Customers.suppliers prov on prov.Id = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join Customers.addresses adr on oh.AddressId = adr.Id
  join billing.LegalEntities le on adr.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId" +
				((showCode || showCodeCr) ? " left join ProviderCodes on ProviderCodes.CatalogCode = " + nameField.primaryField +
					((firmCrField != null ? String.Format(" and ifnull(ProviderCodes.CodeFirmCr, 0) = if(c.Pharmacie = 1, ifnull({0}, 0), 0)", firmCrField.primaryField) : String.Empty)) : String.Empty) +
						@"
where
ol.Junk = 0
";

			selectCommand = ApplyFilters(selectCommand);
			selectCommand = ApplyGroupAndSort(selectCommand, "AllSum desc");

			if (firmCrPosition) {
				var groupPart = selectCommand.Substring(selectCommand.IndexOf("group by"));
				var new_groupPart = groupPart.Replace("cfc.Id", "cfc_id");
				selectCommand = selectCommand.Replace(groupPart, new_groupPart);
			}

			if (includeProductName)
				if (isProductName)
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

			GroupHeaders.Add(new ColumnGroupHeader(
				String.Format("Выбранный поставщик: {0}", _supplierName),
				"SourceFirmCodeSum",
				"SourceSuppliersSoldPosition"));
			for (var i = 0; i < concurrentGroups.Count; i++) {
				GroupHeaders.Add(new ColumnGroupHeader(
					String.Format("Список поставщиков-конкурентов №{0}", i + 1),
					"RivalsSum" + i,
					"RivalsSuppliersSoldPosition" + i));
			}
			GroupHeaders.Add(new ColumnGroupHeader(
				"Общие данные по рынку",
				"AllSum",
				"AllSuppliersSoldPosition"));

			var res = BuildResultTable(selectTable);

			DataColumn dc;
			if (showCode) {
				dc = res.Columns.Add("Code", typeof(String));
				dc.Caption = "Код";
				dc.SetOrdinal(0);
			}

			if (showCodeCr) {
				dc = res.Columns.Add("CodeCr", typeof(String));
				dc.Caption = "Код изготовителя";
				dc.SetOrdinal(1);
			}

			var groupColor = Color.FromArgb(197, 217, 241);
			dc = res.Columns.Add("SourceFirmCodeSum", typeof(Decimal));
			dc.Caption = "Сумма по поставщику";
			dc.ExtendedProperties.Add("Color", groupColor);
			dc.ExtendedProperties.Add("Width", (int?)8);
			dc = res.Columns.Add("SourceFirmCodeRows", typeof(Int32));
			dc.Caption = "Кол-во по постащику";
			dc.ExtendedProperties.Add("Color", groupColor);
			dc.ExtendedProperties.Add("Width", (int?)4);
			dc = res.Columns.Add("SourceFirmCodeMinCost", typeof(Decimal));
			dc.Caption = "Минимальная цена по поставщику";
			dc.ExtendedProperties.Add("Color", groupColor);
			dc.ExtendedProperties.Add("Width", (int?)8);
			dc = res.Columns.Add("SourceFirmCodeAvgCost", typeof(Decimal));
			dc.Caption = "Средняя цена по поставщику";
			dc.ExtendedProperties.Add("Color", groupColor);
			dc.ExtendedProperties.Add("Width", (int?)8);
			dc = res.Columns.Add("SourceFirmCodeMaxCost", typeof(Decimal));
			dc.Caption = "Максимальная цена по поставщику";
			dc.ExtendedProperties.Add("Color", groupColor);
			dc.ExtendedProperties.Add("Width", (int?)8);
			dc = res.Columns.Add("SourceFirmDistinctOrderId", typeof(Int32));
			dc.Caption = "Кол-во заявок по препарату по поставщику";
			dc.ExtendedProperties.Add("Color", groupColor);
			dc.ExtendedProperties.Add("Width", (int?)4);
			dc = res.Columns.Add("SourceFirmDistinctAddressId", typeof(Int32));
			dc.Caption = "Кол-во адресов доставки, заказавших препарат, по постащику";
			dc.ExtendedProperties.Add("Color", groupColor);
			dc.ExtendedProperties.Add("Width", (int?)4);
			dc = res.Columns.Add("SourceSuppliersSoldPosition", typeof(Int32));
			dc.Caption = "Кол-во поставщиков";
			dc.ExtendedProperties.Add("Color", groupColor);
			dc.ExtendedProperties.Add("Width", (int?)4);

			for (var i = 0; i < concurrentGroups.Count; i++) {
				var color = Color.FromArgb(234, 241, 221);
				var hue = (color.GetHue() + i * 40) % 360;
				color = ColorHelper.FromAhsb(255, hue, color.GetSaturation(), color.GetBrightness());

				dc = res.Columns.Add("RivalsSum" + i, typeof(Decimal));
				dc.Caption = "Сумма по конкурентам";
				dc.ExtendedProperties.Add("Color", color);
				dc.ExtendedProperties.Add("Width", (int?)8);

				dc = res.Columns.Add("RivalsRows" + i, typeof(Int32));
				dc.Caption = "Кол-во по конкурентам";
				dc.ExtendedProperties.Add("Color", color);
				dc.ExtendedProperties.Add("Width", (int?)4);

				dc = res.Columns.Add("RivalsMinCost" + i, typeof(Decimal));
				dc.Caption = "Минимальная цена по конкурентам";
				dc.ExtendedProperties.Add("Color", color);
				dc.ExtendedProperties.Add("Width", (int?)8);

				dc = res.Columns.Add("RivalsAvgCost" + i, typeof(Decimal));
				dc.Caption = "Средняя цена по конкурентам";
				dc.ExtendedProperties.Add("Color", color);
				dc.ExtendedProperties.Add("Width", (int?)8);

				dc = res.Columns.Add("RivalsMaxCost" + i, typeof(Decimal));
				dc.Caption = "Максимальная цена по конкурентам";
				dc.ExtendedProperties.Add("Color", color);
				dc.ExtendedProperties.Add("Width", (int?)8);

				dc = res.Columns.Add("RivalsDistinctOrderId" + i, typeof(Int32));
				dc.Caption = "Кол-во заявок по препарату по конкурентам";
				dc.ExtendedProperties.Add("Color", color);
				dc.ExtendedProperties.Add("Width", (int?)4);

				dc = res.Columns.Add("RivalsDistinctAddressId" + i, typeof(Int32));
				dc.Caption = "Кол-во адресов доставки, заказавших препарат, по конкурентам";
				dc.ExtendedProperties.Add("Color", color);
				dc.ExtendedProperties.Add("Width", (int?)4);

				dc = res.Columns.Add("RivalsSuppliersSoldPosition" + i, typeof(Int32));
				dc.Caption = "Кол-во поставщиков";
				dc.ExtendedProperties.Add("Color", color);
				dc.ExtendedProperties.Add("Width", (int?)4);
			}


			var lastGroupColor = Color.FromArgb(253, 233, 217);
			dc = res.Columns.Add("AllSum", typeof(Decimal));
			dc.Caption = "Сумма по всем";
			dc.ExtendedProperties.Add("Color", lastGroupColor);
			dc.ExtendedProperties.Add("Width", (int?)8);
			dc = res.Columns.Add("AllRows", typeof(Int32));
			dc.Caption = "Кол-во по всем";
			dc.ExtendedProperties.Add("Color", lastGroupColor);
			dc.ExtendedProperties.Add("Width", (int?)4);
			dc = res.Columns.Add("AllMinCost", typeof(Decimal));
			dc.Caption = "Минимальная цена по всем";
			dc.ExtendedProperties.Add("Color", lastGroupColor);
			dc.ExtendedProperties.Add("Width", (int?)8);
			dc = res.Columns.Add("AllAvgCost", typeof(Decimal));
			dc.Caption = "Средняя цена по всем";
			dc.ExtendedProperties.Add("Color", lastGroupColor);
			dc.ExtendedProperties.Add("Width", (int?)8);
			dc = res.Columns.Add("AllMaxCost", typeof(Decimal));
			dc.Caption = "Максимальная цена по всем";
			dc.ExtendedProperties.Add("Color", lastGroupColor);
			dc.ExtendedProperties.Add("Width", (int?)8);
			dc = res.Columns.Add("AllDistinctOrderId", typeof(Int32));
			dc.Caption = "Кол-во заявок по препарату по всем";
			dc.ExtendedProperties.Add("Color", lastGroupColor);
			dc.ExtendedProperties.Add("Width", (int?)4);
			dc = res.Columns.Add("AllDistinctAddressId", typeof(Int32));
			dc.Caption = "Кол-во адресов доставки, заказавших препарат, по всем";
			dc.ExtendedProperties.Add("Color", lastGroupColor);
			dc.ExtendedProperties.Add("Width", (int?)4);
			dc = res.Columns.Add("AllSuppliersSoldPosition", typeof(Int32));
			dc.Caption = "Кол-во поставщиков";
			dc.ExtendedProperties.Add("Color", lastGroupColor);
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

			//будь бдителен! перед тем как замораживать колонки нужно переместить окно к начальной колонке
			//тк замораживаемая колонка заморожавается в позиции отностительно текущего окна
			//пример, есть таблица с четырьмя колонками
			//1|2|3|4
			//предположим я хочу заморозить колонки 1 и 2 что бы их значения были всегда видны
			//если я буду замороживать колонку 2 когда окно будет в состоянии
			//------
			//2|3|4|
			//2|3|4|
			//2|3|4|
			//------
			//колонка 2 замерзнет и увидеть колонку 1 будет невозможно
			//что бы правильно заморозить нам нужно отобразить колонку 1
			//те привести окно к виду
			//------
			//1|2|3|
			//1|2|3|
			//1|2|3|
			//------
			//это делает код ниже
			//почему окно может оказаться в таком положении? хз
			//навеное одна из более ранних операций приводит его в такое состояние
			//возможно это операция выделения
			//всего скорее это стрельнет только на сервере если запускать под runer
			//тк runer не ходит интерактивно у него не будет настройки разрешения
			//и excel запустит в малом окне и если данных будет много это приведет к тому
			//что окно при операция будет перемещаться
			exApp.ActiveWindow.ScrollIntoView(0, 0, 200, 200, true);
			//Замораживаем некоторые колонки и столбцы
			ws.Range[ws.Cells[2 + FilterDescriptions.Count, freezeCount + 1], ws.Cells[2 + FilterDescriptions.Count, freezeCount + 1]].Select();
			exApp.ActiveWindow.FreezePanes = true;
		}
	}
}