using System;
using System.Data;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using ExecuteTemplate;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.FastReports
{
	public abstract class BaseFastReport : BaseReport
	{
		public BaseFastReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn, bool Temporary, ReportFormats format, DataSet dsProperties) 
			: base(ReportCode, ReportCaption, Conn, Temporary, format, dsProperties)
		{
		}

		protected bool IsNewClient(ExecuteArgs e, int clientId)
		{
			e.DataAdapter.SelectCommand.CommandText = "select * from future.Clients where Id = " + clientId;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			bool isNewClient = reader.Read();
			reader.Dispose();

			return isNewClient;
		}

		//Получили список действующих прайс-листов для интересующего клиента
		protected void GetActivePrices(ExecuteArgs e, int clientId, bool isNewClient)
		{
			//удаление временных таблиц
			e.DataAdapter.SelectCommand.CommandText = "drop temporary table IF EXISTS Prices, ActivePrices, Core, MinCosts";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			if (isNewClient)
				GetActivePricesNew(e, clientId);
			else
				GetActivePricesOld(e, clientId);
		}

		protected void GetActivePricesNew(ExecuteArgs e, int clientId)
		{// Небольшая магия, через любого пользователя получаем прайсы клиента

			// Получаем первого попавшегося пользователя
			e.DataAdapter.SelectCommand.CommandText = "select Id from future.Users where ClientId = " + clientId + " limit 1, 1";
			var userId = Convert.ToUInt32(e.DataAdapter.SelectCommand.ExecuteScalar());

			// Получаем для него все прайсы
			e.DataAdapter.SelectCommand.CommandText = "future.GetPrices";
			e.DataAdapter.SelectCommand.CommandType = CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			// Включаем для него все прайсы
			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
			e.DataAdapter.SelectCommand.CommandText = "update Prices set DisabledByClient = 0";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

			// Получаем для пользователя активные (которыми теперь являются все) прайсы
			e.DataAdapter.SelectCommand.CommandText = "future.GetActivePrices";
			e.DataAdapter.SelectCommand.CommandType = CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		protected void GetActivePricesOld(ExecuteArgs e, int clientId)
		{
			e.DataAdapter.SelectCommand.CommandText = "usersettings.GetActivePrices";
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ClientCodeParam", clientId);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		//Получили список предложений для интересующего клиента
		protected void GetOffers(ExecuteArgs e, int clientId)
		{
			bool isNewClient = IsNewClient(e, clientId);

			GetActivePrices(e, clientId, isNewClient);

			if (isNewClient)
				GetOffersNew(e, clientId);
			else
				GetOffersOld(e, clientId);

			e.DataAdapter.SelectCommand.CommandType = CommandType.Text;
		}

		protected void GetOffersNew(ExecuteArgs e, int clientId)
		{ // Небольшая магия, через любого пользователя получаем предложение для клиента

			// Получаем первого попавшегося пользователя
			e.DataAdapter.SelectCommand.CommandText = "select Id from future.Users where ClientId = " + clientId + " limit 1, 1";
			var userId = Convert.ToUInt32(e.DataAdapter.SelectCommand.ExecuteScalar());

			//Проверка существования и отключения клиента
			e.DataAdapter.SelectCommand.CommandText =
				"select * from future.Clients cl where cl.Id = " + clientId;
			var reader = e.DataAdapter.SelectCommand.ExecuteReader();
			if (!reader.Read())
				throw new ReportException(String.Format("Невозможно найти клиента с кодом {0}.", clientId));
			if (Convert.ToByte(reader["Status"]) == 0)
				throw new ReportException(String.Format("Невозможно сформировать отчет по отключенному клиенту {0} ({1}).", reader["Name"], clientId));
			reader.Dispose();

			e.DataAdapter.SelectCommand.CommandText = "future.GetOffers";
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?UserIdParam", userId);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		protected void GetOffersOld(ExecuteArgs e, int clientId)
		{
			e.DataAdapter.SelectCommand.CommandText = "usersettings.GetOffers";
			e.DataAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ClientCodeParam", clientId);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?FreshOnly", 0);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
		}

		protected override void FormatExcel(string FileName)
		{
			ProfileHelper.Next("FormatExcel");
			MSExcel.Application exApp = new MSExcel.ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				MSExcel.Workbook wb = exApp.Workbooks.Open(FileName, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
				MSExcel._Worksheet ws;
				try
				{
					ws = (MSExcel._Worksheet)wb.Worksheets["rep" + _reportCode.ToString()];

					try
					{
						ws.Name = _reportCaption.Substring(0, (_reportCaption.Length < MaxListName) ? _reportCaption.Length : MaxListName);

						DataTable res = _dsReport.Tables["Results"];
						for (int i = 0; i < res.Columns.Count; i++)
						{
							ws.Cells[1, i + 1] = "";
							ws.Cells[1, i + 1] = res.Columns[i].Caption;
							if (res.Columns[i].ExtendedProperties.ContainsKey("Width"))
								((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).ColumnWidth = ((int?)res.Columns[i].ExtendedProperties["Width"]).Value;
							else
								((MSExcel.Range)ws.Columns[i + 1, Type.Missing]).AutoFit();
							if (res.Columns[i].ExtendedProperties.ContainsKey("Color"))
								ws.get_Range(ws.Cells[1, i + 1], ws.Cells[res.Rows.Count + 1, i + 1]).Interior.Color = System.Drawing.ColorTranslator.ToOle((System.Drawing.Color)res.Columns[i].ExtendedProperties["Color"]);
						}

						//рисуем границы на всю таблицу
						ws.get_Range(ws.Cells[1, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count]).Borders.Weight = MSExcel.XlBorderWeight.xlThin;

						//Устанавливаем шрифт листа
						ws.Rows.Font.Size = 8;
						ws.Rows.Font.Name = "Arial Narrow";
						ws.Activate();

						//Устанавливаем АвтоФильтр на все колонки
						((MSExcel.Range)ws.get_Range(ws.Cells[1, 1], ws.Cells[res.Rows.Count + 1, res.Columns.Count])).Select();
						((MSExcel.Range)exApp.Selection).AutoFilter(1, System.Reflection.Missing.Value, Microsoft.Office.Interop.Excel.XlAutoFilterOperator.xlAnd, System.Reflection.Missing.Value, true);

						PostProcessing(exApp, ws);
					}
					finally
					{
						wb.SaveAs(FileName, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, MSExcel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
					}
				}
				finally
				{
					ws = null;
					wb = null;
					try { exApp.Workbooks.Close(); }
					catch { }
				}
			}
			finally
			{
				try { exApp.Quit(); }
				catch { }
				exApp = null;
			}
			ProfileHelper.End();
		}

		/// <summary>
		/// Дополнительные действия с форматированием отчета, специфичные для отчета
		/// </summary>
		/// <param name="exApp"></param>
		/// <param name="ws"></param>
		protected virtual void PostProcessing(MSExcel.Application exApp, MSExcel._Worksheet ws)
		{
		}
	}
}
