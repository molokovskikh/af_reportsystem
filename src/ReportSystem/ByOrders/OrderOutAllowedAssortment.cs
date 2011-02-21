﻿using System;
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
	public class OrderOutAllowedAssortment : OrdersReport
	{
		private uint _ClientId;
		private Period _period;
		//private List<ulong> _regions;
		private List<string> head;

		//private const string _mandatoryOrderFilter = "oh.Deleted = 0 and oh.Submited = 1";
		private const string _mandatoryClientFilter = "c.PayerId <> 921 and rcs.InvisibleOnFirm < 2";
		private const string _filters = /*_mandatoryOrderFilter + " and " +*/ _mandatoryClientFilter;

		public OrderOutAllowedAssortment(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties) 
			: base(reportCode, reportCaption, connection, temporary, format, dsProperties)
		{}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			/*if (_Interval)
			{
				dtFrom = _dtFrom;
				dtTo = _dtTo;
				dtTo = dtTo.Date.AddDays(1);
			}*/
			_ClientId = Convert.ToUInt32(getReportParam("ClientCode"));
			_period = new Period(dtFrom,dtTo);
			//_regions = (List<ulong>)getReportParam("Regions");
		}
		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.Excel)
				return new SupplierExcelWriter();
			return null;
		}

		protected override BaseReportSettings GetSettings()
		{
			return new BaseReportSettings(_reportCode, _reportCaption);
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
SELECT O.WriteTime,
CL.Name as ClientName,
U.Name as UserName,
Cat.Name as NameForm,
Prod.Name as Producer,
Ol.Cost, Ol.Quantity,
BM.Code as MatrixCode,
CD.ShortName AS Supplier,
(Ol.Cost*Ol.Quantity) as Summ FROM ordersold.OrdersHead O
join usersettings.RetClientsSet RC on RC.ClientCode = O.ClientCode
join ordersold.OrdersList OL on OL.OrderId = O.RowId
join catalogs.Products P on OL.ProductID = P.Id
left join farm.BuyingMatrix BM on RC.BuyingMatrixPriceId = BM.PriceId and BM.CatalogID = P.CatalogID

and if(OL.CodeFirmCr is null, BM.ProducerId is null, BM.ProducerId is null || BM.ProducerId = ol.CodeFirmCr)

join future.Clients CL on CL.ID = O.ClientCode
join future.Users U on U.ID = O.UserID
join catalogs.Catalog Cat on Cat.Id = P.CatalogID
join usersettings.PricesData PD on O.PriceCode = PD.PriceCode
join usersettings.ClientsData CD on PD.FirmCode = CD.FirmCode
left join catalogs.Producers Prod on Prod.Id = Ol.CodeFirmCr

where O.ClientCode = ?ClientCode
and BM.ID is null and
O.WriteTime > ?begin
and O.WriteTime < ?end

order by O.WriteTime");

// Если написать and BM.ID is NOT null and то будут выводится совпадающие позиции
// сейчас выводятся несовпадающие
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?ClientCode", _ClientId);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?begin", _period.Begin);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?end", _period.End);
			e.DataAdapter.Fill(_dsReport, "data");
			var data = _dsReport.Tables["data"];
			var result = _dsReport.Tables.Add("Results");
			result.Columns.Add("MatrixCode");
			result.Columns.Add("WriteTime");
			result.Columns.Add("ClientName");
			result.Columns.Add("UserName");
			result.Columns.Add("NameForm");
			result.Columns.Add("Supplier");
			result.Columns.Add("Producer");
			result.Columns.Add("Cost");
			result.Columns.Add("Quantity");
			result.Columns.Add("Sum");

			result.Rows.Add("Заказ вне разрешенного ассортимента");
			result.Rows[0][2] = "Сформирован :" + DateTime.Now.ToString();
			MySqlCommand headParameterCommand = _conn.CreateCommand();
			String shPCommand = "select CL.Name from future.Clients CL where CL.ID = " + _ClientId.ToString();
			headParameterCommand.CommandText = shPCommand;
			MySqlDataReader headParameterReader = headParameterCommand.ExecuteReader();
			result.Rows.Add("Клиент");

			if (headParameterReader.Read())
			{
				result.Rows[1][2] = headParameterReader["Name"];
			}
			headParameterReader.Close();
			result.Rows.Add("Период: ");
			result.Rows[2][2] = "с " + _period.Begin.Date.ToShortDateString() + " по " + _period.End.Date.ToShortDateString();
			result.Rows.Add("");
			//result.Rows[1][2] = "";

			result.Columns["MatrixCode"].Caption = "Код";
			result.Columns["Supplier"].Caption = "Поставщик";
			result.Columns["WriteTime"].Caption = "Дата и время";
			result.Columns["ClientName"].Caption = "Клиент";
			result.Columns["UserName"].Caption = "Пользователь";
			result.Columns["NameForm"].Caption = "Наименование и форма выпуска";
			result.Columns["Producer"].Caption = "Производитель";
			result.Columns["Cost"].Caption = "Цена";
			result.Columns["Quantity"].Caption = "Количество";
			result.Columns["Sum"].Caption = "Сумма";

			foreach (var row in data.Rows.Cast<DataRow>())
			{
				var resultRow = result.NewRow();
				resultRow["MatrixCode"] = row["MatrixCode"];
				resultRow["Supplier"] = row["Supplier"];
				resultRow["WriteTime"] = row["WriteTime"];
				resultRow["ClientName"] = row["ClientName"];
				resultRow["UserName"] = row["UserName"];
				resultRow["NameForm"] = row["NameForm"];
				resultRow["Producer"] = row["Producer"];
				resultRow["Cost"] = row["Cost"];
				resultRow["Quantity"] = row["Quantity"];
				resultRow["Sum"] = row["Summ"];
			result.Rows.Add(resultRow);
			}
		}
	}
}
