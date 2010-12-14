using System;
using System.Data;
using System.Data.OleDb;
using System.Collections.Generic;
using System.Text;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.Writers
{
	/*public interface IGetterNames
	{
		List<string> GetterNames(List<ulong> clients, ExecuteArgs e);
	}*/

	public class PricesOfCompetitorsWriter : BaseExcelWriter
	{
		public Dictionary<string, GetterNames> AssiciateReportParams;
		public Dictionary<string, object> ReportParams;
		public delegate List<string> GetterNames(List<ulong> items, ExecuteArgs e);
		public ExecuteArgs e;

		public PricesOfCompetitorsWriter(Dictionary<string, object> reportParams, ExecuteArgs ex)
		{
			AssiciateReportParams = new Dictionary<string, GetterNames>
			                        	{
			                        		 {"PayerEqual", ReadParameterHelper.GetPayerNames},
											 {"FirmCodeEqual", ReadParameterHelper.GetSupplierNames},
											 {"PriceCode", ReadParameterHelper.GetPriceName},
											 {"FirmCrEqual", ReadParameterHelper.GetCrNames},
											 {"FirmCrNonEqual", ReadParameterHelper.GetCrNames},
											 {"IgnoredSuppliers", ReadParameterHelper.GetSupplierNames},
											 {"RegionEqual", ReadParameterHelper.GetRegionNames},
											 {"RegionNonEqual", ReadParameterHelper.GetRegionNames},
											 {"PriceCodeValues", ReadParameterHelper.GetPriceNames},
											 {"PriceCodeNonValues", ReadParameterHelper.GetPriceNames},
											 {"ClientsNON", ReadParameterHelper.GetClientNames},
											 {"PayerNonEqual", ReadParameterHelper.GetPayerNames},
			                        	};
			ReportParams = reportParams;
			e = ex;
		}

		private string GetDescription(ExecuteArgs e, string PropertyName)
		{
			e.DataAdapter.SelectCommand.CommandText = "SELECT r.DisplayName FROM reports.report_type_properties r" +
														" WHERE r.PropertyName = \"" + PropertyName + "\"";
			var dataReader = e.DataAdapter.SelectCommand.ExecuteReader();
			dataReader.Read();
			
			var result = dataReader["DisplayName"].ToString();
			dataReader.Close();
			return result;
		}

		public override void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
		{
			var _result = reportData.Tables["Results"];
			var ppz = _result.NewRow();
			ppz[0] = "Отчет сформирован: " + DateTime.Now;
			/*var items = new object[_result.Columns.Count];
			items[0] = "Отчет сформирован: " + DateTime.Now;
			ppz.ItemArray = items;*/
			//object Date = "Отчет сформирован: " + DateTime.Now;
			_result.Rows.InsertAt(ppz, 0);

			var CountDownRows = ReportParams.Count + 5;
			var reportParameters = new List<object>();
			foreach (var reportParam in ReportParams)
			{
				var typeReportParam = reportParam.Value.GetType();
				if (typeReportParam.IsGenericType)
				{
					var itemList = (List<ulong>) reportParam.Value;
					
					var namesList = (AssiciateReportParams[reportParam.Key](itemList, e));
					reportParameters.Add(GetDescription(e, reportParam.Key) + ": " + string.Join(" ,", namesList.ToArray()));
				}
				if (typeReportParam == typeof(bool))
				{
					var YesNo = (bool) reportParam.Value ? " Да" : " Нет";
					reportParameters.Add(GetDescription(e, reportParam.Key) + YesNo);
				}
				if (typeReportParam == typeof(Int32))
				{
					var value = Convert.ToUInt32(reportParam.Value);
					if (AssiciateReportParams.ContainsKey(reportParam.Key))
					{
						var tempList = new List<ulong> {value};
						var namesList = (AssiciateReportParams[reportParam.Key](tempList, e));
						reportParameters.Add(GetDescription(e, reportParam.Key) + ": " + namesList[0] + "(" + value + ")" );
					}
					else
					{
						reportParameters.Add(GetDescription(e, reportParam.Key) + ": " + value);
					}
				}
			}
			var position = 1;
			foreach (var reportParameter in reportParameters)
			{
				/*ppz.Rows.Add(reportParameter);
				var nrrow = ppz.NewRow();
				nrow.ItemArray = ppz.Rows[position].ItemArray;
				_result.Rows.InsertAt(nrrow, position);*/
				ppz = _result.NewRow();
				/*items[0] = reportParameter;
				ppz.ItemArray = items;*/
				ppz[0] = reportParameter;
				_result.Rows.InsertAt(ppz, position);
				position++;
			}
			var delemitRow = _result.NewRow();
			_result.Rows.InsertAt(delemitRow, position);
			delemitRow = _result.NewRow();
			_result.Rows.InsertAt(delemitRow, position++);

			DataTableToExcel(_result, fileName, settings.ReportCode);

			UseExcel.Workbook(fileName, b =>
			{
				var _ws = (MSExcel._Worksheet)b.Worksheets["rep" + settings.ReportCode.ToString()];
				//_ws.Rows.EntireRow.Insert()
				if (CountDownRows > 0)
				{
					for (int j = 1; j < CountDownRows - 1; j++)
					{
						for (int i = 0; i < CountDownRows - 3; i++)
						{
							_ws.Cells[1 + i, j] = _ws.Cells[2 + i, j];
						}
						_ws.Cells[CountDownRows - 2, j] = "";
						_ws.get_Range("A" + j.ToString(), "Z" + j.ToString()).Merge();
					}
				}
				if (CountDownRows == 0)
				{
					CountDownRows = 2;
				}
				for (int i = 4; i < 20; i++)
				{
					_ws.Cells[1, i] = "";
				}
				for (int i = 0; i < _result.Columns.Count; i++)
				{
					_ws.Cells[CountDownRows - 1, i + 1] = "";
					_ws.Cells[CountDownRows - 1, i + 1] = _result.Columns[i].Caption;
					if (CountDownRows != 2)
					{
						_ws.Cells[1, 4] = "";
					}

					Console.WriteLine(_result.Columns[i]);

					if (_result.Columns[i].ExtendedProperties.ContainsKey("Width"))
						((MSExcel.Range)_ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)_result.Columns[i].ExtendedProperties["Width"]).Value;
					else
						((MSExcel.Range)_ws.Columns[i + 1, Type.Missing]).AutoFit();
					if (_result.Columns[i].ExtendedProperties.ContainsKey("Color"))
						_ws.get_Range(_ws.Cells[CountDownRows, i + 1], _ws.Cells[_result.Rows.Count + 1, i + 1]).Interior.Color = System.Drawing.ColorTranslator.ToOle((System.Drawing.Color)_result.Columns[i].ExtendedProperties["Color"]);
				}

				//рисуем границы на всю таблицу
				_ws.get_Range(_ws.Cells[CountDownRows - 1, 1], _ws.Cells[_result.Rows.Count + 1, _result.Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

				//Устанавливаем шрифт листа
				_ws.Rows.Font.Size = 8;
				_ws.Rows.Font.Name = "Arial Narrow";
				_ws.Activate();

				//Устанавливаем АвтоФильтр на все колонки
				_ws.Range[_ws.Cells[CountDownRows - 1, 1], _ws.Cells[_result.Rows.Count + 1, _result.Columns.Count]].Select();
				((MSExcel.Range)_ws.Application.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);
			});
			ProfileHelper.End();
		}
	}


    public class SupplierExcelWriter : BaseExcelWriter
    {
    	//private List<string> locallist;
		/*public SupplierExcelWriter(List<string> L)
		{
			locallist = L;
		}*/

    	public override void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
         {
             DataTableToExcel(reportData.Tables["Results"], fileName, settings.ReportCode);
             UseExcel.Workbook(fileName, b =>
                                             {
                                                 var ws = (MSExcel._Worksheet)b.Worksheets["rep" + settings.ReportCode.ToString()];
                                                 base.FormatExcelFile(ws, reportData.Tables["Results"], settings.ReportCaption, 6);
                                             });
             ProfileHelper.End();
         }
    }
}
