using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using ExecuteTemplate;
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

			orgId = (int)getReportParam("OrgId");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			var sql = @"
select dh.WriteTime, db.Quantity, db.ProducerCost, db.SerialNumber, db.NDS, db.SupplierCostWithoutNDS, s.VendorId, d.DrugId, c.RegionCode
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
and db.SerialNumber is not null
and db.ProducerCost is not null
and db.SupplierCostWithoutNDS is not null
and s.VendorID is not null
";
			var adapter = new MySqlDataAdapter(sql, _conn);
			var parameters = adapter.SelectCommand.Parameters;
			parameters.AddWithValue("?begin", dtFrom);
			parameters.AddWithValue("?end", dtTo);
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

				var producerCost = Convert.ToDecimal(row["ProducerCost"]);
				var regionId = Convert.ToUInt64(row["RegionCode"]);
				var supplierCostWithoutNds = Convert.ToDecimal(row["SupplierCostWithoutNDS"]);
				var nds = row["NDS"] is DBNull ? 10 : Convert.ToDecimal(row["NDS"]);

				var currentMarkups = markups.Where(m => m.Region.Id == regionId)
					.Where(m => producerCost >= m.Begin && producerCost <= m.End)
					.ToList();

				var drugstoeMarkup = currentMarkups.FirstOrDefault(m => m.Type == MarkupType.Drugstore);
				if (drugstoeMarkup == null)
					continue;

				var retailCost = CalculateRetailCost(supplierCostWithoutNds,
					producerCost, nds, drugstoeMarkup.Value - 5);

				if (currentMarkups.All(m => m.Type != MarkupType.Supplier))
					continue;

				var maxCost = Markup.MaxCost(producerCost, nds, currentMarkups);
				if (retailCost > maxCost)
					continue;

				resultRow["DrugId"] = row["DrugId"];
				resultRow["Segment"] = 1;
				resultRow["Year"] = Convert.ToDateTime(row["WriteTime"]).Year;
				resultRow["Month"] = Convert.ToDateTime(row["WriteTime"]).Month;
				resultRow["Series"] = "\"" + row["SerialNumber"] + "\"";
				resultRow["TotDrugQn"] = row["Quantity"];
				resultRow["MnfPrice"] = Convert.ToDecimal(row["ProducerCost"]).ToString("0.00", CultureInfo.InvariantCulture);
				resultRow["PrcPrice"] = supplierCostWithoutNds.ToString("0.00", CultureInfo.InvariantCulture);
				resultRow["RtlPrice"] = retailCost.ToString("0.00", CultureInfo.InvariantCulture);
				resultRow["Funds"] = 0;
				resultRow["VendorID"] = row["VendorID"];
				result.Rows.Add(resultRow);
			}
		}

		public static decimal CalculateRetailCost(decimal supplierCostWithoutNds, decimal producerCost, decimal nds, decimal markup)
		{
			return Math.Round(supplierCostWithoutNds  + producerCost * markup / 100 * (1 + nds / 100), 2);
		}
	}
}