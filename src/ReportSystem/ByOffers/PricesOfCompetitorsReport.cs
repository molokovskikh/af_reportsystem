using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Common.MySql;
using Common.Tools;

using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem
{
	public class ProducerAwareReportData : ReportData
	{
		public string ProducerName { get; set; }

		public ProducerAwareReportData(DataRow offer) : base(offer)
		{
			ProducerName = offer.Field<string>("ProdName");
		}
	}

	public class ReportData
	{
		public ReportData(DataRow offer)
		{
			Code = offer.Field<string>("Code");
			CodeCr = offer.Field<string>("CodeCr");
			Name = offer.Field<string>("ProductName");
			Drugstore = new List<UInt32>();
			Costs = new List<decimal>();
		}

		public string Code { get; set; }
		public string Name { get; set; }
		public string CodeCr { get; set; }
		public List<UInt32> Drugstore { get; set; }
		public List<decimal> Costs { get; set; }
	}

	public class PricesOfCompetitorsReport : ProviderReport
	{
		protected string reportCaptionPreffix;
		protected string regionNotInprefix;
		protected List<ulong> _clients;
		protected List<ulong> _regions;
		protected List<ulong> _RegionEqual;
		protected List<ulong> _RegionNonEqual;
		protected List<ulong> _PayerEqual;
		protected List<ulong> _PayerNonEqual;
		protected List<ulong> _Clients;
		protected List<ulong> _ClientsNON;
		protected bool _ProducerAccount;
		protected bool _AllAssortment;
		protected bool _WithWithoutProperties;
		protected bool _showCodeCr;
		protected int priceForCorel;

		private string _groupingFieldText;

		protected string _clientsNames = "";
		protected string _suppliersNames = "";
		protected string _regionsWhere = string.Empty;

		public PricesOfCompetitorsReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам конкурентов";
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			priceForCorel = (int)GetReportParam("PriceCode");
			_ProducerAccount = (bool)GetReportParam("ProducerAccount");
			_AllAssortment = (bool)GetReportParam("AllAssortment");
			_WithWithoutProperties = (bool)GetReportParam("WithWithoutProperties");
			_showCodeCr = (bool)GetReportParam("ShowCodeCr");
			_RegionEqual = new List<ulong>();
			_RegionNonEqual = new List<ulong>();
			_PayerEqual = new List<ulong>();
			_PayerNonEqual = new List<ulong>();
			_Clients = new List<ulong>();
			_ClientsNON = new List<ulong>();
			if (_reportParams.ContainsKey("RegionEqual")) {
				_RegionEqual = (List<ulong>)GetReportParam("RegionEqual");
				regionNotInprefix = " IN ";
				_regions = _RegionEqual;
			}
			if (_reportParams.ContainsKey("RegionNonEqual")) {
				_RegionNonEqual = (List<ulong>)GetReportParam("RegionNonEqual");
				regionNotInprefix = " NOT IN ";
				_regions = _RegionNonEqual;
			}
			if (_reportParams.ContainsKey("PayerEqual"))
				_PayerEqual = (List<ulong>)GetReportParam("PayerEqual");
			if (_reportParams.ContainsKey("PayerNonEqual"))
				_PayerNonEqual = (List<ulong>)GetReportParam("PayerNonEqual");
			if (_reportParams.ContainsKey("Clients"))
				_Clients = (List<ulong>)GetReportParam("Clients");
			if (_reportParams.ContainsKey("ClientsNON"))
				_ClientsNON = (List<ulong>)GetReportParam("ClientsNON");

			_groupingFieldText = _WithWithoutProperties ? "CatalogId" : "ProductId";
			if (_regions != null)
				if (_regions.Count != 0) {
					_regionsWhere = String.Format(" where Prices.RegionCode in ({0})", _regions.Implode());
				}
		}

		protected override void GenerateReport()
		{
			ProfileHelper.Next("Начало формирования запроса");
			_clients = GetClientWithSetFilter(_RegionEqual, _RegionNonEqual,
				_PayerEqual, _PayerNonEqual, _Clients, _ClientsNON, null);

			var hash = new Hashtable();
			var data = new List<ReportData>();
			var clientsCount = _clients.Count;

			foreach (var client in _clients) {
				// проверка клиента на доступность
				var cl = GetClientWithSetFilter(_RegionEqual, _RegionNonEqual, _PayerEqual, _PayerNonEqual, _Clients, _ClientsNON, client);
				if (cl == null || cl.Count == 0) {
					clientsCount--;
					continue; // возможно, клиент был заблокирован во время подготовки отчета
				}
				_clientCode = Convert.ToInt32(client);
				InvokeGetActivePrices();
				//todo нужно ли для всех?
				CheckSupplierCount(String.Format("Для клиента {0} получено фактическое количество прайс листов меньше трех", client)
					+ ", получено прайс-листов {0}");
				var joinText = _AllAssortment ? "Left JOIN" : "JOIN";
				string withWithoutPropertiesText;
				if (_WithWithoutProperties)
					withWithoutPropertiesText = String.Format(@" if(C0.SynonymCode is not null, S.Synonym, {0}) ", GetCatalogProductNameSubquery("p.id"));
				else
					withWithoutPropertiesText = String.Format(@" if(C0.SynonymCode is not null, S.Synonym, {0}) ", QueryParts.GetFullFormSubquery("p.id", true));
				var firmcr = _ProducerAccount ? "and ifnull(C0.CodeFirmCr,0) = ifnull(c00.CodeFirmCr,0)" : string.Empty;

				var JunkWhere = _regionsWhere.Length == 0 ? " WHERE c00.Junk = 0 " : " AND c00.Junk = 0 ";
				args.DataAdapter.SelectCommand.CommandText =
					string.Format(
						@"
select c00.ProductId, p.CatalogId, c00.CodeFirmCr, c0.Code, c0.CodeCr,
{2} as ProductName,
if(c0.SynonymFirmCrCode is not null, Sf.Synonym , Prod.Name) as ProdName,

if(if(round(cc.Cost * Prices.Upcost, 2) < c00.MinBoundCost, c00.MinBoundCost, round(cc.Cost * Prices.Upcost, 2)) > c00.MaxBoundCost,
c00.MaxBoundCost, if(round(cc.Cost*Prices.UpCost,2) < c00.MinBoundCost, c00.MinBoundCost, round(cc.Cost * Prices.Upcost, 2))) as Cost,

Prices.FirmCode, Prices.PriceCode
from Usersettings.ActivePrices Prices
	join farm.core0 c00 on c00.PriceCode = Prices.PriceCode
		join farm.CoreCosts cc on cc.Core_Id = c00.Id and cc.PC_CostCode = Prices.CostCode
	{1} farm.Core0 c0 on c0.productid = c00.productid {5} and C0.PriceCode = {0}
	join catalogs.Products as p on p.id = c00.productid
	join Catalogs.Catalog as cg on p.catalogid = cg.id
	left join Catalogs.Producers Prod on c00.CodeFirmCr = Prod.Id
	left join farm.Synonym S on C0.SynonymCode = S.SynonymCode
	left join farm.SynonymFirmCr Sf on C0.SynonymFirmCrCode = Sf.SynonymFirmCrCode
	{3}
	{4}
", priceForCorel, joinText, withWithoutPropertiesText, _regionsWhere, JunkWhere, firmcr);

#if DEBUG
				Debug.WriteLine(args.DataAdapter.SelectCommand.CommandText);
#endif

				var offers = new DataTable();
				args.DataAdapter.Fill(offers);
				NoisingCostInDataTable(offers, "Cost", "FirmCode", _SupplierNoise);
				foreach (var group in Group(offers)) {
					var offer = group.First();
					var dataItem = FindItem(hash, offer, data);
					dataItem.Drugstore.AddRange(group.Select(r => r.Field<UInt32>("FirmCode")).Where(u => !dataItem.Drugstore.Contains(u)));
					dataItem.Costs.Add(group.Min(r => r.Field<decimal>("Cost")));
				}
#if DEBUG
				Console.WriteLine("Код клиента: " + _clientCode + " Строк в таблице: " + data.Count);
#endif
			}
			ProfileHelper.SpendedTime(string.Format("По {0}ти клиентам запрос выполнен за ", clientsCount));

			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("Code");
			if (_showCodeCr) {
				dtRes.Columns.Add("CodeCr");
				dtRes.Columns["CodeCr"].Caption = "Код изготовителя";
				dtRes.Columns["CodeCr"].ExtendedProperties["Width"] = 10;
			}
			dtRes.Columns.Add("ProductName");
			dtRes.Columns["ProductName"].Caption = "Наименование";
			dtRes.Columns["ProductName"].ExtendedProperties["Width"] = 65;
			if (_ProducerAccount) {
				dtRes.Columns.Add("CodeFirmCr");
				dtRes.Columns["CodeFirmCr"].Caption = "Производитель";
				dtRes.Columns["CodeFirmCr"].ExtendedProperties["Width"] = 25;
			}
			dtRes.Columns.Add("MinCost", typeof(decimal));
			dtRes.Columns["Code"].Caption = "Код товара";
			dtRes.Columns["MinCost"].Caption = "Минимальная цена";
			var costNumber = new List<int>();
			for (double i = 0.01; i < 0.7; i += 0.1) {
				var okrugl = Math.Round((i * clientsCount) + 1);
				if (okrugl > 1) {
					if (!costNumber.Contains((int)okrugl)) {
						costNumber.Add((int)okrugl);
						dtRes.Columns.Add("Cost" + okrugl, typeof(decimal));
						dtRes.Columns["Cost" + okrugl].Caption = (100 - i * 100) + "% (" + okrugl + "я цена)";
					}
				}
				if (i == 0.01)
					i -= 0.01;
			}
			dtRes.Columns.Add("SupplierCount", typeof(int));
			dtRes.Columns.Add("DrugstoreCount", typeof(int));
			dtRes.Columns["SupplierCount"].Caption = "Количество поставщиков";
			dtRes.Columns["SupplierCount"].ExtendedProperties["Width"] = 10;
			dtRes.Columns["DrugstoreCount"].Caption = "Количество аптек";
			dtRes.Columns["DrugstoreCount"].ExtendedProperties["Width"] = 10;
			if (_ProducerAccount)
				data = data.OrderBy(i => i.Name).ThenBy(i => ((ProducerAwareReportData)i).ProducerName).ToList();
			else
				data = data.OrderBy(i => i.Name).ToList();

			foreach (var dataItem in data) {
				var newRow = dtRes.NewRow();
				if (_ProducerAccount)
					newRow["CodeFirmCr"] = ((ProducerAwareReportData)dataItem).ProducerName;
				if (_showCodeCr)
					newRow["CodeCr"] = dataItem.CodeCr;
				newRow["Code"] = dataItem.Code;
				newRow["MinCost"] = dataItem.Costs.Min();
				newRow["ProductName"] = dataItem.Name;
				newRow["DrugstoreCount"] = dataItem.Costs.Count;
				newRow["SupplierCount"] = dataItem.Drugstore.Count;
				dataItem.Costs.Sort();
				foreach (var i in costNumber) {
					if (dataItem.Costs.Count < i)
						break;
					newRow["Cost" + i] = dataItem.Costs[i - 1];
				}
				dtRes.Rows.Add(newRow);
			}
			_dsReport.Tables.Add(dtRes);
		}

		private ReportData FindItem(Hashtable hash, DataRow offer, List<ReportData> data)
		{
			var key = GetKey(offer);
			var item = (ReportData)hash[key];
			if (item == null) {
				if (_ProducerAccount)
					item = new ProducerAwareReportData(offer);
				else
					item = new ReportData(offer);
				hash[key] = item;
				data.Add(item);
			}
			return item;
		}

		private IEnumerable<IGrouping<object, DataRow>> Group(DataTable table)
		{
			return table.AsEnumerable().GroupBy(r => GetKey(r));
		}

		private object GetKey(DataRow row)
		{
			//Дебильная группировка по кодам в прайсе
			//задача состоит в том что если у поставщика в прайсе две позиции с одним и тем же ProductId но разным кодом они и здесь должны
			if (row["Code"] is DBNull) {
				if (!_ProducerAccount)
					return row[_groupingFieldText];
				else
					return new { CatalogId = row.Field<uint>(_groupingFieldText), CodeFirmCr = row.Field<uint?>("CodeFirmCr") };
			}
			else if (_showCodeCr)
				return new { Code = row["Code"], CodeCr = row["CodeCr"] };
			else
				return row["Code"];
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.Excel)
				return new PricesOfCompetitorsWriter(_reportParams, args, ReportCaption);
			return null;
		}

		protected override BaseReportSettings GetSettings()
		{
			return new BaseReportSettings(ReportCode, ReportCaption);
		}
	}
}