using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using MySql.Data.MySqlClient;
using Common.Tools;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{
	public class PricesOfCompetitorsReport : ProviderReport
	{
		//protected bool _calculateByCatalog;

		protected string reportCaptionPreffix;
		protected List<ulong> _clients;
		protected List<ulong> _suppliers;

		protected string _clientsNames = "";
		protected string _suppliersNames = "";

		public PricesOfCompetitorsReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам конкурентов";
		}

		public override void ReadReportParams()
		{
			_clients = (List<ulong>)getReportParam("Clients");
			_suppliers = (List<ulong>)getReportParam("Suppliers");
			//_clientCode = (int)getReportParam("ClientCode");
			
			//_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			/*_clients = new List<int>();
 			_suppliers = new List<int>();
			_clients.Add(388);
			_clients.Add(384);
			_clients.Add(1107);
			_suppliers.Add(62);
			_suppliers.Add(82);
			_suppliers.Add(39);*/
			foreach (var client in _clients)
			{
				_clientCode = Convert.ToInt32(client);
				base.GenerateReport(e);
				GetOffers(e);
				var suppliers = new List<int>();
				var proceCode = new List<int>();
				e.DataAdapter.SelectCommand.CommandText = "ALTER TABLE `usersettings`.`Core` ADD COLUMN `ClientID` INT(10) UNSIGNED AFTER `Id`;";
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
				e.DataAdapter.SelectCommand.CommandText = String.Format("update usersettings.Core R set R.ClientID={0};", client);
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
				var smnSuppliers = ConcatWhereIn(_suppliers);
				/*var smnSuppliers = "(";
				foreach (var supplier in _suppliers)
				{
					smnSuppliers += (supplier + ", ");
				}
				smnSuppliers = smnSuppliers.Substring(0, smnSuppliers.Length - 2);
				smnSuppliers += ")";*/
				e.DataAdapter.SelectCommand.CommandText =
					@"
select cd.FirmCode, cor.PriceCode, cor.ProductId, cor.Cost, cor.ClientID, concat(LOWER(cn.Name), '  ', cf.Form) as ProductName
from usersettings.Core cor
	join catalogs.Products as p on p.id = cor.productid
	join Catalogs.Catalog as cg on p.catalogid = cg.id
	JOIN Catalogs.CatalogNames cn on cn.id = cg.nameid
	JOIN Catalogs.CatalogForms cf on cf.id = cg.formid

	join usersettings.PricesData pd on pd.PriceCode = cor.PriceCode
	join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
 where cd.FirmCode in " + smnSuppliers;

				e.DataAdapter.Fill(_dsReport, "CoreClient");
			}
			var resultTable = _dsReport.Tables["CoreClient"].AsEnumerable().GroupBy(t => t["ProductId"]);

			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("ProductName");
			dtRes.Columns.Add("MinCost", typeof (decimal));
			dtRes.Columns["ProductName"].Caption = "Название";
			dtRes.Columns["MinCost"].Caption = "Минимальная цена";
			var costNumber = new List<int>();
			for (double i = 0.01; i < 0.7; i += 0.1)
			{
				var okrugl = Math.Round((i*_clients.Count) + 1);
				if (okrugl > 1)
				{
					if (!costNumber.Contains((int)okrugl))
					{
						costNumber.Add((int)okrugl);
						dtRes.Columns.Add("Cost" + okrugl, typeof(decimal));
						dtRes.Columns["Cost" + okrugl].Caption = okrugl+"я цена";
					}
				}
				if (i == 0.01)
					i -= 0.01;
			}
			dtRes.Rows.Add("Отчет сформирован: " + DateTime.Now);
			dtRes.Rows.Add(string.Join(" ,", GetClientNames(_clients).ToArray()));
			dtRes.Rows.Add(string.Join(" ,", GetSupplierNames(_suppliers).ToArray()));
			dtRes.Rows.Add();
			foreach (var costRow in resultTable)
			{
				var newRow = dtRes.NewRow();
				newRow["MinCost"] = costRow.Min(p => p["Cost"]);
				newRow["ProductName"] = costRow.First()["ProductName"];
				//costRow.OrderBy(p => p["Cost"]);
				//var dfg = costRow.Select(f=>f["Cost"]).ToList();
				var groupCostRow = costRow.GroupBy(p => p["ClientID"]).Select(q => q.Min(u => u["Cost"])).ToList();
				groupCostRow.Sort();
				foreach (var i in costNumber)
				{
					if (groupCostRow.Count < i) break;
					newRow["Cost" + i] = groupCostRow[i - 1];
				}
				dtRes.Rows.Add(newRow);
			}
			_dsReport.Tables.Add(dtRes);
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.Excel)
				return new SupplierExcelWriter();
			return null;
		}

		protected override BaseReportSettings GetSettings()
		{
			return new BaseReportSettings(_reportCode, _reportCaption);
		}
	}
}
