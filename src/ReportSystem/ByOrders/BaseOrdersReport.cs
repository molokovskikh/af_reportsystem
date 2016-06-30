using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Common.MySql;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using Inforoom.ReportSystem.Filters;
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

	public enum ReportPeriod
	{
		[Description("За предыдущий месяц")] ByPrevMonth,
		[Description("За текущий день")] ByToday,
		[Description("Интервал отчета (дни) от текущей даты")] ByInterval,
	}


	public class BaseOrdersReport : BaseReport
	{
		public List<FilterField> RegistredField;
		public List<FilterField> selectedField;

		public DateTime Begin;
		public DateTime End;

		protected bool SupportProductNameOptimization;
		protected bool IncludeProductName; // есть ли параметр Позиция "Наименования продукта" в отчете
		protected bool isProductName = true;
		protected bool IncludeProducerName; // есть ли параметр "Позиция производителя"
		protected string OrdersSchema = "Orders";

		private string[] nameFields = new[] { "FullName", "ShortName", "ProductName" };

		public BaseOrdersReport()
		{
			Init();
		}

		public BaseOrdersReport(MySqlConnection conn, DataSet dsProperties)
			: base(conn, dsProperties)
		{
#if !DEBUG
			OrdersSchema = "OrdersOld";
#endif
			Init();
		}

		[Description("Период подготовки отчета")]
		public ReportPeriod ReportPeriod;

		[Description("Интервал отчета (дни) от текущей даты")]
		public int ReportInterval;

		private void Init()
		{
			selectedField = new List<FilterField>();
			RegistredField = new List<FilterField>();
			RegistredField.Add(new FilterField("p.Id", @"concat(cn.Name, ' ', cf.Form, ' ',
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

			RegistredField.Add(new FilterField("c.Id", "concat(cn.Name, ' ', cf.Form) as CatalogName", "CatalogName", "FullName",
				"Наименование и форма выпуска",
				"catalogs.catalog c, catalogs.catalognames cn, catalogs.catalogforms cf",
				0,
				"В отчет включены следующие наименования",
				"Следующие наименования исключены из отчета",
				40) {
					whereList = "and cn.id = c.NameId and cf.Id = c.FormId"
				});
			RegistredField.Add(new FilterField("cn.Id", "cn.Name as PosName", "PosName", "ShortName", "Наименование", "catalogs.catalognames cn", 0,
				"В отчет включены следующие наименования",
				"Следующие наименования исключены из отчета", 40));
			RegistredField.Add(new FilterField("m.Id", "m.Mnn", "Mnn", "Mnn", "МНН", "catalogs.mnn m",
				41,
				"В отчет включены следующие МНН",
				"Следующие МНН исключены из отчета") {
					Nullable = true,
					width = 40
				});
			RegistredField.Add(new FilterField("cfc.Id", "cfc.Name as FirmCr", "FirmCr", "FirmCr", "Производитель", "catalogs.Producers cfc", 1,
				"В отчет включены следующие производители",
				"Следующие производители исключены из отчета", 15));
			RegistredField.Add(new FilterField("rg.RegionCode", "rg.Region as RegionName", "RegionName", "Region", "Регион", "farm.regions rg", 2,
				"В отчет включены следующие регионы",
				"Следующие регионы исключены из отчета"));
			RegistredField.Add(new FilterField("prov.Id", "concat(prov.Name, ' - ', provrg.Region) as FirmShortName", "FirmShortName", "FirmCode", "Поставщик",
				"Customers.suppliers prov, farm.regions provrg",
				3,
				"В отчет включены следующие поставщики", "Следующие поставщики исключены из отчета",
				10) {
					whereList = "and prov.HomeRegion = provrg.RegionCode"
				});
			RegistredField.Add(new FilterField("pd.PriceCode", "concat(prov.Name , ' (', pd.PriceName, ') - ', provrg.Region) as PriceName", "PriceName", "PriceCode", "Прайс-лист",
				"usersettings.pricesdata pd, Customers.suppliers prov, farm.regions provrg",
				4,
				"В отчет включены следующие прайс-листы поставщиков",
				"Следующие прайс-листы поставщиков исключены из отчета",
				10) {
					whereList = "and prov.Id = pd.FirmCode and prov.HomeRegion = provrg.RegionCode",
				});
			RegistredField.Add(new FilterField("cl.Id", "cl.Name as ClientShortName", "ClientShortName", "ClientCode", "Аптека", "Customers.clients cl", 5,
				"В отчет включены следующие аптеки",
				"Следующие аптеки исключены из отчета",
				10));
			RegistredField.Add(new FilterField("payers.PayerId", "payers.ShortName as PayerName", "PayerName", "Payer", "Плательщик", "billing.payers", 6,
				"В отчет включены следующие плательщики",
				"Следующие плательщики исключены из отчета"));
			RegistredField.Add(new FilterField("ad.Id", "concat(ad.Address, ' (', cl.Name, ') ') as AddressName", "AddressName", "Addresses", "Адрес доставки",
				"customers.addresses ad, Customers.Clients cl", 7,
				"В отчет включены следующие адреса доставки",
				"Следующие адреса доставки исключены из отчета") {
					whereList = "and ad.ClientId = cl.Id"
				});
			RegistredField.Add(new FilterField {
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
			base.ReadReportParams();
			if (Interval) {
				ReportPeriod = ReportPeriod.ByInterval;
				Begin = From;
				End = To;
				End = End.Date.AddDays(1);
			} else if (ReportPeriod == ReportPeriod.ByPrevMonth) {
				End = DateTime.Today;
				End = End.AddDays(-(End.Day - 1)).Date; // Первое число текущего месяца
				Begin = End.AddMonths(-1).Date;
			} else if (ReportPeriod == ReportPeriod.ByToday) {
					Begin = DateTime.Today;
					End = DateTime.Now;
			} else {
				End = DateTime.Today;
				//От текущей даты вычитаем интервал - дата начала отчета
				Begin = End.AddDays(-ReportInterval).Date;
			}
			Header.Add($"Период дат: {Begin:dd.MM.yyyy HH:mm:ss} - {End:dd.MM.yyyy HH:mm:ss}");

			LoadFilters();
			CheckAfterLoadFields();
			SortFields();
			if (ReportPeriod == ReportPeriod.ByToday)
				OrdersSchema = "Orders";
		}

		protected void LoadFilters()
		{
			selectedField = RegistredField.Where(f => f.LoadFromDB(this)).ToList();
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
			IncludeProducerName = ReportParamExists("FirmCrPosition");

			var mnn = selectedField.FirstOrDefault(f => f.reportPropertyPreffix == "Mnn");
			var names = selectedField.Where(f => nameFields.Contains(f.reportPropertyPreffix));
			if (mnn != null && !names.Any()) {
				selectedField.Remove(mnn);
			}
		}

		protected override void GenerateReport()
		{
		}

		protected string ApplyFilters(string selectCommand, string alias = "oh")
		{
			FillFilterDescriptions();
			selectCommand = ApplyUserFilters(selectCommand);

			selectCommand = String.Concat(selectCommand, String.Format(Environment.NewLine + "and ({1}.WriteTime > '{0}')",
				Begin.ToString(MySqlConsts.MySQLDateFormat), alias));
			selectCommand = String.Concat(selectCommand, String.Format(Environment.NewLine + "and ({1}.WriteTime < '{0}')",
				End.ToString(MySqlConsts.MySQLDateFormat), alias));

			return selectCommand;
		}

		protected string GetFilterSql(string alias = "oh")
		{
			FillFilterDescriptions();

			var sql = "";
			foreach (var rf in selectedField) {
				if (rf.equalValues != null && rf.equalValues.Count > 0)
					sql += Environment.NewLine + "and " + rf.GetEqualValues();
				if ((rf.nonEqualValues != null) && (rf.nonEqualValues.Count > 0))
					sql += Environment.NewLine + "and " + rf.GetNonEqualValues();
			}

			return $@"{sql} and ({alias}.WriteTime > '{Begin.ToString(MySqlConsts.MySQLDateFormat)}')
and ({alias}.WriteTime < '{End.ToString(MySqlConsts.MySQLDateFormat)}') ";
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
					Header.Add(String.Format("{0}: {1}", field.nonEqualValuesCaption, ReadNames(field, field.nonEqualValues)));
				if (field.equalValues != null && field.equalValues.Count > 0)
					Header.Add(String.Format("{0}: {1}", field.equalValuesCaption, GetValuesFromSQL(field.GetNamesSql(field.equalValues))));
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
						IncludeProductName = true;
						if (rf.reportPropertyPreffix == "FullName") {
							rf.primaryField = "p.CatalogId";
							rf.viewField = "p.CatalogId as pid";
							isProductName = false;
						}
					}

				if (IncludeProductName)
					selectCommand = @"
drop temporary table IF EXISTS MixedData;
create temporary table MixedData ENGINE=MEMORY
";
			}

			return selectCommand + selectedField.Where(rf => rf.visible)
				.OrderBy(x => x.position)
				.Aggregate("select ", (current, rf) => String.Concat(current, rf.primaryField, ", ", rf.viewField, ", "));
		}

		protected string GetGroupSql()
		{
			if(selectedField.Any(f => f.visible))
				return "group by " + String.Join(",", (from rf in selectedField where rf.visible select rf.primaryField).ToArray());
			return "";
		}

		protected string ApplyGroupAndSort(string selectCommand, string sort)
		{
			if(selectedField.Any(f => f.visible))
				selectCommand = String.Concat(selectCommand, Environment.NewLine + "group by ", String.Join(",", (from rf in selectedField where rf.visible select rf.primaryField).ToArray()));
			selectCommand = String.Concat(selectCommand, Environment.NewLine + $"order by {sort}");
			return selectCommand;
		}

		protected DataTable BuildResultTable(DataTable selectTable)
		{
			var res = new DataTable();
			foreach (var rf in selectedField.Where(f => f.visible)) {
				var dataColumn = selectTable.Columns[rf.outputField];
				if (dataColumn == null)
					throw new Exception($"Не удалось найти колонку {rf.outputField}");
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
			get { return Header.Count + (GroupHeaders.Count > 0 ? 1 : 0); }
		}

		protected void CopyData(DataTable source, DataTable destination)
		{
			var visbleCount = selectedField.Count(x => x.visible);
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

		protected string CalculateSupplierIds(int supplierId, bool showCode, bool showCodeCr,
			CodeSource codeSource = CodeSource.OrdersAndPrices)
		{
			if (!showCode && !showCodeCr)
				return "";

			var names = new [] { "ProductName", "FullName", "ShortName" };
			var productField = selectedField.FirstOrDefault(f => names.Contains(f.reportPropertyPreffix));
			if (productField == null) {
				productField = RegistredField.First(f => f.reportPropertyPreffix == "ProductName");
				selectedField.Insert(0, productField);
			}

			var producerField = selectedField.FirstOrDefault(f => f.reportPropertyPreffix == "FirmCr");
			if (producerField == null && showCodeCr) {
				producerField = RegistredField.First(f => f.reportPropertyPreffix == "FirmCr");
				selectedField.Insert(1, producerField);
			}

			ProfileHelper.Next("FillCodes");
			var groupExpression = productField.primaryField + (producerField != null
				? $", if (c.Pharmacie = 1, {producerField.primaryField}, 0)"
				: String.Empty);
			var selectExpression = productField.primaryField + (producerField != null
				? $", if (c.Pharmacie = 1, {producerField.primaryField}, 0)"
				: ", null ");
			var priceCodesSql = @"
union

select
distinct " +
				(showCode ? "core.Code, " : String.Empty) +
				(showCodeCr ? "core.CodeCr, " : String.Empty) +
				$@"
	core.ProductId,
	core.CodeFirmCr
from
	usersettings.Pricesdata pd,
	farm.Core0 core
where
	pd.FirmCode = {supplierId}
	and core.PriceCode = pd.PriceCode
	and pd.Enabled = 1
	and exists (
		select
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
		and to_days(now())-to_days(pim1.PriceDate) < fr1.MaxOld
	)
";
			if (codeSource == CodeSource.Orders)
				priceCodesSql = "";

			DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS ProviderCodes;
create temporary table ProviderCodes (" +
				(showCode ? "Code varchar(255), " : String.Empty) +
				(showCodeCr ? "CodeCr varchar(255), " : String.Empty) +
				"CatalogCode int unsigned, codefirmcr int unsigned," +
				(showCode ? "key Code(Code), " : String.Empty) +
				(showCodeCr ? "key CodeCr(CodeCr), " : String.Empty) +
				@"key CatalogCode(CatalogCode), key CodeFirmCr(CodeFirmCr)) engine=MEMORY;
insert into ProviderCodes
select " +
				(showCode ? "group_concat(distinct CoreCodes.Code order by CoreCodes.Code), " : String.Empty) +
				(showCodeCr ? "CoreCodes.CodeCr, " : String.Empty) +
				selectExpression +
				@"
from (
	select
		distinct " +
					(showCode ? "ol.Code, " : String.Empty) +
					(showCodeCr ? "ol.CodeCr, " : String.Empty) +
					$@"
		ol.ProductId,
		ol.CodeFirmCr
	from {OrdersSchema}.OrdersHead oh,
		{OrdersSchema}.OrdersList ol,
		usersettings.pricesdata pd
	where
		ol.OrderID = oh.RowID
		and ol.Junk = 0
		and pd.PriceCode = oh.PriceCode
		and pd.IsLocal = 0
		and pd.FirmCode = {supplierId}
		and oh.WriteTime > '" + Begin.ToString(MySqlConsts.MySQLDateFormat) + @"'
		and oh.WriteTime < '" + End.ToString(MySqlConsts.MySQLDateFormat) + $@"'

	{priceCodesSql}
	) as CoreCodes
	join catalogs.products p on p.Id = CoreCodes.ProductId
	join catalogs.catalog c on c.Id = p.CatalogId
	join catalogs.catalognames cn on cn.id = c.NameId
	left join catalogs.Producers cfc on CoreCodes.CodeFirmCr = cfc.Id
group by {groupExpression}";

			ProfileHelper.WriteLine(DataAdapter.SelectCommand.CommandText);
			DataAdapter.SelectCommand.ExecuteNonQuery();
			return " left join ProviderCodes on ProviderCodes.CatalogCode = " + productField.primaryField +
				(producerField != null ? String.Format(" and ifnull(ProviderCodes.CodeFirmCr, 0) = if(c.Pharmacie = 1, ifnull({0}, 0), 0)", producerField.primaryField) : String.Empty);
		}

		protected void CheckSuppliersCount(string filter)
		{
			if (_reportParams.ContainsKey("FirmCodeEqual")) {
				var sql = String.Format(@"
select pd.FirmCode
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
	join billing.payers on payers.PayerId = le.PayerId
where
	pd.IsLocal = 0
	{1}", OrdersSchema, filter);
				ApplyFilters(sql);
				sql += " group by pd.FirmCode";
				var count = Connection.Read(sql).Count();
				if (count < 3) {
					throw new ReportException($"Фактическое количество прайс листов меньше трех, получено прайс-листов {count}");
				}
			}
		}
	}
}
