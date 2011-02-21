using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Common.Tools;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using MySql.Data.MySqlClient;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem
{

	public class SpecShortReportData
	{
		public string Code { get; set; }
		public string ProductName { get; set; }
		public string ProducerName { get; set; }

		public float MinCost { get; set; }

		public string AssortmentQuantity { get; set; }
		public float? AssortmentMinCost { get; set; }

		public SpecShortReportData(Offer offer)
		{
			Code = offer.AssortmentCode;
			ProductName = offer.ProductName;
			ProducerName = offer.ProducerName;
			MinCost = offer.Cost;
			AssortmentQuantity = offer.AssortmentQuantity;
		}

		public void UpdateMinCost(Offer offer)
		{
			if (offer.Cost < MinCost)
				MinCost = offer.Cost;
		}

		public void AssortmentUpdateMinCost(Offer offer)
		{
			if (offer.AssortmentCost.HasValue)
				if (!AssortmentMinCost.HasValue || offer.AssortmentCost < AssortmentMinCost)
					AssortmentMinCost = offer.AssortmentCost;
		}

		public bool IsLeader()
		{

			if (AssortmentMinCost.HasValue)
				return AssortmentMinCost.Value < MinCost || Math.Abs(AssortmentMinCost.Value - MinCost) < 0.001;
			return false;
		}
	}

	public class SpecShortReport : SpecReport
	{
		private List<SpecShortReportData> _reportData;
		private Hashtable _hash;

		protected List<ulong> _Clients;


		public SpecShortReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам";
			_reportData = new List<SpecShortReportData>();
			_hash = new Hashtable();
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			//base.GenerateReport(e);

			//_suppliers = GetSuppliers(e);
			//_ignoredSuppliers = GetIgnoredSuppliers(e);

			NewGeneratereport(e);

			_suppliers = GetShortSuppliers(e);
			_ignoredSuppliers = GetIgnoredSuppliers(e);
		}

		public string GetShortSuppliers(ExecuteArgs e)
		{
			var suppliers = new List<string>();
			e.DataAdapter.SelectCommand.CommandText = @"
select 
	concat(cd.ShortName, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from 
	usersettings.ActivePrices p
	join usersettings.PricesData pd on pd.PriceCode = p.PriceCode
	join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
group by cd.FirmCode
order by cd.ShortName";
			using (var reader = e.DataAdapter.SelectCommand.ExecuteReader())
			{
				while (reader.Read())
					suppliers.Add(Convert.ToString(reader[0]));
			}
			return suppliers.Distinct().Implode();
		}


		public void NewGeneratereport(ExecuteArgs e)
		{
			ProfileHelper.Next("PreGetOffers");
			//Если прайс-лист равен 0, то он не установлен, поэтому берем прайс-лист относительно клиента, для которого делается отчет
			if (_priceCode == 0)
				throw new ReportException("Для специального отчета не указан параметр \"Прайс-лист\".");

			DataRow drPrice = MySqlHelper.ExecuteDataRow(
				ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
				@"
select 
  concat(clientsdata.ShortName, '(', pricesdata.PriceName, ') - ', regions.Region) as FirmName, 
  pricesdata.PriceCode, 
  clientsdata.RegionCode 
from 
  usersettings.pricesdata, 
  usersettings.clientsdata, 
  farm.regions 
where 
    pricesdata.PriceCode = ?PriceCode
and clientsdata.FirmCode = pricesdata.FirmCode
and regions.RegionCode = clientsdata.RegionCode
limit 1", new MySqlParameter("?PriceCode", _priceCode));

			if (drPrice == null)
				throw new ReportException(String.Format("Не найден прайс-лист с кодом {0}.", _priceCode));

			SourcePC = Convert.ToInt32(drPrice["PriceCode"]);
			CustomerFirmName = drPrice["FirmName"].ToString();

			//Проверка актуальности прайс-листа
			int ActualPrice = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					e.DataAdapter.SelectCommand.Connection,
					@"
select 
  pc.PriceCode 
from 
  usersettings.pricescosts pc,
  usersettings.priceitems pim,
  farm.formrules fr 
where 
    pc.PriceCode = ?SourcePC
and pc.BaseCost = 1
and pim.Id = pc.PriceItemId
and fr.Id = pim.FormRuleId
and (to_days(now())-to_days(pim.PriceDate)) < fr.MaxOld",
					new MySqlParameter("?SourcePC", SourcePC)));
			if (ActualPrice == 0)
				throw new ReportException(String.Format("Прайс-лист {0} ({1}) не является актуальным.", CustomerFirmName, SourcePC));

			foreach (var client in _Clients)
				GetOffersByClient(Convert.ToInt32(client));

			ProfileHelper.Next("Calculate");
			GetResultTable();

			ProfileHelper.End();
		}

		private void GetResultTable()
		{
			DataTable dtNewRes = new DataTable();
			dtNewRes.TableName = "Results";
			dtNewRes.Columns.Add("Code", typeof (string));
			dtNewRes.Columns.Add("FullName", typeof(string));
			dtNewRes.Columns.Add("FirmCr", typeof(string));
			dtNewRes.Columns.Add("CustomerCost", typeof(float));
			dtNewRes.Columns.Add("CustomerQuantity", typeof(string));
			dtNewRes.Columns.Add("MinCost", typeof(float));
			dtNewRes.Columns.Add("LeaderName", typeof(string));

			dtNewRes.Columns["Code"].Caption = "Код";
			dtNewRes.Columns["FullName"].Caption = "Наименование";
			dtNewRes.Columns["FirmCr"].Caption = "Производитель";
			dtNewRes.Columns["CustomerCost"].Caption = CustomerFirmName;
			dtNewRes.Columns["CustomerQuantity"].Caption = "Количество";
			dtNewRes.Columns["MinCost"].Caption = "Мин. цена";
			dtNewRes.Columns["LeaderName"].Caption = "Лидер";


			var emptyRow = dtNewRes.NewRow();
			dtNewRes.Rows.Add(emptyRow);
			emptyRow = dtNewRes.NewRow();
			dtNewRes.Rows.Add(emptyRow);

			var sorted = _reportData.OrderBy(r => r.ProductName);
			foreach (var specShortReportData in sorted)
			{
				var newRow = dtNewRes.NewRow();
				newRow["Code"] = specShortReportData.Code;
				newRow["FullName"] = specShortReportData.ProductName;
				newRow["FirmCr"] = specShortReportData.ProducerName;

				newRow["MinCost"] = specShortReportData.MinCost;
				if (specShortReportData.AssortmentMinCost.HasValue)
				{
					newRow["CustomerQuantity"] = specShortReportData.AssortmentQuantity;
					newRow["CustomerCost"] = specShortReportData.AssortmentMinCost;
					if (specShortReportData.IsLeader())
						newRow["LeaderName"] = "+";
				}

				dtNewRes.Rows.Add(newRow);
			}

			if (_dsReport.Tables.Contains("Results"))
				_dsReport.Tables.Remove("Results");
			_dsReport.Tables.Add(dtNewRes);
		}

		private void GetOffersByClient(int clientId)
		{
			ProfileHelper.Next("GetOffers for client: " + clientId);
			var offers = GetOffers(clientId, Convert.ToUInt32(SourcePC), _SupplierNoise.HasValue ? (uint?)Convert.ToUInt32(_SupplierNoise.Value) : null, _reportIsFull, _calculateByCatalog, _reportType > 2);
			ProfileHelper.WriteLine("Offers count: " + offers.Count);
			ProfileHelper.Next("ProcessOffers for client: " + clientId);
			var groups = offers.GroupBy(o => GetKey(o));
			foreach (var @group in groups)
			{
				var ordered = group.OrderBy(o => o.Cost);
				var minOffer = ordered.First();
				var item = FindItem(_hash, minOffer, _reportData);
				item.UpdateMinCost(minOffer);

				var orderedByAssortment = group.OrderBy(o => o.AssortmentCost);
				item.AssortmentUpdateMinCost(orderedByAssortment.First());
			}
		}

		private object GetKey(Offer offer)
		{
			if (offer.AssortmentCoreId.HasValue)
				return offer.AssortmentCoreId;
			else
			{
				if (_reportType <= 2)
					return new { CatalogId = _calculateByCatalog ? offer.CatalogId : offer.ProductId, ProducerId = 0};
				else
					return new { CatalogId = _calculateByCatalog ? offer.CatalogId : offer.ProductId, offer.ProducerId};
			}
		}

		private SpecShortReportData FindItem(Hashtable hash, Offer offer, List<SpecShortReportData> data)
		{
			var key = GetKey(offer);
			var item = (SpecShortReportData)hash[key];
			if (item == null)
			{
				item = new SpecShortReportData(offer);
				hash[key] = item;
				data.Add(item);
			}
			return item;
		}

		public override void ReadReportParams()
		{
			if (_reportParams.ContainsKey("SupplierNoise"))
				_SupplierNoise = (int)getReportParam("SupplierNoise");
			_reportType = (int)getReportParam("ReportType");
			_calculateByCatalog = (bool)getReportParam("CalculateByCatalog");
			_priceCode = (int)getReportParam("PriceCode");
			_reportIsFull = (bool)getReportParam("ReportIsFull");
			if (_reportParams.ContainsKey("Clients"))
				_Clients = (List<ulong>)getReportParam("Clients");
		}

		protected override void Calculate()
		{
			base.Calculate();
			DataTable dtNewRes = _dsReport.Tables["Results"].DefaultView.ToTable("Results", false,
				new[] { "Code", "FullName", "FirmCr", "CustomerCost", "CustomerQuantity", "MinCost", "LeaderName" });
			foreach (DataRow drRes in dtNewRes.Rows)
				if (!drRes["LeaderName"].Equals("+"))
					drRes["LeaderName"] = String.Empty;
			_dsReport.Tables.Remove("Results");
			_dsReport.Tables.Add(dtNewRes);
		}

		protected override void FormatLeaderAndPrices(MSExcel._Worksheet ws)
		{
			//Выравниваем все колонки по ширине
			//ws.Columns.AutoFit();
			//((MSExcel.Range)ws.Columns[1, _dsReport.Tables["Results"].Columns.Count]).AutoFit();
		}

		public override bool DbfSupported
		{
			get
			{
				return true;
			}
		}

		protected override void DataTableToDbf(DataTable dtExport, string fileName)
		{
			dtExport.Rows[0].Delete(); // обрезаем две первые строчки
			dtExport.Rows[0].Delete(); // ибо они пустые, ибо оставлены под шапку в Excel

			dtExport.Columns[0].ColumnName = "CODE";
			dtExport.Columns[1].ColumnName = "PRODUCT";
			dtExport.Columns[2].ColumnName = "PRODUCER";
			dtExport.Columns[3].ColumnName = "PRICECOST";
			dtExport.Columns[4].ColumnName = "QUANTITY";
			dtExport.Columns[5].ColumnName = "MINCOST";
			dtExport.Columns[6].ColumnName = "LEADER";

			if ((_reportType != 2) && (_reportType != 4))
				dtExport.Columns.Remove(dtExport.Columns[4]);

			base.DataTableToDbf(dtExport, fileName);
		}
	}

}
