using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common.Tools;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem
{
	public class PriceCollectionForClientReport : ProviderReport
	{
		protected List<ulong> _Clients; // список клиентов
		protected int _supplierId; // поставщик

		private List<ReportData> _reportData;

		public PriceCollectionForClientReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, ReportFormats format, DataSet dsProperties)
			: base(ReportCode, ReportCaption, Conn, format, dsProperties)
		{
			_reportData = new List<ReportData>();
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_supplierId = (int)getReportParam("FirmCode");
			_Clients = (List<ulong>)getReportParam("Clients");
			if (_Clients.Count == 0)
				throw new ReportException("Не установлен параметр \"Список аптек\".");
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			foreach (var client in _Clients) {
				_clientCode = Convert.ToInt32(client);
				ProfileHelper.Next("GetOffers for client: " + _clientCode);
				GetOffers(); // получили предложения для клиента

				string clientName = Convert.ToString(
					MySqlHelper.ExecuteScalar(
						e.DataAdapter.SelectCommand.Connection,
						@"select FullName from Customers.Clients where Id = ?ClientCode",
						new MySqlParameter("?ClientCode", _clientCode)));

				var prices = new List<uint>(); // прайсы, для которых будем брать синонимы
				e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select ifnull(pd.ParentSynonym, pd.PriceCode) from usersettings.pricesdata pd where pd.FirmCode = {0};", _supplierId);
				using (var reader = e.DataAdapter.SelectCommand.ExecuteReader()) {
					while (reader.Read())
						prices.Add(Convert.ToUInt32(reader[0]));
				}

				e.DataAdapter.SelectCommand.CommandText = string.Format(@"
SELECT
	AP.PriceDate,
	if(s.SynonymCode is not null, s.Synonym, OrigSyn.Synonym) ProductName,
	if(sfcr.SynonymFirmCrCode is not null, sfcr.Synonym, OrigSynCr.Synonym) ProducerName,
	supps.Name SupplierName,
	r.Region RegionName,
	Core.Cost,
	'{0}' ClientName,
	ifnull(cc.RequestRatio, c0.RequestRatio) as RequestRatio,
	ifnull(cc.MinOrderSum, c0.OrderCost) as OrderCost,
	ifnull(cc.MinOrderCount, c0.MinOrderCount) as MinOrderCount
FROM
	usersettings.Core
	join farm.core0 c0 on Core.id = c0.id
	join usersettings.ActivePrices AP on AP.PriceCode = Core.PriceCode
		join Farm.CoreCosts cc on cc.Core_Id = c0.Id and cc.PC_CostCode = ap.CostCode
	join Customers.Suppliers supps on AP.FirmCode = supps.Id
	join farm.Regions r on Core.RegionCode = r.RegionCode
	left join farm.Synonym OrigSyn on c0.SynonymCode = OrigSyn.SynonymCode
	left join farm.SynonymFirmCr OrigSynCr on c0.SynonymFirmCrCode = OrigSynCr.SynonymFirmCrCode
	left join farm.Synonym S on Core.productid = s.productId and s.PriceCode in ({1})
	left join farm.SynonymFirmCr sfcr on c0.CodeFirmCr = sfcr.CodeFirmCr and sfcr.PriceCode in ({1})
group by Core.Id;",
					clientName,
					prices.Distinct().Implode());
#if DEBUG
				Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
				using (var reader = args.DataAdapter.SelectCommand.ExecuteReader()) {
					foreach (var row in reader.Cast<IDataRecord>()) {
						var data = new ReportData(row);
						_reportData.Add(data); // результат
					}
				}
			}
			ProfileHelper.Next("Calculate");
			GetResultTable();
			ProfileHelper.End();
		}

		private void GetResultTable()
		{
			// формируем DataTable с результатами
			DataTable dtNewRes = new DataTable();
			dtNewRes.TableName = "Results";
			dtNewRes.Columns.Add("PriceDate", typeof(string));
			dtNewRes.Columns.Add("ProductName", typeof(string));
			dtNewRes.Columns.Add("ProducerName", typeof(string));
			dtNewRes.Columns.Add("SupplierName", typeof(string));
			dtNewRes.Columns.Add("RegionName", typeof(string));
			dtNewRes.Columns.Add("Cost", typeof(decimal));
			dtNewRes.Columns.Add("ClientName", typeof(string));
			dtNewRes.Columns.Add("RequestRatio", typeof(int));
			dtNewRes.Columns.Add("OrderCost", typeof(decimal));
			dtNewRes.Columns.Add("MinOrderCount", typeof(int));

			dtNewRes.Columns["PriceDate"].Caption = "Дата прайса";
			dtNewRes.Columns["ProductName"].Caption = "Товар";
			dtNewRes.Columns["ProducerName"].Caption = "Производитель";
			dtNewRes.Columns["SupplierName"].Caption = "Поставщик";
			dtNewRes.Columns["RegionName"].Caption = "Регион";
			dtNewRes.Columns["Cost"].Caption = "Цена";
			dtNewRes.Columns["ClientName"].Caption = "Клиент";
			dtNewRes.Columns["RequestRatio"].Caption = "Кратность";
			dtNewRes.Columns["OrderCost"].Caption = "Мин. сумма";
			dtNewRes.Columns["MinOrderCount"].Caption = "Мин. кол-во";

			foreach (var offer in _reportData) {
				var newRow = dtNewRes.NewRow();
				newRow["PriceDate"] = offer.PriceDate.ToString();
				newRow["ProductName"] = offer.ProductName;
				newRow["ProducerName"] = offer.ProducerName;
				newRow["SupplierName"] = offer.SupplierName;
				newRow["RegionName"] = offer.RegionName;
				newRow["Cost"] = Convert.ToDecimal(offer.Cost);
				newRow["ClientName"] = offer.ClientName;
				if (offer.RequestRatio.HasValue)
					newRow["RequestRatio"] = offer.RequestRatio.Value;
				if (offer.OrderCost.HasValue)
					newRow["OrderCost"] = Convert.ToDecimal(offer.OrderCost.Value);
				if (offer.MinOrderCount.HasValue)
					newRow["MinOrderCount"] = offer.MinOrderCount.Value;

				dtNewRes.Rows.Add(newRow);
			}

			if (_dsReport.Tables.Contains("Results"))
				_dsReport.Tables.Remove("Results");
			_dsReport.Tables.Add(dtNewRes);
		}

		protected override void DataTableToExcel(DataTable dtExport, string fileName)
		{
			fileName = Path.Combine(Path.GetDirectoryName(fileName), ReportCaption + ".dbf"); // отчет сохраняется только в dbf
			DataTableToDbf(dtExport, fileName);
		}

		public override bool DbfSupported
		{
			get { return true; }
		}

		protected override void DataTableToDbf(DataTable dtExport, string fileName)
		{
			dtExport.Columns[0].ColumnName = "PRICEDATE";
			dtExport.Columns[1].ColumnName = "PRODUCT";
			dtExport.Columns[2].ColumnName = "PRODUCER";
			dtExport.Columns[3].ColumnName = "SUPPLIER";
			dtExport.Columns[4].ColumnName = "REGION";
			dtExport.Columns[5].ColumnName = "COST";
			dtExport.Columns[6].ColumnName = "CLIENT";
			dtExport.Columns[7].ColumnName = "RATIO";
			dtExport.Columns[8].ColumnName = "MINSUM";
			dtExport.Columns[9].ColumnName = "MINKOL";

			base.DataTableToDbf(dtExport, fileName);
		}

		private class ReportData
		{
			public ReportData(IDataRecord row)
			{
				if (row == null)
					throw new ArgumentNullException("row");

				PriceDate = Convert.ToDateTime(row["PriceDate"]);
				if (!Convert.IsDBNull(row["ProductName"]))
					ProductName = Convert.ToString(row["ProductName"]);
				if (!Convert.IsDBNull(row["ProducerName"]))
					ProducerName = Convert.ToString(row["ProducerName"]);
				SupplierName = Convert.ToString(row["SupplierName"]);
				RegionName = Convert.ToString(row["RegionName"]);
				Cost = Convert.ToSingle(row["Cost"]);
				ClientName = Convert.ToString(row["ClientName"]);
				if (!Convert.IsDBNull(row["RequestRatio"]))
					RequestRatio = Convert.ToInt32(row["RequestRatio"]);
				else
					RequestRatio = null;
				if (!Convert.IsDBNull(row["OrderCost"]))
					OrderCost = Convert.ToSingle(row["OrderCost"]);
				else
					OrderCost = null;
				if (!Convert.IsDBNull(row["MinOrderCount"]))
					MinOrderCount = Convert.ToInt32(row["MinOrderCount"]);
				else
					MinOrderCount = null;
			}

			public DateTime PriceDate { get; private set; }
			public string ProductName { get; private set; }
			public string ProducerName { get; private set; }
			public string SupplierName { get; private set; }
			public string RegionName { get; private set; }
			public float Cost { get; private set; }
			public string ClientName { get; private set; }
			public int? RequestRatio { get; private set; }
			public float? OrderCost { get; private set; }
			public int? MinOrderCount { get; private set; }
		}
	}
}