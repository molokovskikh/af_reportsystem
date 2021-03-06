﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Common.Tools;

using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.ByOrders
{
	public class OrderOutAllowedAssortment : BaseOrdersReport
	{
		private uint _clientId;
		private Period _period;

		public OrderOutAllowedAssortment(MySqlConnection connection, DataSet dsProperties)
			: base(connection, dsProperties)
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_clientId = Convert.ToUInt32(GetReportParam("ClientCode"));
			_period = new Period(Begin, End);
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.Excel)
				return new SupplierExcelWriter();
			return null;
		}

		protected override BaseReportSettings GetSettings()
		{
			return new BaseReportSettings(ReportCode, ReportCaption);
		}

		protected override void GenerateReport()
		{
			DataAdapter.SelectCommand.CommandText = String.Format(@"
SELECT O.WriteTime,
CL.Name as ClientName,
U.Name as UserName,
Cat.Name as NameForm,
Prod.Name as Producer,
Ol.Cost, Ol.Quantity,
BM.Code as MatrixCode,
supps.Name AS Supplier,
(Ol.Cost*Ol.Quantity) as Summ
FROM {0}.OrdersHead O
	join {0}.OrdersList OL on OL.OrderId = O.RowId
		join usersettings.RetClientsSet RC on RC.ClientCode = O.ClientCode
join catalogs.Products P on OL.ProductID = P.Id
left join farm.BuyingMatrix BM on RC.BuyingMatrixPriceId = BM.PriceId and BM.ProductID = P.Id

and if(OL.CodeFirmCr is null, BM.ProducerId is null, BM.ProducerId is null || BM.ProducerId = ol.CodeFirmCr)

join Customers.Clients CL on CL.ID = O.ClientCode
join Customers.Users U on U.ID = O.UserID
join catalogs.Catalog Cat on Cat.Id = P.CatalogID
join usersettings.PricesData PD on O.PriceCode = PD.PriceCode
join Customers.Suppliers supps on PD.FirmCode = supps.Id
left join catalogs.Producers Prod on Prod.Id = Ol.CodeFirmCr

where O.ClientCode = ?ClientCode
and BM.ID is null and
O.WriteTime > ?begin
and O.WriteTime < ?end
and PD.IsLocal = 0
order by O.WriteTime", OrdersSchema);

// Если написать and BM.ID is NOT null and то будут выводится совпадающие позиции
// сейчас выводятся несовпадающие
			DataAdapter.SelectCommand.Parameters.AddWithValue("?ClientCode", _clientId);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?begin", _period.Begin);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?end", _period.End);
#if DEBUG
			Debug.WriteLine(DataAdapter.SelectCommand.CommandText);
#endif
			DataAdapter.Fill(_dsReport, "data");
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
			var headParameterCommand = Connection.CreateCommand();
			var shPCommand = "select CL.Name from Customers.Clients CL where CL.ID = " + _clientId.ToString();
			headParameterCommand.CommandText = shPCommand;
			var headParameterReader = headParameterCommand.ExecuteReader();
			result.Rows.Add("Клиент");

			if (headParameterReader.Read()) {
				result.Rows[1][2] = headParameterReader["Name"];
			}
			headParameterReader.Close();
			result.Rows.Add("Период: ");
			result.Rows[2][2] = "с " + _period.Begin.Date.ToShortDateString() + " по " + _period.End.Date.ToShortDateString();
			result.Rows.Add("");

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

			foreach (var row in data.Rows.Cast<DataRow>()) {
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