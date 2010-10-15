using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common.Tools;
using ExecuteTemplate;
using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.ByOrders
{
	public class Period
	{
		public DateTime Begin;
		public DateTime End;

		public Period(DateTime dtFrom, DateTime dtTo)
		{
			Begin = dtFrom;
			End = dtTo;
		}
	}

	public class SupplierMarketShareByUser : BaseReport
	{
		private uint _supplierId;
		private Period _period;
		private List<ulong> _regions;
		//private List<string > head;

		private const string _mandatoryOrderFilter = "oh.Deleted = 0 and oh.Submited = 1";
		private const string _mandatoryClientFilter = "c.PayerId <> 921 and rcs.InvisibleOnFirm < 2";
		private const string _filters = _mandatoryOrderFilter + " and " + _mandatoryClientFilter;

		public SupplierMarketShareByUser(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties) 
			: base(reportCode, reportCaption, connection, temporary, format, dsProperties)
		{}

		public override void ReadReportParams()
		{
			_supplierId = Convert.ToUInt32(getReportParam("SupplierId"));
			_period = GetPeriod();
			_regions = (List<ulong>) getReportParam("Regions");
		}

		public Period GetPeriod()
		{
			var byPreviousMonth = (bool)getReportParam("ByPreviousMonth");
			if (byPreviousMonth)
			{
				var dtTo = DateTime.Now;
				dtTo = dtTo.AddDays(-(dtTo.Day - 1)).Date; // Первое число текущего месяца
				var dtFrom = dtTo.AddMonths(-1).Date;
				return new Period(dtFrom, dtTo);
			}
			else
			{
				var _reportInterval = (int)getReportParam("ReportInterval");
				var dtTo = DateTime.Now;
				//От текущей даты вычитаем интервал - дата начала отчета
				var dtFrom = dtTo.AddDays(-_reportInterval).Date;
				//К текущей дате 00 часов 00 минут является окончанием периода и ее в отчет не включаем
				dtTo = dtTo.Date;
				return new Period(dtFrom, dtTo);
			}
/*			return new Period {
				Begin = Convert.ToDateTime(getReportParam("Begin")),
				End = Convert.ToDateTime(getReportParam("End"))
			};*/
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.Excel)
				return new SupplierExcelWriter(/*head*/);
			return null;
		}

		protected override BaseReportSettings GetSettings()
		{
			return new BaseReportSettings(_reportCode, _reportCaption);
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select c.Name as ClientName,
ifnull(u.Name, CAST(u.Id AS CHAR)) as UserName,
sum(ol.Cost * ol.Quantity) as TotalSum,
sum(if(pd.FirmCode = ?SupplierId, ol.Cost * ol.Quantity, 0)) as SupplierSum
from Orders.OrdersHead oh 
	join Orders.OrdersList ol on ol.OrderId = oh.RowId
	join Future.Clients c on c.Id = oh.ClientCode
		join Future.Users u on u.ClientId = c.Id and oh.UserId = u.Id
	join Usersettings.RetClientsSet rcs on rcs.ClientCode = oh.ClientCode
	join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
where oh.WriteTime > ?begin
and oh.WriteTime < ?end
and oh.RegionCode in ({1})
and {0}
group by u.Id
order by ClientName, UserName", _filters, _regions.Implode());

			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SupplierId", _supplierId);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?begin", _period.Begin);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?end", _period.End);
			e.DataAdapter.Fill(_dsReport, "data");
			var data = _dsReport.Tables["data"];
			var result = _dsReport.Tables.Add("Results");
            result.Columns.Add("ClientName");
            result.Columns.Add("UserName");
            result.Columns.Add("Share", typeof(double));

		    //result.Rows[0][0] = "dfgd";
			/*head = new List<string>();
			head.Add("Поставщик");
			head.Add("Период");*/
		    MySqlCommand headParameterCommand = _conn.CreateCommand();
            String shPCommand = "select cd.ShortName from usersettings.clientsdata cd where cd.FirmCode = " + _supplierId.ToString();
		    headParameterCommand.CommandText = shPCommand;
		    MySqlDataReader headParameterReader = headParameterCommand.ExecuteReader();
            result.Rows.Add("Поставщик");
            //result.Rows[0][0] = "";
            if (headParameterReader.Read())
            {
                result.Rows[0][1] = headParameterReader["ShortName"];
            }
            headParameterReader.Close();
            result.Rows.Add("Период: ");
		    result.Rows[1][1] = "с " + _period.Begin.Date.ToString() + " по " + _period.End.Date.ToString();
		    string sRegions = "";
            for (int i = 0; i < _regions.Count; i++)
            {
                shPCommand = "select reg.region from farm.regions reg where reg.RegionCode = " +
                             _regions[i].ToString();
                headParameterCommand.CommandText = shPCommand;
                headParameterReader = headParameterCommand.ExecuteReader();
                if (headParameterReader.Read())
                {
                    sRegions += headParameterReader["region"];
					//head.Add("Регионы");
                }
                headParameterReader.Close();
            }
            result.Rows.Add("Регионы");
		    result.Rows[2][1] = sRegions;
		    result.Rows.Add("");
            //result.Rows.Add("TEST");

            result.Columns["ClientName"].Caption = "Имя Клиента";
			result.Columns["UserName"].Caption = "Пользователь";
			result.Columns["Share"].Caption = "Доля рынка, %";
			foreach (var row in data.Rows.Cast<DataRow>())
			{
				var resultRow = result.NewRow();
				var total = Convert.ToDouble(row["TotalSum"]);
				if (total == 0)
					resultRow["Share"] = DBNull.Value;
				else
					resultRow["Share"] = Math.Round((Convert.ToDouble(row["SupplierSum"])/total) * 100, 2);
				resultRow["ClientName"] = row["ClientName"];
				resultRow["UserName"] = row["UserName"];
				result.Rows.Add(resultRow);
			}
		}
	}
}