﻿using System;
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
		protected List<ulong> _RegionEqual;
		protected List<ulong> _RegionNonEqual;
		protected List<ulong> _PayerEqual;
		protected List<ulong> _PayerNonEqual;
		protected List<ulong> _Clients;
		protected List<ulong> _ClientsNON;
		protected int priceForCorel;

		protected string _clientsNames = "";
		protected string _suppliersNames = "";

		public PricesOfCompetitorsReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам конкурентов";
		}

		public override void ReadReportParams()
		{
			priceForCorel = (int)getReportParam("PriceCode");
			//_clients = (List<ulong>)getReportParam("Clients");
			if (_reportParams.ContainsKey("FirmCodeEqual"))
			_suppliers = (List<ulong>)getReportParam("FirmCodeEqual");
			if (_reportParams.ContainsKey("IgnoredSuppliers"))
			_suppliers = (List<ulong>)getReportParam("IgnoredSuppliers");

			_RegionEqual = new List<ulong>();
			_RegionNonEqual = new List<ulong>();
			_PayerEqual = new List<ulong>();
			_PayerNonEqual = new List<ulong>();
			_Clients = new List<ulong>();
			_ClientsNON = new List<ulong>();
			if (_reportParams.ContainsKey("RegionEqual"))
				_RegionEqual = (List<ulong>)getReportParam("RegionEqual");
			if (_reportParams.ContainsKey("RegionNonEqual"))
				_RegionNonEqual = (List<ulong>)getReportParam("RegionNonEqual");
			if (_reportParams.ContainsKey("PayerEqual"))
				_PayerEqual = (List<ulong>)getReportParam("PayerEqual");
			if (_reportParams.ContainsKey("PayerNonEqual"))
				_PayerNonEqual = (List<ulong>)getReportParam("PayerNonEqual");
			if (_reportParams.ContainsKey("Clients"))
				_Clients = (List<ulong>)getReportParam("Clients");
			if (_reportParams.ContainsKey("ClientsNON"))
				_ClientsNON = (List<ulong>)getReportParam("ClientsNON");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			_clients = GetClietnWithSetFilter(_RegionEqual, _RegionNonEqual,
				_PayerEqual, _PayerNonEqual, _Clients, _ClientsNON, e);
			foreach (var client in _clients)
			{
				_clientCode = Convert.ToInt32(client);
				base.GenerateReport(e);
				GetOffers(e);
				e.DataAdapter.SelectCommand.CommandText = "ALTER TABLE `usersettings`.`Core` ADD COLUMN `ClientID` INT(10) UNSIGNED AFTER `Id`;";
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
				e.DataAdapter.SelectCommand.CommandText = String.Format("update usersettings.Core R set R.ClientID={0};", client);
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
				//var smnSuppliers = ConcatWhereIn(_suppliers);
				e.DataAdapter.SelectCommand.CommandText =
					string.Format(
						@"
select p.CatalogId, C0.Code, cor.PriceCode, cor.ProductId, cor.Cost, cor.ClientID, concat(LOWER(cn.Name), '  ', cf.Form ) as ProductName
from usersettings.Core cor
	join catalogs.Products as p on p.id = cor.productid
	join Catalogs.Catalog as cg on p.catalogid = cg.id
	JOIN Catalogs.CatalogNames cn on cn.id = cg.nameid
	JOIN Catalogs.CatalogForms cf on cf.id = cg.formid
	Left JOIN farm.Core0 C0 on cor.productid = C0.productid and C0.PriceCode = {0}", priceForCorel);
				//,
	/*join usersettings.PricesData pd on pd.PriceCode = cor.PriceCode
	join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode";*/
 //where cd.FirmCode in " + smnSuppliers;
 //cd.FirmCode,
				e.DataAdapter.Fill(_dsReport, "CoreClient");
			}
			//var resultTable = _dsReport.Tables["CoreClient"].AsEnumerable().GroupBy(t => t["ProductId"]);
			var resultTable = _dsReport.Tables["CoreClient"].AsEnumerable().GroupBy(t => t["CatalogId"]);
			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("Code");
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
			var clientsName = string.Join(" ,", GetClientNames(_clients).ToArray());
			if (clientsName.Length > 2048)
				clientsName = clientsName.Substring(0, 2047);
			dtRes.Rows.Add("Клиенты:" + clientsName);
			dtRes.Rows.Add("Поставщики:" + string.Join(" ,", GetSupplierNames(_suppliers).ToArray()));
			dtRes.Rows.Add();
			foreach (var costRow in resultTable)
			{
				var newRow = dtRes.NewRow();
				newRow["Code"] = costRow.First()["Code"];
				newRow["MinCost"] = costRow.Min(p => p["Cost"]);
				newRow["ProductName"] = costRow.First()["ProductName"];
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
