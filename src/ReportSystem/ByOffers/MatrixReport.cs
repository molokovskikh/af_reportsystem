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
using Common.Models.Helpers;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
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
			GetOffers();

			var rules = Session.Get<OrderRules>((uint)_clientCode);
			_client = Session.Get<Client>((uint)_clientCode);
			var matrixHelper = new MatrixHelper(rules);
			var sql = matrixHelper.BuyingMatrixCondition(false);
			sql.Having = Regex.Replace(sql.Having.Trim(), @"[\d$]", "0");

			var fromQueryPart = SqlQueryBuilderHelper.GetFromPartForCoreTable(sql, false);
			fromQueryPart = string.Format(fromQueryPart,  string.Format(@"
left join farm.Synonym syn on syn.PriceCode = {0}.PriceId and syn.ProductId = {0}.ProductId
left join farm.Synonym syn1 on  syn1.PriceCode = {1}.PriceId and syn1.ProductId = {1}.ProductId
left join farm.Synonymfirmcr synCr on synCr.PriceCode = {0}.PriceId and synCr.CodeFirmCr = {0}.ProducerId
left join farm.Synonymfirmcr synCr1 on  synCr1.PriceCode = {1}.PriceId and synCr1.CodeFirmCr = {1}.ProducerId
join catalogs.Producers prod on prod.Id = core.CodeFirmCr
left join farm.Synonym origSyn on origSyn.PriceCode = Core.PriceCode and origSyn.ProductId = Core.ProductId
left join farm.Synonymfirmcr origSynCr on origSynCr.PriceCode = Core.PriceCode and origSynCr.CodeFirmCr = Core.CodeFirmCr
", sql.Alias, sql.Alias2));

			var selectPart = string.Format(@"
select
if ({0}.ProductId is not null, {0}.ProductId, if ({1}.ProductId is not null, {1}.ProductId, null)) as ProductId,
if ({0}.ProducerId is not null, {0}.ProducerId, if ({1}.ProducerId is not null, {1}.ProducerId, null)) as ProducerId,
if (syn.Synonym is not null, syn.Synonym, syn1.Synonym) as ProductSynonym,
if (synCr.Synonym is not null, synCr.Synonym, synCr1.Synonym) as ProducerSynonym,
catalog.Name as CatalogName,
prod.Name as ProducerName,
Core.Code as OriginalCode,
Core.CodeCr as OriginalCodeCr,
origSyn.Synonym as OriginalName,
origSynCr.Synonym as OriginalProducerName,
AT.FirmName as  FirmName,
AT.PriceDate as PriceDate
", sql.Alias, sql.Alias2);
			e.DataAdapter.SelectCommand.CommandText = selectPart + sql.Select + Environment.NewLine + fromQueryPart;
			if (rules.OfferMatrix.HasValue)
				e.DataAdapter.SelectCommand.Parameters.AddWithValue("ClientCode", _clientCode);

			var result = new DataTable("Results");
			e.DataAdapter.Fill(result);
			foreach (DataRow row in result.Rows) {
				row.SetAdded();
			}
			result.Columns.Add("BuyingMatrixTypeString", typeof(string));
			foreach (DataRow row in result.Rows) {
				row["BuyingMatrixTypeString"] = GetMatrigTypeString(row);
			}
			for (int i = 0; i < 4; i++) {
				var row = result.NewRow();
				result.Rows.InsertAt(row, 0);
			}

			PrepareCollumns(result, rules);
			SetTableCollomnNames(result);
			SetTableCollumnWidth(result);

			_dsReport.Tables.Add(result);

			FilterDescriptions.AddRange(new[] {
				"Товары поставщиков, подпадающие под действие матрицы",
				String.Format("Выбранная аптека: {0}", _client.Name),
				String.Format("Отчет сформирован: {0}", DateTime.Now),
				string.Empty
			});
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
