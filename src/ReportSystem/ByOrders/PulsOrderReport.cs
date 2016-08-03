using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using Common.Tools;
using NHibernate.Util;

namespace Inforoom.ReportSystem.ByOrders
{
	[Description("Индивидуальный отчет для Пульс")]
	public class PulsOrderReport : BaseOrdersReport
	{
		// (oh.`regioncode` & 2 > 0)

		[Description("Поставщик")]
		public int SupplierId { get; set; }

		protected uint? ParentSynonym;

		protected List<ulong> regions { get; set; }

		public PulsOrderReport()
		{
			HideHeader = true;
		}

		public PulsOrderReport(MySqlConnection conn, DataSet dsProperties)
			: base(conn, dsProperties)
		{
			HideHeader = true;
#if !DEBUG
			OrdersSchema = "OrdersOld";
			ParentSynonym = 4600;
#endif
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			if (_reportParams.ContainsKey("RegionEqual"))
			{
				regions = (List<ulong>)GetReportParam("RegionEqual");
				if (regions.Contains(0))
					regions.Clear(); // все регионы
			}
		}

		protected override void GenerateReport()
		{
			var selectCommand = String.Format(@"
DROP TEMPORARY TABLE IF EXISTS orders;
CREATE TEMPORARY TABLE orders
select CONCAT(p.Id, '_', IFNULL(pr.Id, 0)) AFCode,
CONCAT(c.Name, ' ', IFNULL(p.Properties, '')) name,
IFNULL(pr.Name, 'нет') prod,
SUM(IF(pd.firmcode = ?supplierId, ol.Cost * ol.Quantity, 0)) PulsSum,
SUM(IF(pd.firmcode = ?supplierId, ol.Quantity, 0)) PulsQuanity,
COUNT(DISTINCT IF(pd.firmcode = ?supplierId, oh.rowid, NULL)) PulsOrders,
SUM(IF(pd.FirmCode != ?supplierId, ol.Cost * ol.Quantity, 0)) OtherSum,
SUM(IF(pd.firmcode != ?supplierId, ol.Quantity, 0)) OtherQuanity,
COUNT(DISTINCT IF(pd.FirmCode != ?supplierId, oh.rowid, NULL)) OtherOrders,
COUNT(DISTINCT IF(pd.FirmCode != ?supplierId, pd.FirmCode, NULL)) OtherSup,
MIN(IF(pd.FirmCode != ?supplierId, ol.Cost, NULL)) MinOtherCost,
MAX(IF(pd.FirmCode != ?supplierId, ol.Cost, NULL)) MaxOtherCost,
ol.ProductId, ol.CodeFirmCr
from ({0}.OrdersHead oh, {0}.OrdersList ol, catalogs.Products p, catalogs.Catalog c, usersettings.PricesData pd)
left join catalogs.Producers pr on pr.Id = ol.CodeFirmCr
where oh.WriteTime > ?begin and oh.WriteTime < ?end
and ol.orderid = oh.RowID
and p.Id = ol.ProductId
and c.Id = p.CatalogId
and pd.PriceCode = oh.PriceCode ", OrdersSchema);

if (regions != null && regions.Any())
	selectCommand += "and oh.RegionCode in (?regions) ";

selectCommand += @"group by ol.ProductId, ol.CodeFirmCr
order by name;

select AFCode, IFNULL(GROUP_CONCAT(DISTINCT c.code), '') PulsCode, name, prod, ?begin minDate, ?end maxDate, PulsSum,
CAST(PulsQuanity as SIGNED INTEGER) as PulsQuanity, PulsOrders, OtherSum, CAST(OtherQuanity as SIGNED INTEGER) as OtherQuanity,
OtherOrders, o.OtherSup, MinOtherCost, MaxOtherCost
from (orders o, usersettings.PricesData pd)
left join farm.core0 c on c.PriceCode = pd.PriceCode and c.ProductId = o.ProductId and c.CodeFirmCr = o.CodeFirmCr
where pd.ParentSynonym <=> ?parentSynonym
group by o.ProductId, o.CodeFirmCr;
DROP TEMPORARY TABLE IF EXISTS orders;";

#if DEBUG
			Debug.WriteLine(selectCommand);
#endif

			var data = new System.Data.DataTable();
			DataAdapter.SelectCommand.CommandText = selectCommand;
			DataAdapter.SelectCommand.Parameters.Add(new MySqlParameter("begin", MySqlDbType.Date));
			DataAdapter.SelectCommand.Parameters["begin"].Value = Begin;
			DataAdapter.SelectCommand.Parameters.Add(new MySqlParameter("end", MySqlDbType.Date));
			DataAdapter.SelectCommand.Parameters["end"].Value = End;
			DataAdapter.SelectCommand.Parameters.Add(new MySqlParameter("parentSynonym", MySqlDbType.UInt32));
			DataAdapter.SelectCommand.Parameters["parentSynonym"].Value = ParentSynonym;
			DataAdapter.SelectCommand.Parameters.AddWithValue("supplierId", SupplierId);
			if (regions != null && regions.Any())
				DataAdapter.SelectCommand.Parameters.AddWithValue("regions", regions.Implode());
			DataAdapter.Fill(data);

			var captions = new Dictionary<string, string>();
			captions.Add("AFCode", "Код");
			captions.Add("PulsCode", "КодПульс");
			captions.Add("name", "Наименование");
			captions.Add("prod", "Производитель");
			captions.Add("minDate", "НачалоПериода");
			captions.Add("maxDate", "КонецПериода");
			captions.Add("PulsSum", "РубПульс");
			captions.Add("PulsQuanity", "УпакПульс");
			captions.Add("PulsOrders", "ЗаказовПульс");
			captions.Add("OtherSum", "РубПрочие");
			captions.Add("OtherQuanity", "УпакПрочие");
			captions.Add("OtherOrders", "ЗаказовПрочие");
			captions.Add("OtherSup", "ПоставщиковПрочие");
			captions.Add("MinOtherCost", "МинЦенаПрочие");
			captions.Add("MaxOtherCost", "МаксЦенаПрочие");

			foreach (DataColumn col in data.Columns)
				col.Caption = captions[col.ColumnName];

			data.TableName = "Results";
			var result = data.DefaultView.ToTable();
			_dsReport.Tables.Add(result);
		}

		protected override void PostProcessing(Application exApp, _Worksheet ws)
		{
			//Устанавливаем шрифт листа
			ws.Rows.Font.Size = 10;
			ws.Rows.Font.Name = "Calibri";
			ws.Activate();
		}
	}
}