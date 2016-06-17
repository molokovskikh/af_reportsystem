using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.ByOrders
{
	/*Отчет статистика заказов по поставщику*/

	public class SupplierOrdersStatistics : OrdersStatistics
	{
		protected int sourceFirmCode; //Поставщик, по которому будет строится отчет
		protected int reportType; // Вариант отчета (1 - Позаявочно, 2 - Поклиентно)
		protected List<ulong> regions; // Список регионов
		protected string regionsString; // Список регионов в виде строки

		public SupplierOrdersStatistics(MySqlConnection Conn, DataSet dsProperties)
			: base(Conn, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			sourceFirmCode = (int)GetReportParam("SourceFirmCode"); // поставщик
			reportType = (int)GetReportParam("ReportType");
			if (_reportParams.ContainsKey("RegionEqual")) {
				regions = (List<ulong>)GetReportParam("RegionEqual");
				if (regions.Contains(0))
					regions.Clear(); // все регионы
				regionsString = String.Join(", ", regions.ConvertAll(value => value.ToString()).ToArray());
			}
		}

		protected override void GenerateReport()
		{
			Header.Add(String.Format("Выбранный поставщик : {0}", GetValuesFromSQL("select concat(supps.Name, ' - ', rg.Region) as FirmShortName from Customers.suppliers supps, farm.regions rg where rg.RegionCode = supps.HomeRegion and supps.Id = " + sourceFirmCode)));
			if (!String.IsNullOrEmpty(regionsString))
				Header.Add(String.Format("Список регионов : {0}", GetValuesFromSQL("select r.Region from farm.regions r where r.RegionCode in (" + regionsString + ") order by r.Region;")));
			ProfileHelper.Next("GenerateReport");
			string selectedColumns;
			string groupbyColumns;
			string orderbyColumns;
			if (reportType == 1) {
				selectedColumns = @"
	oh.writetime,
	oh.pricedate,
	fi.supplierclientid firmclientcode,
	cl.name shortname,
	r.Region,
	count(oh.rowid) rowCount,
	ROUND(SUM(ol.cost*ol.Quantity),2) summa";
				groupbyColumns = "oh.rowid";
				orderbyColumns = "oh.writetime";
			}
			else {
				selectedColumns = @"
	fi.supplierclientid firmclientcode,
	cl.name shortname,
	r.Region,
	ROUND(SUM(ol.cost*ol.Quantity),2) summa";
				groupbyColumns = "cl.Id, oh.RegionCode";
				orderbyColumns = "cl.Name";
			}

			var selectCommand = String.Format(@"
select {0}
from
	{1}.ordershead oh
    inner join {1}.orderslist ol on oh.rowid = ol.orderid
    inner join usersettings.pricesdata pd on oh.pricecode = pd.pricecode
    inner join farm.regions r on oh.regioncode = r.regioncode
    inner join Customers.users u on oh.userid = u.id
    inner join Customers.clients cl on oh.clientcode = cl.id
	inner join usersettings.retclientsset rcs on cl.id = rcs.clientcode
    inner join Customers.addresses a on oh.addressid = a.id
    left join Customers.intersection fi on fi.clientid = cl.id
        and fi.regionid = oh.regioncode
        and fi.priceid = pd.pricecode
        and fi.legalentityid = a.legalentityid
where
	pd.firmcode = {2}
	and pd.IsLocal = 0
	and oh.writetime between '{3}' and '{4}' ", selectedColumns, OrdersSchema, sourceFirmCode, Begin.ToString("yyyy-MM-dd HH:mm:ss"), End.ToString("yyyy-MM-dd HH:mm:ss"));

			if (!String.IsNullOrEmpty(regionsString))
				selectCommand += String.Format("and oh.regioncode in ({0}) ", regionsString);

			selectCommand += String.Format("group by {0} order by {1}", groupbyColumns, orderbyColumns);

#if DEBUG
			Debug.WriteLine(selectCommand);
#endif
			var dtNewRes = new DataTable();
			if (reportType == 1) {
				dtNewRes.Columns.Add("WriteTime", typeof(string));
				dtNewRes.Columns.Add("PriceDate", typeof(string));
				dtNewRes.Columns.Add("FirmClientCode", typeof(int));
				dtNewRes.Columns.Add("ShortName", typeof(string));
				dtNewRes.Columns.Add("Region", typeof(string));
				dtNewRes.Columns.Add("RowCount", typeof(int));
				dtNewRes.Columns.Add("Summa", typeof(decimal));
				dtNewRes.Columns["WriteTime"].Caption = "Дата заявки";
				dtNewRes.Columns["PriceDate"].Caption = "Дата прайса";
				dtNewRes.Columns["RowCount"].Caption = "Позиций";
			}
			else {
				dtNewRes.Columns.Add("FirmClientCode", typeof(int));
				dtNewRes.Columns.Add("ShortName", typeof(string));
				dtNewRes.Columns.Add("Region", typeof(string));
				dtNewRes.Columns.Add("Summa", typeof(decimal));
			}
			dtNewRes.Columns["FirmClientCode"].Caption = "Код клиента";
			dtNewRes.Columns["ShortName"].Caption = "Наименование клиента";
			dtNewRes.Columns["Region"].Caption = "Регион";
			dtNewRes.Columns["Summa"].Caption = "Сумма";

			DataAdapter.SelectCommand.CommandText = selectCommand;
			DataAdapter.SelectCommand.Parameters.Clear();
			DataAdapter.Fill(dtNewRes);
			//Добавляем несколько пустых строк, чтобы потом вывести в них значение фильтра в Excel
			foreach (var t in Header)
				dtNewRes.Rows.InsertAt(dtNewRes.NewRow(), 0);

			var res = dtNewRes.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
		}
	}
}