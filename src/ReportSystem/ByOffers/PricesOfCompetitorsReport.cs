using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using ExecuteTemplate;
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
			Name = offer.Field<string>("ProductName");
			//DrugstoreCount = offer.Field<List<string>>("FirmCode");
			Drugstore = new List<UInt32>();
			Costs = new List<decimal>();
		}

		public string Code { get; set; }
		public string Name { get; set; }
		public List<UInt32 > Drugstore { get; set; }
		public List<decimal> Costs { get; set; }
	}

	public class PricesOfCompetitorsReport : ProviderReport
	{
		protected string reportCaptionPreffix;
		protected string regionNotInprefix;
		protected List<ulong> _clients;
		protected List<ulong> _regions;
		protected List<ulong> _suppliers;
		protected List<ulong> _RegionEqual;
		protected List<ulong> _RegionNonEqual;
		protected List<ulong> _PayerEqual;
		protected List<ulong> _PayerNonEqual;
		protected List<ulong> _Clients;
		protected List<ulong> _ClientsNON;
		protected bool _ProducerAccount;
		protected bool _AllAssortment;
		protected bool _WithWithoutProperties;
		protected int priceForCorel;

		private string _groupingFieldText;

		protected string _clientsNames = "";
		protected string _suppliersNames = "";
		protected string _regionsWhere = string.Empty;

		public ExecuteArgs ex;

		public PricesOfCompetitorsReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам конкурентов";
		}

		public override void ReadReportParams()
		{
			priceForCorel = (int)getReportParam("PriceCode");
			_ProducerAccount = (bool) getReportParam("ProducerAccount");
			_AllAssortment = (bool)getReportParam("AllAssortment");
			_WithWithoutProperties = (bool)getReportParam("WithWithoutProperties");
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
			{
				_RegionEqual = (List<ulong>) getReportParam("RegionEqual");
				regionNotInprefix = " IN ";
				_regions = _RegionEqual;
			}
			if (_reportParams.ContainsKey("RegionNonEqual"))
			{
				_RegionNonEqual = (List<ulong>) getReportParam("RegionNonEqual");
				regionNotInprefix = " NOT IN ";
				_regions = _RegionNonEqual;
			}
			if (_reportParams.ContainsKey("PayerEqual"))
				_PayerEqual = (List<ulong>)getReportParam("PayerEqual");
			if (_reportParams.ContainsKey("PayerNonEqual"))
				_PayerNonEqual = (List<ulong>)getReportParam("PayerNonEqual");
			if (_reportParams.ContainsKey("Clients"))
				_Clients = (List<ulong>)getReportParam("Clients");
			if (_reportParams.ContainsKey("ClientsNON"))
				_ClientsNON = (List<ulong>)getReportParam("ClientsNON");

			_groupingFieldText = _WithWithoutProperties ? "CatalogId" : "ProductId";
			if (_regions != null)
			if (_regions.Count !=0)
			{
				_regionsWhere = " where Prices.RegionCode in " + ConcatWhereIn(_regions);
			}
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			ProfileHelper.Next("Начало формирования запроса");
			ex = e;
			_clients = GetClietnWithSetFilter(_RegionEqual, _RegionNonEqual,
				_PayerEqual, _PayerNonEqual, _Clients, _ClientsNON, e);

			var hash = new Hashtable();
			var data = new List<ReportData>();
			Console.WriteLine("всего клиентов {0}", _clients.Count);
			foreach (var client in _clients)
			{
				_clientCode = Convert.ToInt32(client);
				base.GenerateReport(e);
				GetActivePrices(e);
				//GetOffers(e);
				var joinText = _AllAssortment ? "Left JOIN" : "JOIN";
				string withWithoutPropertiesText;
				if (!_WithWithoutProperties)
					withWithoutPropertiesText =
						@"if(C0.SynonymCode is not null, S.Synonym, concat(cn.Name , ' ' ,cf.Form, ' ',
	 cast(GROUP_CONCAT(ifnull(PV.Value, '')
						order by PR.PropertyName, PV.Value
						SEPARATOR ', '
					   ) as char)))";
				else
				{
					withWithoutPropertiesText = @" if(C0.SynonymCode is not null, S.Synonym, concat(cn.Name, ' ', cf.Form)) ";
				}
				var JunkWhere = _regionsWhere.Length == 0 ? " WHERE c00.Junk = 0 " : " AND c00.Junk = 0 ";
				e.DataAdapter.SelectCommand.CommandText =
					string.Format(
						@"
select p.CatalogId, Prices.FirmCode,cg.Brand, c0.Code, if(c0.SynonymFirmCrCode is not null, Sf.Synonym , Prod.Name) as ProdName,

if(if(round(cc.Cost * Prices.Upcost, 2) < c00.MinBoundCost, c00.MinBoundCost, round(cc.Cost * Prices.Upcost, 2)) > c00.MaxBoundCost,
c00.MaxBoundCost, if(round(cc.Cost*Prices.UpCost,2) < c00.MinBoundCost, c00.MinBoundCost, round(cc.Cost * Prices.Upcost, 2))) as Cost, 

c00.CodeFirmCr, Prices.PriceCode, c00.ProductId, {2} as ProductName
from Usersettings.ActivePrices Prices
	join farm.core0 c00 on c00.PriceCode = Prices.PriceCode
	{1} farm.Core0 c0 on c0.productid = c00.productid and ifnull(C0.CodeFirmCr,0) = ifnull(c00.CodeFirmCr,0) and C0.PriceCode = {0}
	join catalogs.Products as p on p.id = c00.productid
	join Catalogs.Catalog as cg on p.catalogid = cg.id
	JOIN Catalogs.CatalogNames cn on cn.id = cg.nameid
	JOIN Catalogs.CatalogForms cf on cf.id = cg.formid
	join Catalogs.Producers Prod on c00.CodeFirmCr = Prod.Id
	JOIN farm.CoreCosts cc on cc.Core_Id = c00.Id and cc.PC_CostCode = Prices.CostCode
	left join farm.Synonym S on C0.SynonymCode = S.SynonymCode
	left join farm.SynonymFirmCr Sf on C0.SynonymFirmCrCode = Sf.SynonymFirmCrCode
	 
	 left join catalogs.ProductProperties PP on PP.ProductId = c00.productid
	 left join catalogs.PropertyValues PV on PV.Id = PP.PropertyValueId
	 left join catalogs.Properties PR on PR.Id = PV.PropertyId
	 {3} 
	 {4}
	 group by c00.id", priceForCorel, joinText, withWithoutPropertiesText, _regionsWhere, JunkWhere);

				var offers = new DataTable();
				e.DataAdapter.Fill(offers);
				foreach(var group in Group(offers))
				{
					//var offer = group.First();
					var offer = group.Where(r => r["ProdName"] != null).First();
					var dataItem = FindItem(hash, offer, data);
					dataItem.Drugstore.AddRange(group.Select(r => r.Field<UInt32>("FirmCode")).Where(u => !dataItem.Drugstore.Contains(u)));
					//dataItem.DrugstoreCount++;
					dataItem.Costs.Add(group.Min(r => r.Field<decimal>("Cost")));
				}
#if DEBUG
				Console.WriteLine("Код клиента: "+ _clientCode + " Строк в таблице: " + data.Count);
#endif
			}
			ProfileHelper.SpendedTime(string.Format("По {0}ти клиентам запрос выполнен за ", _clients.Count));

			var dtRes = new DataTable("Results");
			dtRes.Columns.Add("Code");
			dtRes.Columns.Add("ProductName");
			if (_ProducerAccount)
			{
				dtRes.Columns.Add("CodeFirmCr");
				dtRes.Columns["CodeFirmCr"].Caption = "Производитель";
				dtRes.Columns["CodeFirmCr"].ExtendedProperties["Width"] = 25;
			}
			dtRes.Columns.Add("MinCost", typeof (decimal));
			dtRes.Columns["Code"].Caption = "Код товара";
			dtRes.Columns["ProductName"].Caption = "Наименование";
			dtRes.Columns["ProductName"].ExtendedProperties["Width"] = 65;
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
						dtRes.Columns["Cost" + okrugl].Caption = (100 - i*100) + "% (" + okrugl + "я цена)";
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

			foreach (var dataItem in data)
			{
				var newRow = dtRes.NewRow();
				if (_ProducerAccount)
					newRow["CodeFirmCr"] = ((ProducerAwareReportData)dataItem).ProducerName;
				newRow["Code"] = dataItem.Code;
				newRow["MinCost"] = dataItem.Costs.Min();
				newRow["ProductName"] = dataItem.Name;
				newRow["DrugstoreCount"] = dataItem.Costs.Count;
				newRow["SupplierCount"] = dataItem.Drugstore.Count;
				dataItem.Costs.Sort();
				foreach (var i in costNumber)
				{
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
			if (item == null)
			{
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
			if (!_ProducerAccount)
				return row[_groupingFieldText];
			else
				//return new { CatalogId = row.Field<uint>(_groupingFieldText), CodeFirmCr = row.Field<uint?>("CodeFirmCr") };
				if (row.Field<byte>("Brand") == 1)
					return row[_groupingFieldText];
				else
				{
					return new {CatalogId = row.Field<uint>(_groupingFieldText), CodeFirmCr = row.Field<uint?>("CodeFirmCr")};
				}
		}


		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.Excel)
				return new PricesOfCompetitorsWriter(_reportParams, ex);
			return null;
		}

		protected override BaseReportSettings GetSettings()
		{
			return new BaseReportSettings(_reportCode, _reportCaption);
		}
	}
}
