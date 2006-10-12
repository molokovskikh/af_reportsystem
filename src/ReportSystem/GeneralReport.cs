using System;
using System.Data;
using MySql.Data.MySqlClient;


namespace Inforoom.RatingReport
{
	/// <summary>
	/// Summary description for CombineReport.
	/// </summary>
	public class CombineReport
	{

		public const string colCombineReportCode = "CombineReportCode";
		public const string colFirmCode = "ClientsData_FirmCode";
		public const string colAllow = "Allow";

		public int combineReportID;
		public int clientCode;

		MySqlConnection conn;
		DataTable dtReports;

		Rating[] Ratings;
		DataTable[] dtRes;

		public CombineReport(int ID, int ClientCode, MySqlConnection Conn)
		{
			combineReportID = ID;
			clientCode = ClientCode;
			conn = Conn;

			dtReports = new DataTable();

			//Формируем запрос и заполняем таблицу дочерних отчетов
			MySqlDataAdapter daReports = new MySqlDataAdapter(String.Format("select * from usersettings.Reports where {0} = ?{0}", Rating.colCombineReportCode), conn);
			daReports.SelectCommand.Parameters.Add(Rating.colCombineReportCode, combineReportID);
			daReports.Fill(dtReports);

			if (dtReports.Rows.Count > 0)
			{
				Ratings = new Rating[dtReports.Rows.Count];
				dtRes = new DataTable[dtReports.Rows.Count];

				DataRow dr;
				for(int i = 0; i < dtReports.Rows.Count; i++)
				{
					dr = dtReports.Rows[i];
					Ratings[i] = new Rating(Convert.ToInt32(dr[Rating.colReportCode]), 0, dr[Rating.colReportCaption].ToString(), conn);
				}
			}
			else
				throw new Exception("У комбинированного отчета нет дочерних отчетов.");
		}

		public int ReportCount{
			get
			{
				return Ratings.Length;
			}
		}

		public string GetReportCaption(int Index)
		{
			return Ratings[Index].reportCaption;
		}

		public DataTable GetReportTable(int Index)
		{
			return dtRes[Index];
		}

		public void ExportToExcel()
		{
		}

	}
}
