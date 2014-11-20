using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Common.Models;
using Common.Models.BuyingMatrix;
using Common.Models.Helpers;
using Common.Tools;

using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using DataTable = System.Data.DataTable;

namespace Inforoom.ReportSystem.ByOffers
{
	public class MatrixReport : ProviderReport
	{
		private Client _client;

		public MatrixReport(ulong reportCode, string reportCaption, MySqlConnection connection, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, format, dsProperties)
		{
		}

		private static string[] CollumnNames = new[] {
			"Код товара из матрицы",
			"Код изготовителя из матрицы",
			"Написание товара из матрицы",
			"Написание изготовителя из матрицы",
			"Каталожное написание товара",
			"Каталожное написание изготовителя",
			"Оригинальный код товара",
			"Оригинальный код изготовителя",
			"Оригинальное наименование товара",
			"Оригинальное наименование Производителя",
			"Поставщик",
			"Дата и время прайс-листа",
			"Прайс лист матрицы",
			"Действие по позиции"
		};

		private static Dictionary<string, int> ColumnsWidhts = new Dictionary<string, int> {
			{ "ProductSynonym", 60 },
			{ "ProducerSynonym", 60 },
			{ "CatalogName", 60 },
			{ "ProducerName", 40 },
			{ "OriginalName", 60 },
			{ "OriginalProducerName", 40 }
		};

		public override void GenerateReport(ExecuteArgs e)
		{
			var rules = Session.Get<OrderRules>((uint)_clientCode);
			_client = Session.Get<Client>((uint)_clientCode);

			GetOffers();
			SetFilterDescriptions();

			var matrixHelper = new MatrixHelper(rules);
			var sql = matrixHelper.BuyingMatrixCondition(false);
			sql.Having = Regex.Replace(sql.Having.Trim(), @"[\d$]", "0");
			var matrixPatr = GetPartsData(rules);

			var fromQueryPart = SqlQueryBuilderHelper.GetFromPartForCoreTable(sql, false);
			fromQueryPart = string.Format(fromQueryPart,  string.Format(@"
left join farm.Core0 core01 on core01.ProductId = {0}.ProductId and core01.PriceCode = {0}.PriceId and if({0}.ProducerId is not null, {0}.ProducerId = core01.CodeFirmCr, 1)
left join farm.Synonym syn on syn.SynonymCode = core01.SynonymCode
left join farm.Synonymfirmcr synCr on synCr.SynonymFirmCrCode = core01.SynonymFirmCrCode
join catalogs.Producers prod on prod.Id = core.CodeFirmCr
left join farm.Synonym origSyn on origSyn.SynonymCode = Core.SynonymCode
left join farm.Synonymfirmcr origSynCr on origSynCr.SynonymFirmCrCode = Core.SynonymFirmCrCode
{1}
", sql.Alias, matrixPatr.Join));

			var selectPart = string.Format(@"
select
	{0}.ProductId as ProductId,
	{0}.ProducerId as ProducerId,
	syn.Synonym as ProductSynonym,
	synCr.Synonym as ProducerSynonym,
catalog.Name as CatalogName,
prod.Name as ProducerName,
Core.Code as OriginalCode,
Core.CodeCr as OriginalCodeCr,
origSyn.Synonym as OriginalName,
origSynCr.Synonym as OriginalProducerName,
AT.FirmName as  FirmName,
AT.PriceDate as PriceDate,
{1}
", sql.Alias, matrixPatr.Select);
			e.DataAdapter.SelectCommand.CommandText = selectPart + sql.Select + Environment.NewLine + fromQueryPart;
			if (rules.OfferMatrix.HasValue)
				e.DataAdapter.SelectCommand.Parameters.AddWithValue("ClientCode", _clientCode);

			var result = new DataTable("Results");
			e.DataAdapter.Fill(result);
			foreach (DataRow row in result.Rows) {
				row.SetAdded();
			}
			var whitePriceName = GetPriceForWhiteMatrix(rules);
			result.Columns.Add("BuyingMatrixTypeString", typeof(string));
			foreach (DataRow row in result.Rows) {
				row["BuyingMatrixTypeString"] = GetMatrigTypeString(row);
				if (row["MatrixPriceName"] is DBNull && !string.IsNullOrEmpty(whitePriceName))
					row["MatrixPriceName"] = whitePriceName;
			}
			for (int i = 0; i < FilterDescriptions.Count; i++) {
				var row = result.NewRow();
				result.Rows.InsertAt(row, 0);
			}

			PrepareCollumns(result, rules);
			SetTableCollomnNames(result);
			SetTableCollumnWidth(result);

			_dsReport.Tables.Add(result);
		}

		private void SetFilterDescriptions()
		{
			FilterDescriptions.AddRange(new[] {
				"Товары поставщиков, подпадающие под действие матрицы",
				String.Format("Выбранная аптека: {0}", _client.Name),
				String.Format("Отчет сформирован: {0}", DateTime.Now),
			});
			if (_reportParams.ContainsKey("FirmCodeEqual")) {
				var ids = (List<ulong>)_reportParams["FirmCodeEqual"];
				FilterDescriptions.Add(String.Format("Разрешенные поставщики: {0}", GetValuesFromSQL(GetSqlFromSuppliers(ids))));
			}
			if (_reportParams.ContainsKey("IgnoredSuppliers")) {
				var ids = (List<ulong>)_reportParams["IgnoredSuppliers"];
				FilterDescriptions.Add(String.Format("Игнорируемые поставщики: {0}", GetValuesFromSQL(GetSqlFromSuppliers(ids))));
			}
			if (_reportParams.ContainsKey("PriceCodeValues")) {
				var ids = (List<ulong>)_reportParams["PriceCodeValues"];
				FilterDescriptions.Add(String.Format("Разрешенные прайсы: {0}", GetValuesFromSQL(GetSqlFromPrices(ids))));
			}
			if (_reportParams.ContainsKey("PriceCodeEqual")) {
				var ids = (List<ulong>)_reportParams["PriceCodeEqual"];
				FilterDescriptions.Add(String.Format("Разрешенные прайсы: {0}", GetValuesFromSQL(GetSqlFromPrices(ids))));
			}
			if (_reportParams.ContainsKey("PriceCodeNonValues")) {
				var ids = (List<ulong>)_reportParams["PriceCodeNonValues"];
				FilterDescriptions.Add(String.Format("Игнорируемые прайсы: {0}", GetValuesFromSQL(GetSqlFromPrices(ids))));
			}
			if (_reportParams.ContainsKey("RegionClientEqual")) {
				var ids = (List<ulong>)_reportParams["RegionClientEqual"];
				FilterDescriptions.Add(String.Format("Разрешенные регионы: {0}", GetValuesFromSQL(GetSqlFromRegions(ids))));
			}
			FilterDescriptions.Add(string.Empty);
		}

		private string GetPriceForWhiteMatrix(OrderRules rules)
		{
			if (rules.BuyingMatrix != null && rules.BuyingMatrixType == MatrixType.WhiteList)
				return string.Format("{0} - ({1})", rules.BuyingMatrixPrice.Supplier.Name, rules.BuyingMatrixPrice.PriceName);
			if (rules.OfferMatrix != null && rules.OfferMatrixType == MatrixType.WhiteList)
				return string.Format("{0} - ({1})", rules.OfferMatrixPrice.Supplier.Name, rules.OfferMatrixPrice.PriceName);
			return null;
		}

		private SqlParts GetPartsData(OrderRules rules)
		{
			var offerJoinPart = string.Empty;
			var offerSelectPart = string.Empty;
			var buyingJoinPart = string.Empty;
			var buyingSelectPart = string.Empty;

			if (rules.OfferMatrix.HasValue) {
				offerJoinPart = "left join usersettings.PricesData pd1 on pd1.PriceCode = mol.PriceId\r\n"
					+ "left join customers.Suppliers s1 on s1.Id = pd1.FirmCode";
				offerSelectPart = "Concat(s1.Name, ' - (', pd1.PriceName, ')')";
			}

			if (rules.BuyingMatrix.HasValue) {
				buyingJoinPart = "left join usersettings.PricesData pd3 on pd3.PriceCode = bol.PriceId\r\n"
					+ "left join customers.Suppliers s3 on s3.Id = pd3.FirmCode";
				buyingSelectPart = "Concat(s3.Name, ' - (', pd3.PriceName, ')')";
			}
			var part = new SqlParts();
			if (!string.IsNullOrEmpty(offerJoinPart))
				part.Join += offerJoinPart + Environment.NewLine;
			if (!string.IsNullOrEmpty(buyingJoinPart))
				part.Join += buyingJoinPart + Environment.NewLine;
			if (rules.OfferMatrix.HasValue && rules.BuyingMatrix.HasValue) {
				part.Select = string.Format(@"if(({0}) is not null, {0}, {1})", offerSelectPart, buyingSelectPart);
			}
			else {
				if (rules.OfferMatrix.HasValue)
					part.Select = offerSelectPart;
				if (rules.BuyingMatrix.HasValue)
					part.Select = buyingSelectPart;
			}
			part.Select += " as MatrixPriceName";
			return part;
		}

		private string GetMatrigTypeString(DataRow row)
		{
			switch (row["BuyingMatrixType"].ToString()) {
				case "2":
					return "Предупреждение";
				case "1":
					return "Запрет";
				case "3":
					return "Удаление предложения";
				default:
					return "Нет статуса";
			}
		}

		private void SetTableCollumnWidth(DataTable table)
		{
			foreach (DataColumn column in table.Columns) {
				if (ColumnsWidhts.Keys.Contains(column.ColumnName))
					column.ExtendedProperties["Width"] = ColumnsWidhts[column.ColumnName];
			}
		}

		private void SetTableCollomnNames(DataTable table)
		{
			for (int i = 0; i < CollumnNames.Length; i++) {
				table.Columns[i].Caption = CollumnNames[i];
			}
		}

		private void PrepareCollumns(DataTable table, OrderRules rules)
		{
			table.Columns.Remove("BuyingMatrixType");

			if ((rules.BuyingMatrixType == MatrixType.WhiteList && rules.BuyingMatrix != null && (rules.OfferMatrix == null || rules.OfferMatrixType == MatrixType.WhiteList))
				|| (rules.OfferMatrixType == MatrixType.WhiteList && rules.OfferMatrix != null && (rules.BuyingMatrix == null || rules.BuyingMatrixType == MatrixType.WhiteList))) {
				table.Columns.Remove("ProductId");
				table.Columns.Remove("ProducerId");
				table.Columns.Remove("ProductSynonym");
				table.Columns.Remove("ProducerSynonym");

				CollumnNames = CollumnNames.Skip(4).ToArray();
			}
		}

		public override void ReadReportParams()
		{
			if (_reportParams.ContainsKey("ClientCode")) {
				_clientCode = Convert.ToInt32(getReportParam("ClientCode"));
			}
		}
	}
}
