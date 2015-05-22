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

namespace Inforoom.ReportSystem
{
	public class MixedReport : OrdersReport
	{
		[Description("Скрыть статистику поставщика")]
		public bool HideSupplierStat;

		[Description("Исключить сроковые товары")]
		public bool HideJunk;
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

		public MixedReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
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

			//Проверяем, что выбран один из параметров для отображения: Наименование, Полное Наименование, Продукт
			var nameFields = selectedField.FindAll(
				value => (value.reportPropertyPreffix == "ProductName") || value.reportPropertyPreffix == "FullName" || value.reportPropertyPreffix == "ShortName");
			if (nameFields.Count == 0)
				throw new ReportException("Из полей \"Наименование продукта\", \"Полное наименование\", \"Наименование\" не выбрано ни одно поле.");
			if (nameFields.Count > 1)
				throw new ReportException("Из полей \"Наименование продукта\", \"Полное наименование\", \"Наименование\" должно быть выбрано только одно поле.");
			nameField = nameFields[0];
		}

		protected override void GenerateReport()
		{
			ProfileHelper.Next("GenerateReport");
			_supplierName = String.Format("Выбранный поставщик: {0}", GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from Customers.suppliers supps, farm.regions rg where rg.RegionCode = supps.HomeRegion and supps.Id = " + SourceFirmCode));
			FilterDescriptions.Add(_supplierName);
			for (var i = 0; i < concurrentGroups.Count; i++) {
				var ids = concurrentGroups[i];
				FilterDescriptions.Add(String.Format("Список поставщиков-конкурентов №{1}: {0}",
					GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from Customers.suppliers supps, farm.regions rg  where rg.RegionCode = supps.HomeRegion and supps.Id in (" + ids.Implode() + ") order by supps.Name"),
					i + 1));
			}

			if (ShowCode || ShowCode)
				CalculateSupplierIds(SourceFirmCode, ShowCode, ShowCode);

			ProfileHelper.Next("GenerateReport2");

			var selectCommand = BuildSelect();

			if (firmCrPosition)
				selectCommand = selectCommand.Replace("cfc.Id", "if(c.Pharmacie = 1, cfc.Id, 0) as cfc_id")
					.Replace("cfc.Name", "if(c.Pharmacie = 1, cfc.Name, 'Нелекарственный ассортимент')");

			if (ShowCode)
				selectCommand += " ProviderCodes.Code, ";
			if (ShowCode)
				selectCommand += " ProviderCodes.CodeCr, ";

			var concurrentSqlBlock = new StringBuilder();
			var filter = "";
			if (HideJunk) {
				filter = " and ol.Junk = 0 ";
				FilterDescriptions.Add("Из отчета исключены уцененные товары и товары с ограниченным сроком годности");
			}

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
Count(distinct oh.AddressId) as AllDistinctAddressId ", SourceFirmCode, concurrentSqlBlock));
			selectCommand += String.Format(@"
from {0}.OrdersHead oh
  join {0}.OrdersList ol on ol.OrderID = oh.RowID
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
  join billing.payers on payers.PayerId = le.PayerId", OrdersSchema) +
				((ShowCode || ShowCode) ? " left join ProviderCodes on ProviderCodes.CatalogCode = " + nameField.primaryField +
					((firmCrField != null ? String.Format(" and ifnull(ProviderCodes.CodeFirmCr, 0) = if(c.Pharmacie = 1, ifnull({0}, 0), 0)", firmCrField.primaryField) : String.Empty)) : String.Empty) +
						String.Format(@"
where pd.IsLocal = 0
	{0}
", filter);

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
			args.DataAdapter.SelectCommand.CommandText = selectCommand;
			args.DataAdapter.SelectCommand.Parameters.Clear();
			args.DataAdapter.Fill(selectTable);

			ProfileHelper.Next("GenerateReport3");

			if (!HideSupplierStat) {
				GroupHeaders.Add(new ColumnGroupHeader(
					String.Format("{0}", _supplierName),
					"SourceFirmCodeSum",
					"SourceSuppliersSoldPosition"));
			}
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
			if (ShowCode) {
				dc = res.Columns.Add("Code", typeof(String));
				dc.Caption = "Код";
				dc.SetOrdinal(0);
			}

			if (ShowCode) {
				dc = res.Columns.Add("CodeCr", typeof(String));
				dc.Caption = "Код изготовителя";
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
			if (ShowCode)
				freezeCount++;

			var begin = ws.Cells[2 + FilterDescriptions.Count, freezeCount + 1];
			var end = ws.Cells[2 + FilterDescriptions.Count, freezeCount + 1];
			ExcelHelper.SafeFreeze(ws, begin, end);
		}
	}
}