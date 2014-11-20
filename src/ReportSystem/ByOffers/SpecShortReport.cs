using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Common.Models;
using Common.Tools;

using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using System.Data;
using NHibernate.Linq;
using MSExcel = Microsoft.Office.Interop.Excel;
using Offer = Inforoom.ReportSystem.Model.Offer;

namespace Inforoom.ReportSystem
{
	public class SpecShortReportData
	{
		public string Code { get; set; }
		public string CodeCr { get; set; }
		public string ProductName { get; set; }
		public string ProducerName { get; set; }

		public float MinCost { get; set; }

		public string AssortmentQuantity { get; set; }
		public float? AssortmentMinCost { get; set; }

		public string CodeWithoutProducer { get; set; }

		public SpecShortReportData(Offer offer)
		{
			Code = offer.AssortmentCode;
			CodeCr = offer.AssortmentCodeCr;
			ProductName = offer.ProductName;
			ProducerName = offer.ProducerName;
			MinCost = offer.Cost;
			AssortmentQuantity = offer.AssortmentQuantity;
			if (!String.IsNullOrEmpty(offer.CodeWithoutProducer)) {
				CodeWithoutProducer = offer.CodeWithoutProducer;
			}
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
		protected List<SpecShortReportData> _reportData;
		protected Hashtable _hash;

		protected List<ulong> _Clients;

		protected SpecShortReport() // конструктор для возможности тестирования
		{
		}

		public SpecShortReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
			reportCaptionPreffix = "Отчет по минимальным ценам";
			_reportData = new List<SpecShortReportData>();
			_hash = new Hashtable();
		}

		protected override void GenerateReport(ExecuteArgs e)
		{
			NewGeneratereport(e);

			_suppliers = GetShortSuppliers(e);
			_ignoredSuppliers = GetIgnoredSuppliers(e);

			if (_Clients.Count > 1)
				_clientsNames = GetClientsNamesFromSQL(_Clients);
		}

		public void NewGeneratereport(ExecuteArgs e)
		{
			ProfileHelper.Next("PreGetOffers");
			if (WithoutAssortmentPrice) {
				_priceCode = 0;
				SourcePC = 0;
				CustomerFirmName = String.Empty;
			}
			else {
				//Если прайс-лист равен 0, то он не установлен, поэтому берем прайс-лист относительно клиента, для которого делается отчет
				if (_priceCode == 0)
					throw new ReportException("Для специального отчета не указан параметр \"Прайс-лист\".");

				SourcePC = _priceCode;
				CustomerFirmName = GetSupplierName(_priceCode);

				//Проверка актуальности прайс-листа
				int ActualPrice = Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						@"
select distinct
  pc.PriceCode
from
  usersettings.pricescosts pc,
  usersettings.priceitems pim,
  farm.formrules fr
where
	pc.PriceCode = ?SourcePC
and exists(select * from userSettings.pricesregionaldata prd where prd.PriceCode = pc.PriceCode and prd.BaseCost=pc.CostCode limit 1)
and pim.Id = pc.PriceItemId
and fr.Id = pim.FormRuleId
and (to_days(now())-to_days(pim.PriceDate)) < fr.MaxOld",
						new MySqlParameter("?SourcePC", SourcePC)));
#if !DEBUG
				if (ActualPrice == 0)
					throw new ReportException(String.Format("Прайс-лист {0} ({1}) не является актуальным.", CustomerFirmName, SourcePC));
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
			var dtNewRes = new DataTable();
			dtNewRes.TableName = "Results";

			dtNewRes.Columns.Add("Code", typeof(string));
			dtNewRes.Columns.Add("CodeWithoutProducer", typeof(string));
			dtNewRes.Columns.Add("CodeCr", typeof(string));
			dtNewRes.Columns.Add("FullName", typeof(string));
			dtNewRes.Columns.Add("FirmCr", typeof(string));
			dtNewRes.Columns.Add("CustomerCost", typeof(decimal));
			dtNewRes.Columns.Add("CustomerQuantity", typeof(string));
			dtNewRes.Columns.Add("MinCost", typeof(decimal));
			dtNewRes.Columns.Add("LeaderName", typeof(string));

			dtNewRes.Columns["Code"].Caption = "Код";
			dtNewRes.Columns["CodeWithoutProducer"].Caption = "Код без изгот.";

			dtNewRes.Columns["CodeCr"].Caption = "Код производителя";
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
			foreach (var specShortReportData in sorted) {
				var newRow = dtNewRes.NewRow();
				newRow["Code"] = specShortReportData.Code;
				if (_codesWithoutProducer)
					newRow["CodeWithoutProducer"] = specShortReportData.CodeWithoutProducer;
				if (_showCodeCr)
					newRow["CodeCr"] = specShortReportData.CodeCr;

				newRow["FullName"] = specShortReportData.ProductName;
				newRow["FirmCr"] = specShortReportData.ProducerName;

				newRow["MinCost"] = Convert.ToDecimal(specShortReportData.MinCost);
				if (specShortReportData.AssortmentMinCost.HasValue) {
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

		protected void GetOffersByClient(int clientId)
		{
			ProfileHelper.Next("GetOffers for client: " + clientId);
			var client = Session.Get<Client>((uint)clientId);
			if (client == null)
				return;
			if (client.Enabled == false)
				return;
			var offers = GetOffers(clientId, Convert.ToUInt32(SourcePC), _SupplierNoise.HasValue ? (uint?)Convert.ToUInt32(_SupplierNoise.Value) : null, _reportIsFull, _calculateByCatalog, _reportType > 2);

			var assortmentMap = new Dictionary<uint, IGrouping<uint, Offer>>();
			if (_reportType > 2 && _codesWithoutProducer) {
				var assortmentGroups = offers.Where(o => o.AssortmentCoreId.HasValue).GroupBy(o => o.ProductId);
				foreach (var agroup in assortmentGroups) {
					assortmentMap[agroup.Key] = agroup;
				}
			}
			ProfileHelper.WriteLine("Offers count: " + offers.Count);
			ProfileHelper.Next("ProcessOffers for client: " + clientId);
			var groups = offers.GroupBy(o => GetKey(o));
			foreach (var @group in groups) {
				var ordered = group.OrderBy(o => o.Cost);
				var minOffer = ordered.First();

				if (_reportType > 2 && _codesWithoutProducer) {
					// отчет с учетом производителя и выбрана опция "Выставление кодов без учета изготовителя."
					// находим группу с выбранным productId
					//var assortmentGroup = assortmentGroups.Where(g => g.Key == minOffer.ProductId).FirstOrDefault();
					if (assortmentMap.ContainsKey(minOffer.ProductId)) {
						var assortmentGroup = assortmentMap[minOffer.ProductId];
						if (assortmentGroup != null) {
							IList<long> codes = new List<long>();
							foreach (var offer in assortmentGroup) {
								long val;
								if (long.TryParse(offer.AssortmentCode, out val))
									codes.Add(val);
							}
							if (codes.Count == assortmentGroup.Count() && codes.Count != 0) {
								codes = codes.OrderBy(c => c).ToList();
								minOffer.CodeWithoutProducer = codes.First().ToString();
							}
							else {
								var offer = assortmentGroup.FirstOrDefault();
								// берем первый (если коды преобразуются в числа - нужно брать мин. значение)
								if (offer != null) {
									var assortmentCode = offer.AssortmentCode;
									if (!String.IsNullOrEmpty(assortmentCode))
										minOffer.CodeWithoutProducer = assortmentCode;
								}
							}
						}
					}
				}

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
			else if (_reportType <= 2)
				return new { CatalogId = _calculateByCatalog ? offer.CatalogId : offer.ProductId, ProducerId = 0 };
			else
				return new { CatalogId = _calculateByCatalog ? offer.CatalogId : offer.ProductId, offer.ProducerId }; // с учетом производителя
		}

		private SpecShortReportData FindItem(Hashtable hash, Offer offer, List<SpecShortReportData> data)
		{
			var key = GetKey(offer);
			var item = (SpecShortReportData)hash[key];
			if (item == null) {
				item = new SpecShortReportData(offer);
				hash[key] = item;
				data.Add(item);
			}
			return item;
		}

		protected override void FormatLeaderAndPrices(MSExcel._Worksheet ws)
		{
		}

		public override void ReadReportParams()
		{
			if (_reportParams.ContainsKey("SupplierNoise"))
				_SupplierNoise = (int)GetReportParam("SupplierNoise");
			_reportType = (int)GetReportParam("ReportType");
			_calculateByCatalog = (bool)GetReportParam("CalculateByCatalog");
			_priceCode = (int)GetReportParam("PriceCode");
			_reportIsFull = (bool)GetReportParam("ReportIsFull");
			if (ReportParamExists("ShowCodeCr")) // показывать код изготовителя
				_showCodeCr = (bool)_reportParams["ShowCodeCr"];
			else
				_showCodeCr = false;

			_Clients = (List<ulong>)GetReportParam("Clients");
			//если не делать приведения nhibernate валится с ошибкой
			//System.NotSupportedException : Don't currently support idents of type UInt64
			var ids = _Clients.Select(l => (uint)l).ToArray();
			var clients = Session.Query<Client>().Where(c => ids.Contains(c.Id)).Where(c => c != null && c.Enabled).ToList();
			_Clients = clients.Select(c => (ulong)c.Id).ToList();
			if (_Clients.Count == 0)
				throw new ReportException("Не установлен параметр \"Список аптек\".");

			if (_reportParams.ContainsKey("WithoutAssortmentPrice"))
				WithoutAssortmentPrice = (bool)GetReportParam("WithoutAssortmentPrice");
			if (WithoutAssortmentPrice)
				_reportIsFull = true;

			if (_reportParams.ContainsKey("CodesWithoutProducer")) // Выставление кодов без учета изготовителя
				_codesWithoutProducer = (bool)GetReportParam("CodesWithoutProducer");
		}

		protected override void Calculate()
		{
			base.Calculate();
			DataTable dtNewRes;
			dtNewRes = _dsReport.Tables["Results"].DefaultView.ToTable("Results", false,
				new[] { "Code", "CodeCr", "FullName", "FirmCr", "CustomerCost", "CustomerQuantity", "MinCost", "LeaderName" });

			foreach (DataRow drRes in dtNewRes.Rows)
				if (!drRes["LeaderName"].Equals("+"))
					drRes["LeaderName"] = String.Empty;
			_dsReport.Tables.Remove("Results");
			_dsReport.Tables.Add(dtNewRes);
		}

		public override bool DbfSupported
		{
			get { return true; }
		}

		protected override void DataTableToDbf(DataTable dtExport, string fileName)
		{
			dtExport.Rows[0].Delete(); // обрезаем две первые строчки
			dtExport.Rows[0].Delete(); // ибо они пустые, ибо оставлены под шапку в Excel

			dtExport.Columns["Code"].ColumnName = "CODE";
			dtExport.Columns["CodeWithoutProducer"].ColumnName = "CODE2";
			dtExport.Columns["CodeCr"].ColumnName = "CODECR";
			dtExport.Columns["FullName"].ColumnName = "PRODUCT";
			dtExport.Columns["FirmCr"].ColumnName = "PRODUCER";
			dtExport.Columns["CustomerCost"].ColumnName = "PRICECOST";
			dtExport.Columns["CustomerQuantity"].ColumnName = "QUANTITY";
			dtExport.Columns["MinCost"].ColumnName = "MINCOST";
			dtExport.Columns["LeaderName"].ColumnName = "LEADER";

			if (!WithoutAssortmentPrice) {
				if ((_reportType != 2) && (_reportType != 4))
					dtExport.Columns.Remove("QUANTITY");
			}
			else {
				dtExport.Columns.Remove("LEADER");
				dtExport.Columns.Remove("QUANTITY");
				dtExport.Columns.Remove("PRICECOST");
				dtExport.Columns.Remove("CODE");
			}
			if (!_showCodeCr)
				dtExport.Columns.Remove("CODECR");
			if (!_codesWithoutProducer)
				dtExport.Columns.Remove("CODE2");
			base.DataTableToDbf(dtExport, fileName);
		}
	}
}