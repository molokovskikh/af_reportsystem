using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

using Inforoom.ReportSystem.Model;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;

namespace Inforoom.ReportSystem.ByOrders
{
	public class WaybillsReport : OrdersReport
	{
		private int orgId;

		public WaybillsReport()
		{
			DbfSupported = true;
		}

		public WaybillsReport(ulong reportCode, string reportCaption, MySqlConnection conn, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, conn, format, dsProperties)
		{
			DbfSupported = true;
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();

			orgId = (int)GetReportParam("OrgId");
		}

		protected override void GenerateReport()
		{
			var sql = @"
drop temporary table if exists uniq_document_lines;
create temporary table uniq_document_lines engine=memory
select max(db.Id) as Id
from Documents.DocumentBodies db
	join Documents.DocumentHeaders dh on dh.Id = db.DocumentId
		join Customers.Addresses a on a.Id = dh.AddressId
			join Customers.Clients c on c.Id = a.ClientId
		join Customers.Suppliers s on s.Id = dh.FirmCode
	join Reports.Drugs d on d.EAN = db.EAN13
where a.LegalEntityId = ?orgId
and dh.WriteTime > ?begin
and dh.WriteTime < ?end
and db.Quantity is not null
and db.ProducerCost is not null
and db.SupplierCost is not null
and s.VendorID is not null
group by db.EAN13
;

select db.Quantity, db.ProducerCost, db.SerialNumber, db.NDS, db.SupplierCost, s.VendorId, d.DrugId, c.RegionCode, d.MaxMnfPrice
from Documents.DocumentBodies db
	join Documents.DocumentHeaders dh on dh.Id = db.DocumentId
		join Customers.Addresses a on a.Id = dh.AddressId
			join Customers.Clients c on c.Id = a.ClientId
		join Customers.Suppliers s on s.Id = dh.FirmCode
	join uniq_document_lines u on u.Id = db.Id
	join Reports.Drugs d on d.EAN = db.EAN13
;
drop temporary table if exists uniq_document_lines;
";
			var adapter = new MySqlDataAdapter(sql, Connection);
			var parameters = adapter.SelectCommand.Parameters;
			parameters.AddWithValue("begin", Begin);
			parameters.AddWithValue("end", End);
			parameters.AddWithValue("orgId", orgId);
			var data = new DataTable();
			adapter.Fill(data);

			var result = new DataTable("Results");
			_dsReport.Tables.Add(result);
			result.Columns.Add("DrugID");
			result.Columns.Add("Segment");
			result.Columns.Add("Year");
			result.Columns.Add("Month");
			result.Columns.Add("Series");
			result.Columns.Add("TotDrugQn");
			result.Columns.Add("MnfPrice");
			result.Columns.Add("PrcPrice");
			result.Columns.Add("RtlPrice");
			result.Columns.Add("Funds");
			result.Columns.Add("VendorID");
			result.Columns.Add("Remark");
			result.Columns.Add("SrcOrg");

			var markups = Session.Query<Markup>().ToList();

			foreach (DataRow row in data.Rows) {
				var resultRow = result.NewRow();

				var producerCost = row["ProducerCost"] is DBNull ? 0 : Convert.ToDecimal(row["ProducerCost"]);
				var regionId = Convert.ToUInt64(row["RegionCode"]);
				var supplierCost = Convert.ToDecimal(row["SupplierCost"]);
				var nds = row["NDS"] is DBNull ? 10 : Convert.ToDecimal(row["NDS"]);

				var currentMarkups = markups.Where(m => m.Region.Id == regionId)
					.Where(m => producerCost >= m.Begin && producerCost <= m.End)
					.ToList();

				var retailCost = Markup.RetailCost(supplierCost, producerCost, nds, currentMarkups);
				if (retailCost == 0)
					continue;

				var producerCostForReport = Math.Round(producerCost * (1 + nds / 100), 2);

				decimal maxProducerCost;
				if (decimal.TryParse(row["MaxMnfPrice"].ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out maxProducerCost) && maxProducerCost > 0) {
					if (producerCost > maxProducerCost)
						continue;

					if (producerCost / producerCostForReport > 10)
						continue;
				}

				if ((producerCostForReport - supplierCost) / producerCostForReport > 0.25m)
					continue;

				resultRow["DrugId"] = row["DrugId"];
				resultRow["Segment"] = 1;
				resultRow["Year"] = DateTime.Now.Year;
				resultRow["Month"] = DateTime.Now.Month;
				resultRow["Series"] = "\"" + (row["SerialNumber"] is DBNull ? "-" : row["SerialNumber"]) + "\"";
				resultRow["TotDrugQn"] = Convert.ToDecimal(row["Quantity"]).ToString("0.00", CultureInfo.InvariantCulture);
				resultRow["MnfPrice"] = producerCostForReport.ToString("0.00", CultureInfo.InvariantCulture);
				resultRow["PrcPrice"] = supplierCost.ToString("0.00", CultureInfo.InvariantCulture);
				resultRow["RtlPrice"] = retailCost.ToString("0.00", CultureInfo.InvariantCulture);
				resultRow["Funds"] = 0.ToString("0.00", CultureInfo.InvariantCulture);
				resultRow["VendorID"] = row["VendorID"];
				result.Rows.Add(resultRow);
			}
		}
	}
}