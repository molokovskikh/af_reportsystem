﻿using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using System.Drawing;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Common.MySql;
using Inforoom.ReportSystem.Writers;
using Inforoom.ReportSystem.ReportSettings;

namespace Inforoom.ReportSystem
{
	//Смешанный для аптеки
	public class PharmacyMixedReport : MixedReport
	{
		public PharmacyMixedReport()
		{
			AddressesEqual = new List<ulong>();
			AddressRivals = new List<ulong>();
		}

		public PharmacyMixedReport(MySqlConnection Conn, DataSet dsProperties)
			: base(Conn, dsProperties)
		{
			AddressesEqual = new List<ulong>();
			AddressRivals = new List<ulong>();
		}

		public List<ulong> AddressRivals { get; set; }
		public List<ulong> AddressesEqual { get; set; }

		private ulong GetClientRegionMask()
		{
			DataAdapter.SelectCommand.CommandText = @"select OrderRegionMask from usersettings.RetClientsSet where ClientCode=" + SourceFirmCode;
			return Convert.ToUInt64(DataAdapter.SelectCommand.ExecuteScalar());
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();

			if (ReportParamExists("AddressRivals"))
				AddressRivals = (List<ulong>)GetReportParam("AddressRivals");
		}

		public string ReadAddress(List<ulong> ids)
		{
			var field = RegistredField.First(f => f.reportPropertyPreffix.Match("Addresses"));
			return ReadNames(field, ids);
		}

		protected override void GenerateReport()
		{
			ProfileHelper.Next("GenerateReport");
			var clientName = String.Format("Выбранная аптека : {0}", GetClientsNamesFromSQL(new List<ulong> { (ulong)SourceFirmCode }));
			Header.Add(clientName);
			var concurentClientNames = String.Format("Список аптек-конкурентов : {0}", GetClientsNamesFromSQL(concurrentGroups[0]));
			Header.Add(concurentClientNames);
			if (AddressRivals.Count > 0)
				Header.Add(String.Format("Список адресов доставки-конкурентов : {0}", ReadAddress(AddressRivals)));

			ProfileHelper.Next("GenerateReport2");

			var regionMask = GetClientRegionMask();
			var selectCommand = BuildSelect();

			var rivalFilter = String.Format("oh.ClientCode in ({0})", concurrentGroups[0].Implode());

			if (AddressRivals.Count > 0)
				rivalFilter += String.Format(" and oh.AddressId in ({0})", AddressRivals.Implode());

			if (IncludeProducerName)
				selectCommand = selectCommand.Replace("cfc.Id", "if(c.Pharmacie = 1, cfc.Id, 0) as cfc_id")
					.Replace("cfc.Name", "if(c.Pharmacie = 1, cfc.Name, 'Нелекарственный ассортимент')");

			var filter = " and (oh.RegionCode & " + regionMask + @") > 0 ";
			if (HideJunk) {
				filter = " and ol.Junk = 0 ";
				Header.Add("Из отчета исключены уцененные товары и товары с ограниченным сроком годности");
			}

			CheckSuppliersCount(filter);

			selectCommand = String.Concat(selectCommand, String.Format(@"
sum(if(oh.ClientCode = {0}, ol.cost*ol.quantity, NULL)) as SourceFirmCodeSum,
sum(if(oh.ClientCode = {0}, ol.quantity, NULL)) SourceFirmCodeRows,
Min(if(oh.ClientCode = {0}, ol.cost, NULL)) as SourceFirmCodeMinCost,
Avg(if(oh.ClientCode = {0}, ol.cost, NULL)) as SourceFirmCodeAvgCost,
Max(if(oh.ClientCode = {0}, ol.cost, NULL)) as SourceFirmCodeMaxCost,
Count(distinct if(oh.ClientCode = {0}, oh.RowId, NULL)) as SourceFirmDistinctOrderId,

sum(if({1}, ol.cost*ol.quantity, NULL)) as RivalsSum,
sum(if({1}, ol.quantity, NULL)) RivalsRows,
Min(if({1}, ol.cost, NULL)) as RivalsMinCost,
Avg(if({1}, ol.cost, NULL)) as RivalsAvgCost,
Max(if({1}, ol.cost, NULL)) as RivalsMaxCost,
Count(distinct if({1}, oh.RowId, NULL)) as RivalsDistinctOrderId,
Count(distinct if({1}, oh.AddressId, NULL)) as RivalsDistinctAddressId,

sum(ol.cost*ol.quantity) as AllSum,
sum(ol.quantity) AllRows,
Min(ol.cost) as AllMinCost,
Avg(ol.cost) as AllAvgCost,
Max(ol.cost) as AllMaxCost,
Count(distinct oh.RowId) as AllDistinctOrderId,
Count(distinct oh.AddressId) as AllDistinctAddressId ", SourceFirmCode, rivalFilter));
			selectCommand += String.Format(@"from {0}.OrdersHead oh
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
{1} ", OrdersSchema, filter);

			selectCommand = ApplyFilters(selectCommand);
			selectCommand = ApplyGroupAndSort(selectCommand, "AllSum desc");

			if (IncludeProducerName) {
				var groupPart = selectCommand.Substring(selectCommand.IndexOf("group by"));
				var newGroupPart = groupPart.Replace("cfc.Id", "cfc_id");
				selectCommand = selectCommand.Replace(groupPart, newGroupPart);
			}

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
#if DEBUG
			Debug.WriteLine(selectCommand);
#endif

			var selectTable = new DataTable();
			DataAdapter.SelectCommand.CommandText = selectCommand;
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.Fill(selectTable);

			ProfileHelper.Next("GenerateReport3");

			GroupHeaders.Add(new ColumnGroupHeader(clientName,
				"SourceFirmCodeSum",
				"SourceFirmDistinctOrderId"));
			GroupHeaders.Add(new ColumnGroupHeader(
				concurentClientNames,
				"RivalsSum",
				"RivalsDistinctAddressId"));
			GroupHeaders.Add(new ColumnGroupHeader(
				"Общие данные по рынку",
				"AllSum",
				"AllDistinctAddressId"));

			var result = BuildResultTable(selectTable);
			CustomizeResultTableColumns(result);
			CopyData(selectTable, result);

			ProfileHelper.Next("PostProcessing");
		}

		private void CustomizeResultTableColumns(DataTable res)
		{
			DataColumn dc;

			dc = res.Columns.Add("SourceFirmCodeSum", typeof(Decimal));
			dc.Caption = "Сумма по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("SourceFirmCodeRows", typeof(Int32));
			dc.Caption = "Кол-во по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("SourceFirmCodeMinCost", typeof(Decimal));
			dc.Caption = "Минимальная цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("SourceFirmCodeAvgCost", typeof(Decimal));
			dc.Caption = "Средняя цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("SourceFirmCodeMaxCost", typeof(Decimal));
			dc.Caption = "Максимальная цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("SourceFirmDistinctOrderId", typeof(Int32));
			dc.Caption = "Кол-во заявок препарата по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("RivalsSum", typeof(Decimal));
			dc.Caption = "Сумма по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("RivalsRows", typeof(Int32));
			dc.Caption = "Кол-во по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("RivalsMinCost", typeof(Decimal));
			dc.Caption = "Минимальная цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("RivalsAvgCost", typeof(Decimal));
			dc.Caption = "Средняя цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("RivalsMaxCost", typeof(Decimal));
			dc.Caption = "Максимальная цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("RivalsDistinctOrderId", typeof(Int32));
			dc.Caption = "Кол-во заявок препарата по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("RivalsDistinctAddressId", typeof(Int32));
			dc.Caption = "Кол-во адресов доставки, заказавших препарат, по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("AllSum", typeof(Decimal));
			dc.Caption = "Сумма по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("AllRows", typeof(Int32));
			dc.Caption = "Кол-во по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("AllMinCost", typeof(Decimal));
			dc.Caption = "Минимальная цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("AllAvgCost", typeof(Decimal));
			dc.Caption = "Средняя цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("AllMaxCost", typeof(Decimal));
			dc.Caption = "Максимальная цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)8);

			dc = res.Columns.Add("AllDistinctOrderId", typeof(Int32));
			dc.Caption = "Кол-во заявок препарата по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?)4);

			dc = res.Columns.Add("AllDistinctAddressId", typeof(Int32));
			dc.Caption = "Кол-во адресов доставки, заказавших препарат, по рынку";
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
			return new PharmacyMixedSettings(ReportCode, ReportCaption, Header, selectedField, GroupHeaders);
		}
	}
}