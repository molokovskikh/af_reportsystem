using System;
using System.ComponentModel;
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

using System.Collections.Generic;
using System.Drawing;
using Common.Models;
using NHibernate.Linq;

namespace Inforoom.ReportSystem
{
	public enum CodeSource
	{
		[Description("Кодами из прайс-листа и истории заказов")] OrdersAndPrices,
		[Description("Только кодами из заказов")] Orders
	}

	public class MixedReport : BaseOrdersReport
	{
		[Description("Скрыть статистику поставщика")]
		public bool HideSupplierStat;

		[Description("Исключить сроковые товары")]
		public bool HideJunk;

		[Description("Заполнять колонку код")]
		public CodeSource CodeSource;

		[Description("Позиция колонки \"Синоним поставщика\" в отчете")]
		public int? SupplierSynonymPosition;

		//Поставщик, по которому будет производиться отчет
		public int SourceFirmCode;
		//Отображать поле Code из прайс-листа поставщика?
		public bool ShowCode;
		//Отображать поле CodeCr из прайс-листа поставщика?
		public bool ShowCodeCr;

		protected const string businessRivalsProperty = "BusinessRivals";
		//Список поставщиков-конкурентов в виде строки
		protected List<List<ulong>> concurrentGroups = new List<List<ulong>>();

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

		public MixedReport()
		{
			HideJunk = false;
		}

		public MixedReport(MySqlConnection Conn, DataSet dsProperties)
			: base(Conn, dsProperties)
		{
			SupportProductNameOptimization = true;
			HideJunk = false;
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			if (SourceFirmCode == 0)
				throw new ReportException("Не установлен параметр \"Поставщик\".");

			foreach (var reportParam in _reportParams) {
				if (reportParam.Key.StartsWith(businessRivalsProperty)) {
					var items = reportParam.Value as List<ulong>;
					if (items != null && items.Count > 0) {
						concurrentGroups.Add(items);
					}
				}
			}

			//Пытаемся найти список ограничений по поставщику
			var firmCodeField = selectedField.Find(value => value.reportPropertyPreffix == "FirmCode");
			if (firmCodeField != null && firmCodeField.equalValues != null) {
				//Если в списке выбранных значений нет интересующего поставщика, то добавляем его туда
				//Для каждого поставщика из списка конкурентов проверяем: есть ли он в списке выбранных значений, если нет, то добавляем его
				firmCodeField.equalValues = firmCodeField.equalValues
					.Concat(concurrentGroups.SelectMany(l => l))
					.Concat(new[] { Convert.ToUInt64(SourceFirmCode) })
					.Distinct()
					.ToList();
			}
		}

		public override void CheckAfterLoadFields()
		{
			ProfileHelper.Next("BaseCheckAfterLoad");
			base.CheckAfterLoadFields();
			ProfileHelper.Next("CheckAfterLoad");
			//Выбираем поле "Производитель", если в настройке отчета есть соответствующий параметр
			firmCrField = selectedField.Find(value => value.reportPropertyPreffix == "FirmCr");

			if (SupplierSynonymPosition != null) {
				selectedField.Add(
					new FilterField {
						visible = true,
						position = SupplierSynonymPosition.Value,
						primaryField = "p.Id",
						viewField = "ifnull(s.Synonym, concat(c.Name, ' ', ifnull(p.Properties, ''))) as SupplierSynonym",
						outputField = "SupplierSynonym",
						outputCaption = "Наименование",
						width = 40
				});
			}

			//Проверяем, что выбран один из параметров для отображения: Наименование, Полное Наименование, Продукт
			var nameFields = selectedField.FindAll(
				value => (value.reportPropertyPreffix == "ProductName")
					|| value.reportPropertyPreffix == "FullName"
					|| value.reportPropertyPreffix == "ShortName"
					|| value.outputField == "SupplierSynonym");
			if (nameFields.Count == 0)
				throw new ReportException("Из полей \"Наименование продукта\", \"Полное наименование\", \"Наименование\", \"Синоним поставщика\" не выбрано ни одно поле.");
			if (nameFields.Count > 1)
				throw new ReportException("Из полей \"Наименование продукта\", \"Полное наименование\", \"Наименование\", \"Синоним поставщика\" должно быть выбрано только одно поле.");
			nameField = nameFields[0];
		}

		protected override void GenerateReport()
		{
			ProfileHelper.Next("GenerateReport");
			_supplierName = String.Format("Выбранный поставщик: {0}", GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from Customers.suppliers supps, farm.regions rg where rg.RegionCode = supps.HomeRegion and supps.Id = " + SourceFirmCode));
			Header.Add(_supplierName);
			for (var i = 0; i < concurrentGroups.Count; i++) {
				var ids = concurrentGroups[i];
				Header.Add(String.Format("Список поставщиков-конкурентов №{1}: {0}",
					GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from Customers.suppliers supps, farm.regions rg  where rg.RegionCode = supps.HomeRegion and supps.Id in (" + ids.Implode() + ") order by supps.Name"),
					i + 1));
			}

			if (ShowCode || ShowCodeCr)
				CalculateSupplierIds(SourceFirmCode, ShowCode, ShowCodeCr, CodeSource);

			ProfileHelper.Next("GenerateReport2");

			var selectCommand = BuildSelect();

			//max(cfc.Name) - судя по реализации mysql игнорирует null для min или max, если cfc.Name есть значение отличное от null
			//мы должны выбрать его а не null
			if (IncludeProducerName)
				selectCommand = selectCommand.Replace("cfc.Id", "if(c.Pharmacie = 1, cfc.Id, 0) as cfc_id")
					.Replace("cfc.Name", "if(c.Pharmacie = 1, max(cfc.Name), 'Нелекарственный ассортимент')");

			if (ShowCode)
				selectCommand += " ProviderCodes.Code, ";
			if (ShowCodeCr)
				selectCommand += " ProviderCodes.CodeCr, ";

			var concurrentSqlBlock = new StringBuilder();
			var filter = "";
			if (HideJunk) {
				filter = " and ol.Junk = 0 ";
				Header.Add("Из отчета исключены уцененные товары и товары с ограниченным сроком годности");
			}

			var priceIds = Session.Query<PriceList>().Where(x => x.Supplier.Id == SourceFirmCode)
				.ToArray()
				.Implode(x => x.PriceCode);
			if (String.IsNullOrEmpty(priceIds))
				priceIds = "0";
			CheckSuppliersCount(filter);

			for (var i = 0; i < concurrentGroups.Count; i++) {
				concurrentSqlBlock.AppendFormat(@"
sum(if(pd.firmcode in ({1}), ol.cost*ol.quantity, NULL)) as RivalsSum{0},
sum(if(pd.firmcode in ({1}), ol.quantity, NULL)) as RivalsRows{0},
Min(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsMinCost{0},
Avg(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsAvgCost{0},
Max(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsMaxCost{0},
Count(distinct if(pd.firmcode in ({1}), oh.RowId, NULL)) as RivalsDistinctOrderId{0},
Count(distinct if(pd.firmcode in ({1}), oh.AddressId, NULL)) as RivalsDistinctAddressId{0},
Count(distinct if(pd.firmcode in ({1}), pd.FirmCode, NULL)) as RivalsSuppliersSoldPosition{0},",
					i, concurrentGroups[i].Implode());
			}

			var groupBy = GetGroupSql();
			if (IncludeProducerName && IncludeProductName && ShowCode && ShowCodeCr) {
				groupBy = @"group by if(ProviderCodes.Code is null or ProviderCodes.CodeCr = '',
concat(ol.ProductId, '\t', ifnull(cfc_id, 0)), concat(ProviderCodes.Code, '\t', ifnull(ProviderCodes.CodeCr, '')))";
			}
			else if (IncludeProducerName) {
				groupBy = groupBy.Replace("cfc.Id", "cfc_id");
			}

			filter += " " + GetFilterSql();

			selectCommand += $@"
sum(if(pd.firmcode = {SourceFirmCode}, ol.cost*ol.quantity, NULL)) as SourceFirmCodeSum,
sum(if(pd.firmcode = {SourceFirmCode}, ol.quantity, NULL)) SourceFirmCodeRows,
Min(if(pd.firmcode = {SourceFirmCode}, ol.cost, NULL)) as SourceFirmCodeMinCost,
Avg(if(pd.firmcode = {SourceFirmCode}, ol.cost, NULL)) as SourceFirmCodeAvgCost,
Max(if(pd.firmcode = {SourceFirmCode}, ol.cost, NULL)) as SourceFirmCodeMaxCost,
Count(distinct if(pd.firmcode = {SourceFirmCode}, oh.RowId, NULL)) as SourceFirmDistinctOrderId,
Count(distinct if(pd.firmcode = {SourceFirmCode}, oh.AddressId, NULL)) as SourceFirmDistinctAddressId,
Count(distinct if(pd.firmcode = {SourceFirmCode}, pd.FirmCode, NULL)) as SourceSuppliersSoldPosition,

{concurrentSqlBlock}

sum(ol.cost*ol.quantity) as AllSum,
sum(ol.quantity) AllRows,
Min(ol.cost) as AllMinCost,
Avg(ol.cost) as AllAvgCost,
Max(ol.cost) as AllMaxCost,
Count(distinct oh.RowId) as AllDistinctOrderId,
Count(distinct pd.firmcode) as AllSuppliersSoldPosition,
Count(distinct oh.AddressId) as AllDistinctAddressId
from {OrdersSchema}.OrdersHead oh
  join {OrdersSchema}.OrdersList ol on ol.OrderID = oh.RowID
  join catalogs.products p on p.Id = ol.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId
  left join catalogs.mnn m on cn.MnnId = m.Id
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
  left join Customers.Clients cl on cl.Id = oh.ClientCode
  join customers.addresses ad on ad.Id = oh.AddressId
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join Customers.suppliers prov on prov.Id = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join Customers.addresses adr on oh.AddressId = adr.Id
  join billing.LegalEntities le on adr.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId
	left join (
		select * from Farm.Synonym
		where Fresh = 1 and PriceCode in ({priceIds})
		group by ProductId
	) as s on s.ProductId = ol.ProductId " +
				((ShowCode || ShowCodeCr) ? " left join ProviderCodes on ProviderCodes.CatalogCode = " + nameField.primaryField +
					((firmCrField != null ?
						$" and ifnull(ProviderCodes.CodeFirmCr, 0) = if(c.Pharmacie = 1, ifnull({firmCrField.primaryField}, 0), 0)"
						: String.Empty)) : String.Empty) +
				$@"
where pd.IsLocal = 0
	{filter}
{groupBy}
order by AllSum desc
";

			//selectCommand = ApplyFilters(selectCommand);

			if (IncludeProductName)
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

			var selectTable = new DataTable();
			DataAdapter.SelectCommand.CommandText = selectCommand;
			DataAdapter.SelectCommand.Parameters.Clear();
#if DEBUG
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
#endif

			DataAdapter.Fill(selectTable);

			ProfileHelper.Next("GenerateReport3");

			if (!HideSupplierStat) {
				GroupHeaders.Add(new ColumnGroupHeader(
					_supplierName,
					"SourceFirmCodeSum",
					"SourceSuppliersSoldPosition"));
			}
			for (var i = 0; i < concurrentGroups.Count; i++) {
				GroupHeaders.Add(new ColumnGroupHeader(
					$"Список поставщиков-конкурентов №{i + 1}",
					"RivalsSum" + i,
					"RivalsSuppliersSoldPosition" + i));
			}
			GroupHeaders.Add(new ColumnGroupHeader(
				"Общие данные по рынку",
				"AllSum",
				"AllSuppliersSoldPosition"));

			var res = BuildResultTable(selectTable);

			DataColumn dc;
			if (ShowCode) {
				dc = res.Columns.Add("Code", typeof(String));
				dc.Caption = "Код";
				dc.ExtendedProperties.Add("Width", (int?)20);
				dc.SetOrdinal(0);
			}

			if (ShowCodeCr) {
				dc = res.Columns.Add("CodeCr", typeof(String));
				dc.Caption = "Код изготовителя";
				dc.ExtendedProperties.Add("Width", (int?)20);
				dc.SetOrdinal(1);
			}

			if (!HideSupplierStat) {
				var groupColor = Color.FromArgb(197, 217, 241);
				dc = res.Columns.Add("SourceFirmCodeSum", typeof(Decimal));
				dc.Caption = "Сумма по поставщику";
				dc.ExtendedProperties.Add("Color", groupColor);
				dc.ExtendedProperties.Add("Width", (int?)8);
				dc = res.Columns.Add("SourceFirmCodeRows", typeof(Int32));
				dc.Caption = "Кол-во по поставщику";
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
				dc.Caption = "Кол-во адресов доставки, заказавших препарат, по поставщику";
				dc.ExtendedProperties.Add("Color", groupColor);
				dc.ExtendedProperties.Add("Width", (int?)4);
				dc = res.Columns.Add("SourceSuppliersSoldPosition", typeof(Int32));
				dc.Caption = "Кол-во поставщиков";
				dc.ExtendedProperties.Add("Color", groupColor);
				dc.ExtendedProperties.Add("Width", (int?)4);
			}

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
			if (ShowCode)
				freezeCount++;
			if (ShowCodeCr)
				freezeCount++;

			ExcelHelper.SafeFreeze(ws, freezeCount);
		}
	}
}