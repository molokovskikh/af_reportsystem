using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common.Tools;

using Inforoom.ReportSystem.Writers;
using MySql.Data.MySqlClient;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem
{
	public class LeakOffersReport : ProviderReport
	{
		public LeakOffersReport(ulong reportCode, string reportCaption, MySqlConnection connection, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, format, dsProperties)
		{
		}

		public DataSet DSResult
		{
			get { return _dsReport; }
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_clientCode = (int)GetReportParam("ClientCode");
		}

		protected override void GenerateReport()
		{
			GetOffers(_SupplierNoise);

			args.DataAdapter.SelectCommand.CommandText = @"
select c0.Code,
c0.CodeCr,
s.Synonym as Product,
sfc.Synonym as Producer,
c.Cost,
c0.Quantity,
c0.Period,
c0.Note,
c0.PriceCode,
if(cg.VitallyImportant, '+', '') as VitallyImportant,
if(cg.Pharmacie, '+', '') as Pharmacie
from usersettings.core c
join farm.core0 c0 on c0.Id = c.Id
join farm.SynonymArchive s on s.SynonymCode = c0.SynonymCode
join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c0.SynonymFirmCrCode
join catalogs.Products pr on c0.ProductId = pr.Id
join catalogs.Catalog cg on pr.CatalogId = cg.Id";
			var data = new DataSet();
			args.DataAdapter.Fill(data, "offers");

			args.DataAdapter.SelectCommand.CommandText = @"
select ap.PriceCode, supps.Name as ShortName, ap.PriceName, ap.PositionCount
from usersettings.activeprices ap
join Customers.suppliers supps on supps.Id = ap.FirmCode
order by ap.PositionCount desc";

			args.DataAdapter.Fill(_dsReport, "prices");

			var groupByPrice = data.Tables["offers"].Rows.Cast<DataRow>().GroupBy(r => r["PriceCode"]);
			groupByPrice = groupByPrice.OrderByDescending(p => {
				var priceId = Convert.ToInt32(p.Key);
				var rows = _dsReport.Tables["Prices"].Rows.Cast<DataRow>();
				return Convert.ToInt32(rows.First(r => Convert.ToInt32(r["PriceCode"]) == priceId)["PositionCount"]);
			});

			foreach (var price in groupByPrice) {
				var table = new DataTable(price.Key.ToString());
				data.Tables["Offers"].Columns
					.Cast<DataColumn>()
					.Where(c => c.ColumnName != "PriceCode")
					.Each(c => table.Columns.Add(c.ColumnName, c.DataType));
				foreach (var offer in price) {
					var row = table.NewRow();
					foreach (DataColumn column in table.Columns)
						row[column.ColumnName] = offer[column.ColumnName];

					table.Rows.Add(row);
				}
				_dsReport.Tables.Add(table);
			}
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.DBF)
				throw new ReportException("Не умею готовить отчет в dbf");

			return new LeakOffersNativeWriter();
		}
	}
}