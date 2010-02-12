using System.Data;
using MySql.Data.MySqlClient;
using System.Drawing;
using ReportSystem.Profiling;
using System.Diagnostics;
using Inforoom.ReportSystem.Filters;
using System;
using System.Collections.Generic;
using Inforoom.ReportSystem.Writers;
using Inforoom.ReportSystem.ReportSettings;
using ExecuteTemplate;

namespace Inforoom.ReportSystem
{
	public class PharmacyMixedReport : MixedReport
	{
		public PharmacyMixedReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties) 
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
		}

		private ulong GetClientRegionMask(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText =
@"select OrderRegionMask from usersettings.RetClientsSet where ClientCode=" + sourceFirmCode;
			return Convert.ToUInt64(e.DataAdapter.SelectCommand.ExecuteScalar());
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("GenerateReport");
			filter.Add(String.Format("Выбранная аптека : {0}", GetClientsNamesFromSQL(e, new List<ulong>{(ulong)sourceFirmCode})));
			filter.Add(String.Format("Список аптек-конкурентов : {0}", GetClientsNamesFromSQL(e, businessRivals)));

			ProfileHelper.Next("GenerateReport2");

			bool isProductName = true;
			bool includeProductName = false;
			ulong regionMask = GetClientRegionMask(e);

			foreach (var rf in selectedField) // В целях оптимизации при некоторых случаях используем
				if (rf.visible && (rf.reportPropertyPreffix == "ProductName" || // временные таблицы
					rf.reportPropertyPreffix == "FullName"))
				{
					rf.primaryField = "ol.Productid";
					rf.viewField = "ol.Productid as pid";
					includeProductName = true;
					if (rf.reportPropertyPreffix == "FullName")
					{
						rf.primaryField = "p.CatalogId";
						rf.viewField = "p.CatalogId as pid";
						isProductName = false;
					}
				}

			string SelectCommand = !includeProductName ? "select " :
				@"drop temporary table IF EXISTS MixedData;
				  create temporary table MixedData ENGINE=MEMORY select ";

			foreach (FilterField rf in selectedField)
				if (rf.visible)
					SelectCommand = String.Concat(SelectCommand, rf.primaryField, ", ", rf.viewField, ", ");

			SelectCommand = String.Concat(SelectCommand, String.Format(@"
sum(if(oh.ClientCode = {0}, ol.cost*ol.quantity, NULL)) as SourceFirmCodeSum,
sum(if(oh.ClientCode = {0}, ol.quantity, NULL)) SourceFirmCodeRows,
Min(if(oh.ClientCode = {0}, ol.cost, NULL)) as SourceFirmCodeMinCost,
Avg(if(oh.ClientCode = {0}, ol.cost, NULL)) as SourceFirmCodeAvgCost,
Max(if(oh.ClientCode = {0}, ol.cost, NULL)) as SourceFirmCodeMaxCost,
Count(distinct if(oh.ClientCode = {0}, oh.RowId, NULL)) as SourceFirmDistinctOrderId,

sum(if(oh.ClientCode in ({1}), ol.cost*ol.quantity, NULL)) as RivalsSum,
sum(if(oh.ClientCode in ({1}), ol.quantity, NULL)) RivalsRows,
Min(if(oh.ClientCode in ({1}), ol.cost, NULL)) as RivalsMinCost,
Avg(if(oh.ClientCode in ({1}), ol.cost, NULL)) as RivalsAvgCost,
Max(if(oh.ClientCode in ({1}), ol.cost, NULL)) as RivalsMaxCost,
Count(distinct if(oh.ClientCode in ({1}), oh.RowId, NULL)) as RivalsDistinctOrderId,
Count(distinct if(oh.ClientCode in ({1}), oh.ClientCode, NULL)) as RivalsDistinctClientCode,

sum(ol.cost*ol.quantity) as AllSum,
sum(ol.quantity) AllRows,
Min(ol.cost) as AllMinCost,
Avg(ol.cost) as AllAvgCost,
Max(ol.cost) as AllMaxCost,
Count(distinct oh.RowId) as AllDistinctOrderId,
Count(distinct oh.ClientCode) as AllDistinctClientCode ", sourceFirmCode, businessRivalsList));
			SelectCommand +=
@"from 
  orders.OrdersHead oh
  join orders.OrdersList ol on ol.OrderID = oh.RowID";
			if (!includeProductName || !isProductName)
				SelectCommand +=
		@"
  join catalogs.products p on p.Id = ol.ProductId";
			if (!includeProductName)
				SelectCommand +=
		@"
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId";
			SelectCommand +=
		@"
  join catalogs.Producers cfc on cfc.Id = if(ol.CodeFirmCr is not null, ol.CodeFirmCr, 1)
  left join usersettings.clientsdata cd on cd.FirmCode = oh.ClientCode
  left join future.Clients cl on cl.Id = oh.ClientCode
  join usersettings.retclientsset rcs on rcs.ClientCode = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
  join usersettings.clientsdata prov on prov.FirmCode = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.RegionCode
  join billing.payers on payers.PayerId = IFNULL(cl.PayerId, cd.BillingCode) " +
		@"
where 
    oh.deleted = 0
and oh.processed = 1
and ol.Junk = 0
and ol.Await = 0
and (oh.RegionCode & " + regionMask + ") > 0 ";

			SelectCommand +=
		@"
and payers.PayerId <> 921
and rcs.InvisibleOnFirm < 2";

			foreach (FilterField rf in selectedField)
			{
				if (rf.equalValues != null && rf.equalValues.Count > 0)
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetEqualValues());
					if (rf.reportPropertyPreffix == "ClientCode") // Список клиентов особенный т.к. выбирается из двух таблиц
						filter.Add(String.Format("{0}: {1}", rf.equalValuesCaption, GetClientsNamesFromSQL(e, rf.equalValues)));
					else
						filter.Add(String.Format("{0}: {1}", rf.equalValuesCaption, GetValuesFromSQL(e, rf.GetEqualValuesSQL())));
				}
				if ((rf.nonEqualValues != null) && (rf.nonEqualValues.Count > 0))
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetNonEqualValues());
					filter.Add(String.Format("{0}: {1}", rf.nonEqualValuesCaption, GetValuesFromSQL(e, rf.GetNonEqualValuesSQL())));
				}
			}

			SelectCommand = String.Concat(SelectCommand, String.Format(Environment.NewLine + "and Date(oh.WriteTime) >= Date('{0}')", dtFrom.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand, String.Format(Environment.NewLine + "and Date(oh.WriteTime) <= Date('{0}')", dtTo.ToString(MySQLDateFormat)));

			//Применяем группировку и сортировку
			List<string> GroupByList = new List<string>();
			foreach (FilterField rf in selectedField)
				if (rf.visible)
				{
					GroupByList.Add(rf.primaryField);
				}
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "group by ", String.Join(",", GroupByList.ToArray()));
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "order by AllSum desc");

			if (includeProductName)
				if (isProductName)
					SelectCommand += @"; select
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
					SelectCommand += @"; select
				(select c.name
				  from catalogs.catalog c
				  where
					c.Id = md.pid) CatalogName,
				  md.*
				from MixedData md";
#if DEBUG
			Debug.WriteLine(SelectCommand);
#endif

			DataTable SelectTable = new DataTable();

			e.DataAdapter.SelectCommand.CommandText = SelectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(SelectTable);

			ProfileHelper.Next("GenerateReport3");

			DataTable res = new DataTable();
			DataColumn dc;

			foreach (FilterField rf in selectedField)
			{
				if (rf.visible)
				{
					dc = res.Columns.Add(rf.outputField, SelectTable.Columns[rf.outputField].DataType);
					dc.Caption = rf.outputCaption;
					if (rf.width.HasValue)
						dc.ExtendedProperties.Add("Width", rf.width);
				}
			}

			CustomizeResultTableColumns(res);

			DataRow newrow;
			try
			{
				int visbleCount = selectedField.FindAll(delegate(FilterField x) { return x.visible; }).Count;
				res.BeginLoadData();
				foreach (DataRow dr in SelectTable.Rows)
				{
					newrow = res.NewRow();

					foreach (FilterField rf in selectedField)
						if (rf.visible)
							newrow[rf.outputField] = dr[rf.outputField];

					//Выставляем явно значения определенного типа для полей: "Сумма", "Доля рынка в %" и т.д.
					//(visbleCount * 2) - потому, что участвует код (первичный ключ) и строковое значение,
					//пример: PriceCode и PriceName.
					for (int i = (visbleCount * 2); i < SelectTable.Columns.Count; i++)
					{
						if (!(dr[SelectTable.Columns[i].ColumnName] is DBNull) && res.Columns.Contains(SelectTable.Columns[i].ColumnName))
							newrow[SelectTable.Columns[i].ColumnName] = Convert.ChangeType(dr[SelectTable.Columns[i].ColumnName], res.Columns[SelectTable.Columns[i].ColumnName].DataType);
					}

					res.Rows.Add(newrow);
				}
			}
			finally
			{
				res.EndLoadData();
			}

			//Добавляем несколько пустых строк, чтобы потом вывести в них значение фильтра в Excel
			for (int i = 0; i < filter.Count; i++)
				res.Rows.InsertAt(res.NewRow(), 0);

			res = res.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
			ProfileHelper.Next("PostProcessing");
		}

		private void CustomizeResultTableColumns(DataTable res)
		{
			DataColumn dc;

			dc = res.Columns.Add("SourceFirmCodeSum", typeof(System.Decimal));
			dc.Caption = "Сумма по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("SourceFirmCodeRows", typeof(System.Int32));
			dc.Caption = "Кол-во по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("SourceFirmCodeMinCost", typeof(System.Decimal));
			dc.Caption = "Минимальная цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("SourceFirmCodeAvgCost", typeof(System.Decimal));
			dc.Caption = "Средняя цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("SourceFirmCodeMaxCost", typeof(System.Decimal));
			dc.Caption = "Максимальная цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("SourceFirmDistinctOrderId", typeof(System.Int32));
			dc.Caption = "Кол-во заявок препарата по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("RivalsSum", typeof(System.Decimal));
			dc.Caption = "Сумма по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("RivalsRows", typeof(System.Int32));
			dc.Caption = "Кол-во по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("RivalsMinCost", typeof(System.Decimal));
			dc.Caption = "Минимальная цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("RivalsAvgCost", typeof(System.Decimal));
			dc.Caption = "Средняя цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("RivalsMaxCost", typeof(System.Decimal));
			dc.Caption = "Максимальная цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("RivalsDistinctOrderId", typeof(System.Int32));
			dc.Caption = "Кол-во заявок препарата по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("RivalsDistinctClientCode", typeof(System.Int32));
			dc.Caption = "Кол-во клиентов, заказавших препарат, по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("AllSum", typeof(System.Decimal));
			dc.Caption = "Сумма по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("AllRows", typeof(System.Int32));
			dc.Caption = "Кол-во по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("AllMinCost", typeof(System.Decimal));
			dc.Caption = "Минимальная цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("AllAvgCost", typeof(System.Decimal));
			dc.Caption = "Средняя цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("AllMaxCost", typeof(System.Decimal));
			dc.Caption = "Максимальная цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("AllDistinctOrderId", typeof(System.Int32));
			dc.Caption = "Кол-во заявок препарата по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("AllDistinctClientCode", typeof(System.Int32));
			dc.Caption = "Кол-во аптек, заказавших препарат, по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)4);
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format != ReportFormats.Excel)
				return null;

			return new PharmacyMixedOleWriter();
		}

		protected override BaseReportSettings GetSettings()
		{
			return new PharmacyMixedSettings(_reportCode, _reportCaption, filter, selectedField);
		}
	}
}
