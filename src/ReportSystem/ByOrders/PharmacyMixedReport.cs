﻿using System.Data;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using System.Drawing;
using System.Diagnostics;
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
		{}

		private ulong GetClientRegionMask(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = @"select OrderRegionMask from usersettings.RetClientsSet where ClientCode=" + sourceFirmCode;
			return Convert.ToUInt64(e.DataAdapter.SelectCommand.ExecuteScalar());
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("GenerateReport");
			filterDescriptions.Add(String.Format("Выбранная аптека : {0}", GetClientsNamesFromSQL(new List<ulong>{(ulong)sourceFirmCode})));
			filterDescriptions.Add(String.Format("Список аптек-конкурентов : {0}", GetClientsNamesFromSQL(businessRivals)));

			ProfileHelper.Next("GenerateReport2");

			var regionMask = GetClientRegionMask(e);
			var selectCommand = BuildSelect();

			selectCommand = String.Concat(selectCommand, String.Format(@"
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
			selectCommand +=
@"from 
  ordersold.OrdersHead oh
  join ordersold.OrdersList ol on ol.OrderID = oh.RowID";
			if (!includeProductName || !isProductName)
				selectCommand +=@"
  join catalogs.products p on p.Id = ol.ProductId";

			if (!includeProductName)
				selectCommand += @"
  join catalogs.catalog c on c.Id = p.CatalogId
  join catalogs.catalognames cn on cn.id = c.NameId
  join catalogs.catalogforms cf on cf.Id = c.FormId";

			selectCommand += @"
  left join catalogs.Producers cfc on cfc.Id = ol.CodeFirmCr
#  left join usersettings.clientsdata cd on cd.FirmCode = oh.ClientCode
  left join future.Clients cl on cl.Id = oh.ClientCode
  join farm.regions rg on rg.RegionCode = oh.RegionCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
#  join usersettings.clientsdata prov on prov.FirmCode = pd.FirmCode
  join future.suppliers prov on prov.Id = pd.FirmCode
  join farm.regions provrg on provrg.RegionCode = prov.HomeRegion
  join future.addresses adr on oh.AddressId = adr.Id
  join billing.LegalEntities le on adr.LegalEntityId = le.Id
  join billing.payers on payers.PayerId = le.PayerId 
where 
ol.Junk = 0
and ol.Await = 0
and (oh.RegionCode & " + regionMask + @") > 0";

			selectCommand = ApplyFilters(selectCommand);
			selectCommand = ApplyGroupAndSort(selectCommand, "AllSum desc");

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

			var result = BuildResultTable(selectTable);
			CustomizeResultTableColumns(result);
			CopyData(selectTable, result);

			ProfileHelper.Next("PostProcessing");
		}

		private void CustomizeResultTableColumns(DataTable res)
		{
			DataColumn dc;

			dc = res.Columns.Add("SourceFirmCodeSum", typeof (Decimal));
			dc.Caption = "Сумма по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("SourceFirmCodeRows", typeof (Int32));
			dc.Caption = "Кол-во по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 4);

			dc = res.Columns.Add("SourceFirmCodeMinCost", typeof (Decimal));
			dc.Caption = "Минимальная цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("SourceFirmCodeAvgCost", typeof (Decimal));
			dc.Caption = "Средняя цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("SourceFirmCodeMaxCost", typeof (Decimal));
			dc.Caption = "Максимальная цена для аптеки";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("SourceFirmDistinctOrderId", typeof (Int32));
			dc.Caption = "Кол-во заявок препарата по аптеке";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc.ExtendedProperties.Add("Width", (int?) 4);

			dc = res.Columns.Add("RivalsSum", typeof (Decimal));
			dc.Caption = "Сумма по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("RivalsRows", typeof (Int32));
			dc.Caption = "Кол-во по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 4);

			dc = res.Columns.Add("RivalsMinCost", typeof (Decimal));
			dc.Caption = "Минимальная цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("RivalsAvgCost", typeof (Decimal));
			dc.Caption = "Средняя цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("RivalsMaxCost", typeof (Decimal));
			dc.Caption = "Максимальная цена для конкурентов";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("RivalsDistinctOrderId", typeof (Int32));
			dc.Caption = "Кол-во заявок препарата по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 4);

			dc = res.Columns.Add("RivalsDistinctClientCode", typeof (Int32));
			dc.Caption = "Кол-во клиентов, заказавших препарат, по конкурентам";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc.ExtendedProperties.Add("Width", (int?) 4);

			dc = res.Columns.Add("AllSum", typeof (Decimal));
			dc.Caption = "Сумма по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("AllRows", typeof (Int32));
			dc.Caption = "Кол-во по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 4);

			dc = res.Columns.Add("AllMinCost", typeof (Decimal));
			dc.Caption = "Минимальная цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("AllAvgCost", typeof (Decimal));
			dc.Caption = "Средняя цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("AllMaxCost", typeof (Decimal));
			dc.Caption = "Максимальная цена по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 8);

			dc = res.Columns.Add("AllDistinctOrderId", typeof (Int32));
			dc.Caption = "Кол-во заявок препарата по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 4);

			dc = res.Columns.Add("AllDistinctClientCode", typeof (Int32));
			dc.Caption = "Кол-во аптек, заказавших препарат, по рынку";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc.ExtendedProperties.Add("Width", (int?) 4);
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format != ReportFormats.Excel)
				return null;

			return new PharmacyMixedOleWriter();
		}

		protected override BaseReportSettings GetSettings()
		{
			return new PharmacyMixedSettings(_reportCode, _reportCaption, filterDescriptions, selectedField);
		}
	}
}
