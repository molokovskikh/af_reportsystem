using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Common.Models;
using Common.MySql;
using Common.Tools;
using Common.Web.Ui.Models;
using ExecuteTemplate;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.Model;
using Inforoom.ReportSystem.ReportSettings;
using Inforoom.ReportSystem.Writers;
using Microsoft.Office.Interop.Excel;
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

	public class Grouping
	{
		public Grouping(string @group, Column[] columns)
		{
			Group = group;
			Columns = columns;
		}

		public string Group;
		public Column[] Columns;
		public string Join;
	}

	public class Column
	{
		public Column(string name, string caption, string sql) : this(name, caption, sql, true)
		{
		}

		public Column(string name, string caption, string sql, bool order)
		{
			Name = name;
			Caption = caption;
			Sql = sql;
			Order = order;
		}

		public string Name;
		public string Caption;
		public string Sql;
		public bool Order;
	}

	public class SupplierShareByUserExcelWriter : SupplierExcelWriter
	{
		public SupplierShareByUserExcelWriter()
		{
			CountDownRows = 7;
			HeaderCollumnCount = 5;
		}

		public override Range GetRangeForMerge(_Worksheet sheet, int rowCount)
		{
			if (rowCount != 4)
				return sheet.get_Range("A" + rowCount, "B" + rowCount);
			return sheet.get_Range("A" + rowCount, "F" + rowCount);
		}
	}

	//Доля поставщика в заказах аптек
	public class SupplierMarketShareByUser : OrdersReport
	{
		private uint _supplierId;
		private Period _period;
		private List<ulong> _regions;

		private Grouping[] groupings = new[] {
			new Grouping("oh.UserId",
				new[] {
					new Column("Empty", string.Empty, "''", false),
					new Column("ClientName", "Клиент", "c.Name"),
					new Column("UserName", "Пользователь", "ifnull(u.Name, CAST(u.Id AS CHAR))"),
				}),
			new Grouping("oh.AddressId",
				new[] {
					new Column("SupplierDeliveryId", "Код доставки", @"(select group_concat(distinct TI.SupplierDeliveryId order by TI.SupplierDeliveryId )
from reports.TempIntersection TI
where oh.AddressId = TI.AddressId)", false),
					new Column("ClientName", "Клиент", "c.Name"),
					new Column("AddressName", "Адрес", "a.Address"),
				}),
			new Grouping("oh.ClientCode",
				new[] {
					new Column("Empty", string.Empty, "''", false),
					new Column("ClientName", "Клиент", "c.Name"),
				}),
			new Grouping("a.LegalEntityId",
				new[] {
					new Column("SupplierClientId", "Код клиента",
						@"(select group_concat(distinct TI.SupplierClientId order by TI.SupplierClientId )
from reports.TempIntersection TI
where TI.LegalEntityId = a.LegalEntityId)", false),
					new Column("OrgName", "Юридическое лицо", "le.Name")
				})
		};

		private Grouping _grouping;

		public SupplierMarketShareByUser(ulong reportCode, string reportCaption, MySqlConnection connection, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, format, dsProperties)
		{
		}

		//Конструктор для тестирования
		public SupplierMarketShareByUser()
		{
		}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_supplierId = Convert.ToUInt32(getReportParam("SupplierId"));
			_period = new Period(dtFrom, dtTo);
			_regions = (List<ulong>)getReportParam("Regions");
			_grouping = groupings[Convert.ToInt32(getReportParam("Type"))];
		}

		protected override IWriter GetWriter(ReportFormats format)
		{
			if (format == ReportFormats.Excel)
				return new SupplierShareByUserExcelWriter();
			return null;
		}

		protected override BaseReportSettings GetSettings()
		{
			return new BaseReportSettings(ReportCode, ReportCaption);
		}

		public override void GenerateReport(ExecuteArgs e)
		{
			var connection = e.DataAdapter.SelectCommand.Connection;
			var supplierDeliveryIdSql = @"DROP TEMPORARY TABLE IF EXISTS reports.TempIntersection;
CREATE TEMPORARY TABLE reports.TempIntersection (
	AddressId INT unsigned,
	SupplierDeliveryId varchar(255),
	index(AddressId)
) engine=MEMORY;

insert into reports.TempIntersection(SupplierDeliveryId, AddressId)
SELECT ai.SupplierDeliveryId, ai.AddressId
FROM Customers.Intersection i
	JOIN Customers.AddressIntersection ai on ai.IntersectionId = i.Id
	JOIN Customers.Clients c ON c.Id = i.ClientId
	JOIN usersettings.RetClientsSet r ON r.clientcode = c.Id
	JOIN usersettings.PricesData pd ON pd.pricecode = i.PriceId
	JOIN Customers.Suppliers supplier ON supplier.Id = pd.firmcode
	JOIN usersettings.PricesRegionalData prd ON prd.regioncode = i.RegionId AND prd.pricecode = pd.pricecode
	JOIN usersettings.RegionalData rd ON rd.RegionCode = i.RegionId AND rd.FirmCode = pd.firmcode
WHERE  c.Status = 1
	and (supplier.RegionMask & i.RegionId) > 0
	and (c.maskregion & i.RegionId) > 0
	and (r.WorkRegionMask & i.RegionId) > 0
	and pd.agencyenabled = 1
	and pd.enabled = 1
	and prd.enabled = 1
	and i.AvailableForClient = 1
	and i.AgencyEnabled = 1
	and supplier.Id = ?supplierId
group by ai.AddressId, ai.SupplierDeliveryId";

			var supplierClientIdSql = @"DROP TEMPORARY TABLE IF EXISTS reports.TempIntersection;
CREATE TEMPORARY TABLE reports.TempIntersection (
	SupplierClientId varchar(255),
	LegalEntityId INT unsigned,
	index(LegalEntityId)
) engine=MEMORY;

insert into reports.TempIntersection(SupplierClientId, LegalEntityId)
SELECT i.SupplierClientId, i.LegalEntityId
FROM Customers.Intersection i
	JOIN Customers.Clients c ON c.Id = i.ClientId
	JOIN usersettings.RetClientsSet r ON r.clientcode = c.Id
	JOIN usersettings.PricesData pd ON pd.pricecode = i.PriceId
	JOIN Customers.Suppliers supplier ON supplier.Id = pd.firmcode
	JOIN usersettings.PricesRegionalData prd ON prd.regioncode = i.RegionId AND prd.pricecode = pd.pricecode
	JOIN usersettings.RegionalData rd ON rd.RegionCode = i.RegionId AND rd.FirmCode = pd.firmcode
WHERE  c.Status = 1
	and (supplier.RegionMask & i.RegionId) > 0
	and (c.maskregion & i.RegionId) > 0
	and (r.WorkRegionMask & i.RegionId) > 0
	and pd.agencyenabled = 1
	and pd.enabled = 1
	and prd.enabled = 1
	and i.AvailableForClient = 1
	and i.AgencyEnabled = 1
	and supplier.Id = ?supplierId
group by i.LegalEntityId, i.SupplierClientId";

			if (_grouping.Group.Match("a.LegalEntityId")) {
				connection.Execute(supplierClientIdSql, new { supplierId = _supplierId });
			}
			else if (_grouping.Group.Match("oh.AddressId")) {
				connection.Execute(supplierDeliveryIdSql, new { supplierId = _supplierId });
			}

			var userIds = connection.Read<uint>(String.Format(@"
select oh.UserId
from Orders.OrdersHead oh
where oh.WriteTime > ?begin
	and oh.WriteTime < ?end
	and oh.RegionCode in ({0})
group by oh.UserId", _regions.Implode()), new { begin = _period.Begin, end = _period.End })
				.ToArray();

			connection.Execute(@"
create temporary table Reports.UserPricesStat(
	UserId int unsigned not null,
	Count int unsigned not null,
	primary key (UserId)
) engine=memory;");
			foreach (var userId in userIds) {
				connection.Execute(@"
call Customers.GetActivePrices(?userId);
insert into Reports.UserPricesStat(UserId, Count)
select ?userId, count(*)
from Usersettings.ActivePrices;
drop temporary table IF EXISTS Usersettings.ActivePrices;", new { userId });
			}

			e.DataAdapter.SelectCommand.CommandText = String.Format(@"
select {2},
	sum(ol.Cost * ol.Quantity) as TotalSum,
	sum(if(pd.FirmCode = ?SupplierId, ol.Cost * ol.Quantity, 0)) as SupplierSum,
	group_concat(distinct us.UserId, us.Count) as SuppliersCount
from Orders.OrdersHead oh
	join Orders.OrdersList ol on ol.OrderId = oh.RowId
	join Customers.Clients c on c.Id = oh.ClientCode
		join Customers.Users u on u.ClientId = c.Id and oh.UserId = u.Id
			join Reports.UserPricesStat us on us.UserId = u.Id
	join Customers.Addresses a on a.Id = oh.AddressId
		join Billing.LegalEntities le on le.Id = a.LegalEntityId
	join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
	{4}
where oh.WriteTime > ?begin
and oh.WriteTime < ?end
and oh.RegionCode in ({0})
and pd.IsLocal = 0
group by {1}
order by {3}", _regions.Implode(), _grouping.Group,
				_grouping.Columns.Implode(c => String.Format("{0} as {1}", c.Sql, c.Name)),
				_grouping.Columns.Where(c => c.Order).Implode(c => c.Name),
				_grouping.Join);

			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SupplierId", _supplierId);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?begin", _period.Begin);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?end", _period.End);

#if DEBUG
			ProfileHelper.WriteLine(e.DataAdapter.SelectCommand);
#endif
			e.DataAdapter.Fill(_dsReport, "data");

			connection.Execute(@"
drop temporary table if exists reports.TempIntersection;
drop temporary table if exists reports.UserPricesStat;
");

			var data = _dsReport.Tables["data"];
			var result = _dsReport.Tables.Add("Results");
			foreach (var column in _grouping.Columns) {
				var dataColumn = result.Columns.Add(column.Name);
				dataColumn.Caption = column.Caption;
			}
			result.Columns.Add("Share", typeof(string));
			result.Columns.Add("SupplierSum", typeof(string));
			result.Columns.Add("SuppliersCount", typeof(string));

			var supplier = Session.Get<Supplier>(_supplierId);
			var regions = _regions
				.Select(id => Region.Find(Convert.ToUInt64(id)));

			result.Rows.Add("Поставщик: " + supplier.Name);
			result.Rows.Add("Период: c " + _period.Begin.Date + " по " + _period.End.Date);
			result.Rows.Add("Регионы: " + regions.Implode(r => r.Name));
			result.Rows.Add("Из отчета ИСКЛЮЧЕНЫ юр. лица, клиенты, адреса," +
				" по которым отсутствуют заказы на любых поставщиков за период формирования отчета");
			result.Rows.Add("");

			result.Columns["SupplierSum"].Caption = string.Format("Сумма по '{0}'", supplier.Name);
			result.Columns["Share"].Caption = string.Format("Доля '{0}', %", supplier.Name);
			result.Columns["SuppliersCount"].Caption = "Кол-во поставщиков";
			foreach (var row in data.Rows.Cast<DataRow>()) {
				var resultRow = result.NewRow();
				SetTotalSum(row, resultRow);
				resultRow["SuppliersCount"] = row["SuppliersCount"];
				foreach (var column in _grouping.Columns) {
					resultRow[column.Name] = row[column.Name];
					resultRow[column.Name] = row[column.Name];
				}
				result.Rows.Add(resultRow);
			}
		}

		public void SetTotalSum(DataRow dataRow, DataRow resultRow)
		{
			var total = Convert.ToDouble(dataRow["TotalSum"]);
			if (total <= 0)
				resultRow["Share"] = DBNull.Value;
			else {
				var supplierSum = Convert.ToDouble(dataRow["SupplierSum"]);
				var quota = Math.Round((supplierSum / total) * 100, 2);
				resultRow["Share"] = quota.ToString();
				resultRow["SupplierSum"] = supplierSum.ToString("C");
			}
		}
	}
}