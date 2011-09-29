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
			reportCaptionPreffix = "����� �� ����������� �����";
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

			if (_Clients.Count > 1)
				_clientsNames = GetClientsNamesFromSQL(_Clients);
		}

		public string GetShortSuppliers(ExecuteArgs e)
		{
			var suppliers = new List<string>();

            e.DataAdapter.SelectCommand.CommandText = @"
select 
	concat(supps.Name, '(', group_concat(distinct pd.PriceName order by pd.PriceName separator ', '), ')')
from 
	usersettings.ActivePrices p
	join usersettings.PricesData pd on pd.PriceCode = p.PriceCode
	join future.suppliers supps on supps.Id = pd.FirmCode
group by supps.Id
order by supps.Name";
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
			if (WithoutAssortmentPrice)
			{
				_priceCode = 0;
				SourcePC = 0;
				CustomerFirmName = String.Empty;
			}
			else
			{
				//���� �����-���� ����� 0, �� �� �� ����������, ������� ����� �����-���� ������������ �������, ��� �������� �������� �����
				if (_priceCode == 0)
					throw new ReportException("��� ������������ ������ �� ������ �������� \"�����-����\".");
				
                DataRow drPrice = MySqlHelper.ExecuteDataRow(
                    ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
                    @"
select 
  concat(suppliers.Name, '(', pricesdata.PriceName, ') - ', regions.Region) as FirmName, 
  pricesdata.PriceCode, 
  suppliers.HomeRegion as RegionCode 
from 
  usersettings.pricesdata, 
  future.suppliers, 
  farm.regions 
where 
    pricesdata.PriceCode = ?PriceCode
and suppliers.Id = pricesdata.FirmCode
and regions.RegionCode = suppliers.HomeRegion
limit 1", new MySqlParameter("?PriceCode", _priceCode));

				if (drPrice == null)
					throw new ReportException(String.Format("�� ������ �����-���� � ����� {0}.", _priceCode));

				SourcePC = Convert.ToInt32(drPrice["PriceCode"]);
				CustomerFirmName = drPrice["FirmName"].ToString();

				//�������� ������������ �����-�����
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
#if !DEBUG
				if (ActualPrice == 0)
					throw new ReportException(String.Format("�����-���� {0} ({1}) �� �������� ����������.", CustomerFirmName, SourcePC));
#endif
			}

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

			dtNewRes.Columns.Add("Code", typeof(string));
			dtNewRes.Columns.Add("FullName", typeof(string));
			dtNewRes.Columns.Add("FirmCr", typeof(string));
			dtNewRes.Columns.Add("CustomerCost", typeof(decimal));
			dtNewRes.Columns.Add("CustomerQuantity", typeof(string));
			dtNewRes.Columns.Add("MinCost", typeof(decimal));
			dtNewRes.Columns.Add("LeaderName", typeof(string));

			dtNewRes.Columns["Code"].Caption = "���";
			dtNewRes.Columns["FullName"].Caption = "������������";
			dtNewRes.Columns["FirmCr"].Caption = "�������������";
			dtNewRes.Columns["CustomerCost"].Caption = CustomerFirmName;
			dtNewRes.Columns["CustomerQuantity"].Caption = "����������";
			dtNewRes.Columns["MinCost"].Caption = "���. ����";
			dtNewRes.Columns["LeaderName"].Caption = "�����";


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

				newRow["MinCost"] = Convert.ToDecimal(specShortReportData.MinCost);
				if (specShortReportData.AssortmentMinCost.HasValue)
				{
					newRow["CustomerQuantity"] = specShortReportData.AssortmentQuantity;
					newRow["CustomerCost"] = Convert.ToDecimal(specShortReportData.AssortmentMinCost);
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
			_Clients = (List<ulong>)getReportParam("Clients");
			if (_Clients.Count == 0)
				throw new ReportException("�� ���������� �������� \"������ �����\".");
			if (_reportParams.ContainsKey("WithoutAssortmentPrice"))
				WithoutAssortmentPrice = (bool)getReportParam("WithoutAssortmentPrice");
			if (WithoutAssortmentPrice)
				_reportIsFull = true;
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
			//����������� ��� ������� �� ������
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
			dtExport.Rows[0].Delete(); // �������� ��� ������ �������
			dtExport.Rows[0].Delete(); // ��� ��� ������, ��� ��������� ��� ����� � Excel

			dtExport.Columns[0].ColumnName = "CODE";
			dtExport.Columns[1].ColumnName = "PRODUCT";
			dtExport.Columns[2].ColumnName = "PRODUCER";
			dtExport.Columns[3].ColumnName = "PRICECOST";
			dtExport.Columns[4].ColumnName = "QUANTITY";
			dtExport.Columns[5].ColumnName = "MINCOST";
			dtExport.Columns[6].ColumnName = "LEADER";

			if (!WithoutAssortmentPrice)
			{
				if ((_reportType != 2) && (_reportType != 4))
					dtExport.Columns.Remove(dtExport.Columns[4]);
			}
			else
			{
				dtExport.Columns.Remove(dtExport.Columns[6].ColumnName);
				dtExport.Columns.Remove(dtExport.Columns[4].ColumnName);
				dtExport.Columns.Remove(dtExport.Columns[3].ColumnName);
				dtExport.Columns.Remove(dtExport.Columns[0].ColumnName);
			}

			base.DataTableToDbf(dtExport, fileName);
		}
	}

}
