using System;
using System.Data;
using System.Data.OleDb;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.MySql;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.Writers
{
	public class PricesOfCompetitorsWriter : BaseExcelWriter
	{
		public Dictionary<string, Func<List<ulong>, MySqlConnection, List<string>>> AssiciateReportParams;
		public Dictionary<string, object> ReportParams;
		public List<string> ParamNOVisualisation;

		private MySqlConnection connection;
		public string _reportCaption;

		public PricesOfCompetitorsWriter(MySqlConnection connection, Dictionary<string, object> reportParams, string reportCaprion)
		{
			this.connection = connection;
			ParamNOVisualisation = new List<string> {
				"AllAssortment",
				"WithWithoutProperties",
				"SupplierNoise"
			};

			AssiciateReportParams = new Dictionary<string, Func<List<ulong>, MySqlConnection, List<string>>> {
				{ "PayerEqual", ReadParameterHelper.GetPayerNames },
				{ "FirmCodeEqual", ReadParameterHelper.GetSupplierNames },
				{ "PriceCode", ReadParameterHelper.GetPriceName },
				{ "FirmCrEqual", ReadParameterHelper.GetCrNames },
				{ "FirmCrNonEqual", ReadParameterHelper.GetCrNames },
				{ "IgnoredSuppliers", ReadParameterHelper.GetSupplierNames },
				{ "RegionEqual", ReadParameterHelper.GetRegionNames },
				{ "RegionNonEqual", ReadParameterHelper.GetRegionNames },
				{ "PriceCodeValues", ReadParameterHelper.GetPriceNames },
				{ "PriceCodeNonValues", ReadParameterHelper.GetPriceNames },
				{ "ClientsNON", ReadParameterHelper.GetClientNames },
				{ "Clients", ReadParameterHelper.GetClientNames },
				{ "PayerNonEqual", ReadParameterHelper.GetPayerNames },
			};
			ReportParams = reportParams;
			_reportCaption = reportCaprion;
		}

		private string GetDescription(string propertyName)
		{
			return connection.Read("SELECT r.DisplayName FROM reports.report_type_properties r" +
				" WHERE r.PropertyName = \"" + propertyName + "\"", x => x["DisplayName"].ToString())
				.FirstOrDefault();
		}

		public override void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
		{
			var result = reportData.Tables["Results"];
			var ppz = result.NewRow();
			ppz[0] = "Отчет сформирован: " + DateTime.Now;
			result.Rows.InsertAt(ppz, 0);

			var reportParameters = new List<object>();
			foreach (var reportParam in ReportParams) {
				var typeReportParam = reportParam.Value.GetType();
				if (typeReportParam.IsGenericType) {
					if (!ParamNOVisualisation.Contains(reportParam.Key)) {
						var itemList = (List<ulong>)reportParam.Value;

						var namesList = (AssiciateReportParams[reportParam.Key](itemList, connection));
						namesList.Sort();
						var itemString = string.Join(" ,", namesList.ToArray());
						if (itemString.Length > 2048)
							itemString = itemString.Substring(0, 2047);
						reportParameters.Add(GetDescription(reportParam.Key) + ": " + itemString);
					}
				}
				if (typeReportParam == typeof(bool)) {
					if (!ParamNOVisualisation.Contains(reportParam.Key)) {
						var yesNo = (bool)reportParam.Value ? ": Да" : ": Нет";
						reportParameters.Add(GetDescription(reportParam.Key) + yesNo);
					}
				}
				if (typeReportParam == typeof(Int32)) {
					if (!ParamNOVisualisation.Contains(reportParam.Key)) {
						var value = Convert.ToUInt32(reportParam.Value);
						if (AssiciateReportParams.ContainsKey(reportParam.Key)) {
							var tempList = new List<ulong> { value };
							var namesList = (AssiciateReportParams[reportParam.Key](tempList, connection));
							if (namesList.Count > 0)
								reportParameters.Add(GetDescription(reportParam.Key) + ": " + namesList[0]);
						}
						else {
							reportParameters.Add(GetDescription(reportParam.Key) + ": " + value);
						}
					}
				}
			}
			var countDownRows = reportParameters.Count + 5;
			var position = 1;
			foreach (var reportParameter in reportParameters) {
				ppz = result.NewRow();
				ppz[0] = reportParameter;
				result.Rows.InsertAt(ppz, position);
				position++;
			}
			var delemitRow = result.NewRow();
			result.Rows.InsertAt(delemitRow, position);
			delemitRow = result.NewRow();
			result.Rows.InsertAt(delemitRow, position++);

			DataTableToExcel(result, fileName, settings.ReportCode);

			ExcelHelper.Workbook(fileName, b => {
				var ws = (MSExcel._Worksheet)b.Worksheets["rep" + settings.ReportCode.ToString()];
				ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);
				ws.Activate();

				if (countDownRows > 0) {
					for (int j = 1; j < countDownRows - 1; j++) {
						for (int i = 0; i < countDownRows - 3; i++) {
							ws.Cells[1 + i, j] = ws.Cells[2 + i, j];
						}
						ws.Cells[countDownRows - 2, j] = "";
						ws.get_Range("A" + j.ToString(), "Z" + j.ToString()).Merge();
					}
				}
				if (countDownRows == 0) {
					countDownRows = 2;
				}
				for (int i = 4; i < 20; i++) {
					ws.Cells[1, i] = "";
				}
				for (int i = 0; i < result.Columns.Count; i++) {
					ws.Cells[countDownRows - 1, i + 1] = "";
					ws.Cells[countDownRows - 1, i + 1] = result.Columns[i].Caption;
					if (countDownRows != 2) {
						ws.Cells[1, 4] = "";
					}

					if (result.Columns[i].ExtendedProperties.ContainsKey("Width"))
						((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)result.Columns[i].ExtendedProperties["Width"]).Value;
					else
						((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).AutoFit();
					if (result.Columns[i].ExtendedProperties.ContainsKey("Color"))
						ws.get_Range(ws.Cells[countDownRows, i + 1], ws.Cells[result.Rows.Count + 1, i + 1]).Interior.Color = System.Drawing.ColorTranslator.ToOle((System.Drawing.Color)result.Columns[i].ExtendedProperties["Color"]);
				}

				//рисуем границы на всю таблицу
				ws.get_Range(ws.Cells[countDownRows - 1, 1], ws.Cells[result.Rows.Count + 1, result.Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

				//Устанавливаем шрифт листа
				ws.Rows.Font.Size = 8;
				ws.Rows.Font.Name = "Arial Narrow";
				ws.Activate();

				//Устанавливаем АвтоФильтр на все колонки
				ws.Range[ws.Cells[countDownRows - 1, 1], ws.Cells[result.Rows.Count + 1, result.Columns.Count]].Select();
				((MSExcel.Range)ws.Application.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);
			});
			ProfileHelper.End();
		}
	}


	public class SupplierExcelWriter : BaseExcelWriter
	{
		public int CountDownRows = 6;

		public override void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
		{
			DataTableToExcel(reportData.Tables["Results"], fileName, settings.ReportCode);
			ExcelHelper.Workbook(fileName, b => {
				var ws = (MSExcel._Worksheet)b.Worksheets["rep" + settings.ReportCode.ToString()];
				FormatExcelFile(ws, reportData.Tables["Results"], settings.ReportCaption, CountDownRows);
			});
			ProfileHelper.End();
		}
	}
}