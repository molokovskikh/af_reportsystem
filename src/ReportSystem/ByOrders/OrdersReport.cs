﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using Inforoom.ReportSystem.Filters;
using ExecuteTemplate;
using System.Data;
using DataTable = System.Data.DataTable;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	public class ColumnGroupHeader
	{
		public ColumnGroupHeader(string title, string beginColumn, string endColumn)
		{
			Title = title;
			BeginColumn = beginColumn;
			EndColumn = endColumn;
		}

		public string Title;
		public string BeginColumn;
		public string EndColumn;
	}

	public class OrdersReport : BaseReport
	{
		public List<FilterField> registredField;
		public List<FilterField> selectedField;

		protected DateTime dtFrom;
		protected DateTime dtTo;

		protected bool SupportProductNameOptimization;
		protected bool includeProductName;
		protected bool isProductName = true;
		protected bool firmCrPosition; // есть ли параметр "Позиция производителя"
		protected string OrdersSchema = "Orders";

		private string[] nameFields = new[] { "FullName", "ShortName", "ProductName" };

		public OrdersReport()
		{
			Init();
		}

		public OrdersReport(ulong reportCode, string reportCaption, MySqlConnection conn, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, conn, format, dsProperties)
		{
#if !DEBUG
			OrdersSchema = "OrdersOld";
#endif
			Init();
		}

		[Description("За предыдущий месяц")]
		public bool ByPreviousMonth { get; set; }

		[Description("Интервал отчета (дни) от текущей даты")]
		public int ReportInterval { get; set; }

		private void Init()
		{
			selectedField = new List<FilterField>();
			registredField = new List<FilterField>();
			registredField.Add(new FilterField("p.Id", @"concat(cn.Name, ' ', cf.Form, ' ',
			  (select
				 ifnull(GROUP_CONCAT(ifnull(PropertyValues.Value, '')
									order by Properties.PropertyName, PropertyValues.Value
									SEPARATOR ', '), '')
			  from
				 catalogs.products inp
				 left join catalogs.ProductProperties on ProductProperties.ProductId = inp.Id
				 left join catalogs.PropertyValues on PropertyValues.Id = ProductProperties.PropertyValueId
				 left join catalogs.Properties on Properties.Id = PropertyValues.PropertyId
			   where inp.Id = p.Id)) as ProductName",
				"ProductName", "ProductName", "Наименование и форма выпуска",
				"catalogs.products p, catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf",
				0,
				"В отчет включены следующие продукты", "Следующие продукты исключены из отчета", 40) {
					whereList = "and c.Id = p.CatalogId and cn.id = c.NameId and cf.Id = c.FormId"
				});

			registredField.Add(new FilterField("c.Id", "concat(cn.Name, ' ', cf.Form) as CatalogName", "CatalogName", "FullName",
				"Наименование и форма выпуска",
				"catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf",
				0,
				"В отчет включены следующие наименования",
				"Следующие наименования исключены из отчета",
				40) {
					whereList = "and cn.id = c.NameId and cf.Id = c.FormId"
				});
			registredField.Add(new FilterField("cn.Id", "cn.Name as PosName", "PosName", "ShortName", "Наименование", "catalogs.catalognames cn", 0,
				"В отчет включены следующие наименования",
				"Следующие наименования исключены из отчета", 40));
			registredField.Add(new FilterField("m.Id", "m.Mnn", "Mnn", "Mnn", "МНН", "catalogs.mnn m",
				41,
				"В отчет включены следующие МНН",
				"Следующие МНН исключены из отчета") {
					Nullable = true,
					width = 40
				});
			registredField.Add(new FilterField("cfc.Id", "cfc.Name as FirmCr", "FirmCr", "FirmCr", "Производитель", "catalogs.Producers cfc", 1,
				"В отчет включены следующие производители",
				"Следующие производители исключены из отчета", 15));
			registredField.Add(new FilterField("rg.RegionCode", "rg.Region as RegionName", "RegionName", "Region", "Регион", "farm.regions rg", 2,
				"В отчет включены следующие регионы",
				"Следующие регионы исключены из отчета"));
			registredField.Add(new FilterField("prov.Id", "concat(prov.Name, ' - ', provrg.Region) as FirmShortName", "FirmShortName", "FirmCode", "Поставщик",
				"Customers.suppliers prov, farm.regions provrg",
				3,
				"В отчет включены следующие поставщики", "Следующие поставщики исключены из отчета",
				10) {
					whereList = "and prov.HomeRegion = provrg.RegionCode"
				});
			registredField.Add(new FilterField("pd.PriceCode", "concat(prov.Name , ' (', pd.PriceName, ') - ', provrg.Region) as PriceName", "PriceName", "PriceCode", "Прайс-лист",
				"usersettings.pricesdata pd, Customers.suppliers prov, farm.regions provrg",
				4,
				"В отчет включены следующие прайс-листы поставщиков",
				"Следующие прайс-листы поставщиков исключены из отчета",
				10) {
					whereList = "and prov.Id = pd.FirmCode and prov.HomeRegion = provrg.RegionCode",
				});
			registredField.Add(new FilterField("cl.Id", "cl.Name as ClientShortName", "ClientShortName", "ClientCode", "Аптека", "Customers.clients cl", 5,
				"В отчет включены следующие аптеки",
				"Следующие аптеки исключены из отчета",
				10));
			registredField.Add(new FilterField("payers.PayerId", "payers.ShortName as PayerName", "PayerName", "Payer", "Плательщик", "billing.payers", 6,
				"В отчет включены следующие плательщики",
				"Следующие плательщики исключены из отчета"));
			registredField.Add(new FilterField("ad.Id", "concat(ad.Address, ' (', cl.Name, ') ') as AddressName", "AddressName", "Addresses", "Адрес доставки",
				"customers.addresses ad, Customers.Clients cl", 7,
				"В отчет включены следующие адреса доставки",
				"Следующие адреса доставки исключены из отчета") {
					whereList = "and ad.ClientId = cl.Id"
				});
			registredField.Add(new FilterField {
				primaryField = "ol.Code",
				viewField = "ol.Code as SupplierProductCode",
				outputField = "SupplierProductCode",
				reportPropertyPreffix = "SupplierProductCode",
				outputCaption = "Оригинальный код товара",
				position = 8
			});
		}

		public override void ReadReportParams()
		{
			foreach (var property in GetType().GetProperties()) {
				if (reportParamExists(property.Name)) {
					property.SetValue(this, getReportParam(property.Name), null);
				}
			}

			if (Interval) {
				dtFrom = From;
				dtTo = To;
				dtTo = dtTo.Date.AddDays(1);
			}
			else if (ByPreviousMonth) {
				dtTo = DateTime.Today;
				dtTo = dtTo.AddDays(-(dtTo.Day - 1)).Date; // Первое число текущего месяца
				dtFrom = dtTo.AddMonths(-1).Date;
			}
			else {
				dtTo = DateTime.Today;
				//От текущей даты вычитаем интервал - дата начала отчета
				dtFrom = dtTo.AddDays(-ReportInterval).Date;
			}
			FilterDescriptions.Add(String.Format("Период дат: {0} - {1}", dtFrom.ToString("dd.MM.yyyy HH:mm:ss"), dtTo.ToString("dd.MM.yyyy HH:mm:ss")));

			LoadFilters();
			CheckAfterLoadFields();
			SortFields();
		}

		protected void LoadFilters()
		{
			selectedField = registredField.Where(f => f.LoadFromDB(this)).ToList();
		}

		public void SortFields()
		{
			var mnn = selectedField.FirstOrDefault(f => f.reportPropertyPreffix == "Mnn");
			if (mnn != null) {
				var names = selectedField.Where(f => nameFields.Contains(f.reportPropertyPreffix));
				var maxPosition = names.Max(n => n.position);
				selectedField
					.Except(names)
					.Where(f => f.position >= maxPosition)
					.Each(f => f.position += Math.Max(1, f.position - maxPosition));
				mnn.position = maxPosition + 1;
			}

			selectedField.Sort((x, y) => (x.position - y.position));
		}

		public virtual void CheckAfterLoadFields()
		{
			firmCrPosition = reportParamExists("FirmCrPosition");

			var mnn = selectedField.FirstOrDefault(f => f.reportPropertyPreffix == "Mnn");
			var names = selectedField.Where(f => nameFields.Contains(f.reportPropertyPreffix));
			if (mnn != null && !names.Any()) {
				selectedField.Remove(mnn);
			}
		}

		public override void GenerateReport(ExecuteArgs e)
		{
		}

		protected string ApplyFilters(string selectCommand, string alias = "oh")
		{
			FillFilterDescriptions();
			selectCommand = ApplyUserFilters(selectCommand);

			selectCommand = String.Concat(selectCommand, String.Format(Environment.NewLine + "and ({1}.WriteTime > '{0}')",
				dtFrom.ToString(MySqlConsts.MySQLDateFormat), alias));
			selectCommand = String.Concat(selectCommand, String.Format(Environment.NewLine + "and ({1}.WriteTime < '{0}')",
				dtTo.ToString(MySqlConsts.MySQLDateFormat), alias));

			return selectCommand;
		}

		protected string ApplyUserFilters(string selectCommand)
		{
			foreach (var rf in selectedField) {
				if (rf.equalValues != null && rf.equalValues.Count > 0)
					selectCommand = String.Concat(selectCommand, Environment.NewLine + "and ", rf.GetEqualValues());
				if ((rf.nonEqualValues != null) && (rf.nonEqualValues.Count > 0))
					selectCommand = String.Concat(selectCommand, Environment.NewLine + "and ", rf.GetNonEqualValues());
			}
			return selectCommand;
		}

		protected void FillFilterDescriptions()
		{
			foreach (var field in selectedField) {
				if (field.nonEqualValues != null && field.nonEqualValues.Count > 0)
					FilterDescriptions.Add(String.Format("{0}: {1}", field.nonEqualValuesCaption, ReadNames(field, field.nonEqualValues)));
				if (field.equalValues != null && field.equalValues.Count > 0)
					FilterDescriptions.Add(String.Format("{0}: {1}", field.equalValuesCaption, GetValuesFromSQL(field.GetNamesSql(field.equalValues))));
			}
		}

		protected string ReadNames(FilterField field, List<ulong> ids)
		{
			return GetValuesFromSQL(field.GetNamesSql(ids));
		}

		protected string BuildSelect()
		{
			var selectCommand = "";
			if (SupportProductNameOptimization) {
				foreach (var rf in selectedField) // В целях оптимизации при некоторых случаях используем
					if (rf.visible && (rf.reportPropertyPreffix == "ProductName" || // временные таблицы
						rf.reportPropertyPreffix == "FullName")) {
						rf.primaryField = "ol.Productid";
						rf.viewField = "ol.Productid as pid";
						includeProductName = true;
						if (rf.reportPropertyPreffix == "FullName") {
							rf.primaryField = "p.CatalogId";
							rf.viewField = "p.CatalogId as pid";
							isProductName = false;
						}
					}

				if (includeProductName)
					selectCommand = @"
drop temporary table IF EXISTS MixedData;
create temporary table MixedData ENGINE=MEMORY
";
			}

			return selectCommand + selectedField.Where(rf => rf.visible).Aggregate("select ", (current, rf) => String.Concat(current, rf.primaryField, ", ", rf.viewField, ", "));
		}

		protected string ApplyGroupAndSort(string selectCommand, string sort)
		{
			if(selectedField.Any(f => f.visible))
				selectCommand = String.Concat(selectCommand, Environment.NewLine + "group by ", String.Join(",", (from rf in selectedField where rf.visible select rf.primaryField).ToArray()));
			selectCommand = String.Concat(selectCommand, Environment.NewLine + String.Format("order by {0}", sort));
			return selectCommand;
		}

		protected DataTable BuildResultTable(DataTable selectTable)
		{
			var res = new DataTable();
			foreach (var rf in selectedField.Where(f => f.visible)) {
				var dataColumn = selectTable.Columns[rf.outputField];
				if (dataColumn == null)
					throw new Exception(String.Format("Не удалось найти колонку {0}", rf.outputField));
				var dc = res.Columns.Add(rf.outputField, dataColumn.DataType);
				dc.Caption = rf.outputCaption;
				if (rf.width.HasValue)
					dc.ExtendedProperties.Add("Width", rf.width);
			}

			//Добавляем несколько пустых строк, чтобы потом вывести в них значение фильтра в Excel
			var emptyRowCount = EmptyRowCount;
			for (var i = 0; i < emptyRowCount; i++)
				res.Rows.InsertAt(res.NewRow(), 0);

			res = res.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
			return res;
		}

		public int EmptyRowCount
		{
			get { return FilterDescriptions.Count + (GroupHeaders.Count > 0 ? 1 : 0); }
		}

		protected void CopyData(DataTable source, DataTable destination)
		{
			int visbleCount = selectedField.Count(x => x.visible);
			destination.BeginLoadData();
			foreach (DataRow dr in source.Rows) {
				var newrow = destination.NewRow();

				foreach (var rf in selectedField)
					if (rf.visible)
						newrow[rf.outputField] = dr[rf.outputField];

				//Выставляем явно значения определенного типа для полей: "Сумма", "Доля рынка в %" и т.д.
				//(visbleCount * 2) - потому, что участвует код (первичный ключ) и строковое значение,
				//пример: PriceCode и PriceName.
				for (int i = (visbleCount * 2); i < source.Columns.Count; i++) {
					if (!(dr[source.Columns[i].ColumnName] is DBNull) && destination.Columns.Contains(source.Columns[i].ColumnName))
						newrow[source.Columns[i].ColumnName] = Convert.ChangeType(dr[source.Columns[i].ColumnName], destination.Columns[source.Columns[i].ColumnName].DataType);
				}

				destination.Rows.Add(newrow);
			}
			destination.EndLoadData();
		}

		protected string CalculateSupplierIds(ExecuteArgs e, int supplierId, bool showCode, bool showCodeCr)
		{
			if (!showCode && !showCodeCr)
				return "";

			var names = new [] { "ProductName", "FullName", "ShortName" };
			var productField = selectedField.FirstOrDefault(f => names.Contains(f.reportPropertyPreffix));
			if (productField == null) {
				productField = registredField.First(f => f.reportPropertyPreffix == "ProductName");
				selectedField.Insert(0, productField);
			}

			var producerField = selectedField.FirstOrDefault(f => f.reportPropertyPreffix == "FirmCr");
			if (producerField == null && showCodeCr) {
				producerField = registredField.First(f => f.reportPropertyPreffix == "FirmCr");
				selectedField.Insert(1, producerField);
			}

			ProfileHelper.Next("FillCodes");
			var groupExpression = productField.primaryField + (producerField != null ? ", " + String.Format("if (c.Pharmacie = 1, {0}, 0)", producerField.primaryField) : String.Empty);
			var selectExpression = productField.primaryField + (producerField != null ? ", " + String.Format("if (c.Pharmacie = 1, {0}, 0)", producerField.primaryField) : ", null ");

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
	and pd.IsLocal = 0
	and pd.FirmCode = ", OrdersSchema) + supplierId +
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
	pd.FirmCode = " + supplierId + @"
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
and exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pc1.PriceCode and prd.BaseCost=pc1.CostCode limit 1)
and pim1.Id = pc1.PriceItemId
and fr1.Id = pim1.FormRuleId
and (to_days(now())-to_days(pim1.PriceDate)) < fr1.MaxOld)
)) CoreCodes)
  join catalogs.products p on p.Id = CoreCodes.ProductId
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  left join catalogs.Producers cfc on CoreCodes.CodeFirmCr = cfc.Id
group by " +
				groupExpression;

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			return " left join ProviderCodes on ProviderCodes.CatalogCode = " + productField.primaryField +
				(producerField != null ? String.Format(" and ifnull(ProviderCodes.CodeFirmCr, 0) = if(c.Pharmacie = 1, ifnull({0}, 0), 0)", producerField.primaryField) : String.Empty);
		}
	}
}
