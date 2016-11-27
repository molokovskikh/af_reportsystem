using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using Common.Models;
using Common.MySql;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
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
	public class SupplierMarketShareByUser : BaseOrdersReport
	{
		private uint _supplierId;
		private List<ulong> _regions;

		private Grouping[] groupings = {
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

		public SupplierMarketShareByUser(MySqlConnection connection, DataSet dsProperties)
			: base(connection, dsProperties)
		{
		}

		//Конструктор для тестирования
		public SupplierMarketShareByUser()
		{
		}

		[Description("Показывать колонку \"Сумма по всем поставщикам\"")]
		public bool ShowAllSum { get; set; }

		private decimal? _shareMoreThan;

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_supplierId = Convert.ToUInt32(GetReportParam("SupplierId"));
			_regions = (List<ulong>)GetReportParam("Regions");
			_grouping = groupings[Convert.ToInt32(GetReportParam("Type"))];
			if (_reportParams.ContainsKey("ShareMoreThan"))
				_shareMoreThan = (decimal)GetReportParam("ShareMoreThan");
		}

		protected override void GenerateReport()
		{
			var connection = DataAdapter.SelectCommand.Connection;
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
	and oh.Deleted = 0
	and oh.Submited = 1
	and oh.RegionCode in ({0})
group by oh.UserId", _regions.Implode()), new { begin = Begin, end = End })
				.ToArray();

			connection.Execute(@"
drop temporary table if exists Reports.UserStat;
create temporary table Reports.UserStat(
	UserId int unsigned not null,
	RequestCount int unsigned not null,
	primary key (UserId)
) engine=memory;

drop temporary table if exists Reports.UserAllStat;
create temporary table Reports.UserAllStat(
	Id int auto_increment primary key,
	UserId int unsigned not null,
	RequestCount int unsigned not null
) engine=memory;");
			if (userIds.Length > 0) {
				connection.Execute(String.Format(@"
insert into Reports.UserStat(UserId, RequestCount)
select l.UserId, count(*)
from Logs.AnalitfUpdates l
where l.UserId in ({0})
	and l.UpdateType in (4, 11)
	and l.RequestTime > ?begin
	and l.RequestTime < ?end
group by l.UserId;

insert into Reports.UserAllStat(UserId, RequestCount)
	select r.UserId, count(*)
	from Logs.RequestLogs r
	where r.Userid in ({0})
		and r.CreatedOn > ?begin
		and r.CreatedOn < ?end
		and r.IsCompleted = 1
		and r.IsFaulted = 0
		and r.UpdateType = 'OrdersController'
	group by r.UserId
union all
	select *
	from Reports.UserStat s
	group by s.UserId", userIds.Implode()), new {begin = Begin, end = End});
			}

			DataAdapter.SelectCommand.CommandText = String.Format(@"
drop temporary table if exists Reports.KeyToUser;
create temporary table Reports.KeyToUser(
	GroupKey int unsigned not null,
	UserId int unsigned not null,
	primary key(GroupKey, UserId)
) engine=memory;

insert into Reports.KeyToUser(GroupKey, UserId)
select {1}, UserId
from Orders.OrdersHead oh
	join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
	join Customers.Clients c on c.Id = oh.ClientCode
	join Customers.Users u on u.ClientId = c.Id and oh.UserId = u.Id
	join Customers.Addresses a on a.Id = oh.AddressId
		join Billing.LegalEntities le on le.Id = a.LegalEntityId
where oh.WriteTime > ?begin
	and oh.WriteTime < ?end
	and oh.Deleted = 0
	and oh.Submited = 1
	and oh.RegionCode in ({0})
	and pd.IsLocal = 0
group by {1}, UserId;

drop temporary table if exists Reports.KeyToCount;
create temporary table Reports.KeyToCount(
	GroupKey int unsigned not null,
	RequestCount int unsigned not null,
	primary key(GroupKey)
) engine=memory;

insert into Reports.KeyToCount(GroupKey, RequestCount)
	select k.GroupKey, sum(s.RequestCount)
	from Reports.KeyToUser k
		join Reports.UserAllStat s on s.UserId = k.UserId
	group by k.GroupKey;

drop temporary table if exists Reports.PreResult;
create temporary table Reports.PreResult(
	GroupKey int unsigned not null,
	{6},
	TotalSum decimal(12, 2),
	SupplierSum decimal(12, 2),
	SuppliersCount int unsigned,
	LastOrder time,
	primary key(GroupKey)
) engine=memory;

insert into Reports.PreResult(GroupKey, {5}, TotalSum, SupplierSum, SuppliersCount, LastOrder)
select {1} as GroupKey,
	{2},
	sum(ol.Cost * ol.Quantity) as TotalSum,
	sum(if(pd.FirmCode = ?SupplierId, ol.Cost * ol.Quantity, 0)) as SupplierSum,
	count(distinct pd.FirmCode) as SuppliersCount,
	time(min(oh.WriteTime)) as LastOrder
from Orders.OrdersHead oh
	join Orders.OrdersList ol on ol.OrderId = oh.RowId
	join Customers.Clients c on c.Id = oh.ClientCode
	join Customers.Users u on u.ClientId = c.Id and oh.UserId = u.Id
	join Customers.Addresses a on a.Id = oh.AddressId
		join Billing.LegalEntities le on le.Id = a.LegalEntityId
	join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
	{4}
where oh.WriteTime > ?begin
	and oh.WriteTime < ?end
	and oh.Deleted = 0
	and oh.Submited = 1
	and oh.RegionCode in ({0})
	and pd.IsLocal = 0
group by {1}
order by {3};

drop temporary table if exists Reports.PrevResult;
create temporary table Reports.PrevResult(
	GroupKey int unsigned not null,
	TotalSum decimal(12, 2),
	SupplierSum decimal(12, 2),
	primary key(GroupKey)
) engine=memory;

insert into Reports.PrevResult(GroupKey, TotalSum, SupplierSum)
select {1} as GroupKey,
	sum(ol.Cost * ol.Quantity) as TotalSum,
	sum(if(pd.FirmCode = ?SupplierId, ol.Cost * ol.Quantity, 0)) as SupplierSum
from Orders.OrdersHead oh
	join Orders.OrdersList ol on ol.OrderId = oh.RowId
	join Customers.Clients c on c.Id = oh.ClientCode
	join Customers.Users u on u.ClientId = c.Id and oh.UserId = u.Id
	join Customers.Addresses a on a.Id = oh.AddressId
		join Billing.LegalEntities le on le.Id = a.LegalEntityId
	join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
	{4}
where oh.WriteTime > ?prevBegin
	and oh.WriteTime < ?prevEnd
	and oh.Deleted = 0
	and oh.Submited = 1
	and oh.RegionCode in ({0})
	and pd.IsLocal = 0
group by {1};

select r.*, k.RequestCount as OrderSendRequestCount, pr.SupplierSum as PrevSupplierSum, pr.TotalSum as PrevTotalSum
from Reports.PreResult r
left join Reports.KeyToCount k on k.GroupKey = r.GroupKey
left join Reports.PrevResult pr on pr.GroupKey = r.GroupKey;",
				_regions.Implode(),
				_grouping.Group,
				_grouping.Columns.Implode(c => $"{c.Sql} as {c.Name}"),
				_grouping.Columns.Where(c => c.Order).Implode(c => c.Name),
				_grouping.Join,
				_grouping.Columns.Implode(c => c.Name),
				_grouping.Columns.Implode(c => $"{c.Name} varchar(255)"));

			var prevBegin = Begin.AddDays(-(int)(End - Begin).TotalDays);
			var prevEnd = Begin;
			if (ReportPeriod == ReportPeriod.ByToday) {
				prevBegin = Begin.AddDays(-1);
				prevEnd = End.AddDays(-1);
			} else if (ReportPeriod == ReportPeriod.ByPrevMonth) {
				prevBegin = Begin.AddMonths(-1);
				prevEnd = End.AddMonths(-1);
			}

			DataAdapter.SelectCommand.Parameters.AddWithValue("?SupplierId", _supplierId);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?begin", Begin);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?end", End);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?prevBegin", prevBegin);
			DataAdapter.SelectCommand.Parameters.AddWithValue("?prevEnd", prevEnd);

#if DEBUG
			ProfileHelper.WriteLine(DataAdapter.SelectCommand);
#endif
			DataAdapter.Fill(_dsReport, "data");

			connection.Execute(@"

drop temporary table if exists reports.TempIntersection;
drop temporary table if exists reports.UserStat;
drop temporary table if exists reports.UserAllStat;
drop temporary table if exists Reports.KeyToCount;
drop temporary table if exists Reports.KeyToUser;
drop temporary table if exists Reports.PreResult;
");

			var data = _dsReport.Tables["data"];
			var result = _dsReport.Tables.Add("Results");
			foreach (var column in _grouping.Columns) {
				var dataColumn = result.Columns.Add(column.Name);
				dataColumn.Caption = column.Caption;
			}
			result.Columns.Add("Share", typeof(string));
			result.Columns.Add("ShareDiff", typeof(string));
			result.Columns.Add("SupplierSum", typeof(string));
			if (ShowAllSum)
				result.Columns.Add("TotalSum", typeof(string));
			result.Columns.Add("SuppliersCount", typeof(string));
			result.Columns.Add("OrderSendRequestCount", typeof(string));
			result.Columns.Add("LastOrder", typeof(string));

			var supplier = Session.Get<Supplier>(_supplierId);
			var regions = _regions
				.Select(id => Session.Load<global::Common.Models.Region>(Convert.ToUInt64(id)));

			Header.Add("Поставщик: " + supplier.Name);
			Header.Add("Регионы: " + regions.Implode(r => r.Name));
			if (_shareMoreThan.HasValue) {
				Header.Add($"Из отчета ИСКЛЮЧЕНЫ юр. лица, клиенты, адреса, по которым доля НЕ превышает {_shareMoreThan.Value}%");
			}
			else {
				Header.Add("Из отчета ИСКЛЮЧЕНЫ юр. лица, клиенты, адреса," +
									" по которым отсутствуют заказы на любых поставщиков за период формирования отчета");
			}
			Header.Add("");

			result.Columns["SupplierSum"].Caption = $"Сумма по '{supplier.Name}'";
			if (ShowAllSum)
				result.Columns["TotalSum"].Caption = "Сумма по всем поставщикам";
			result.Columns["Share"].Caption = $"Доля '{supplier.Name}', %";
			result.Columns["ShareDiff"].Caption = "Изменение доли";
			result.Columns["SuppliersCount"].Caption = "Кол-во поставщиков";
			result.Columns["OrderSendRequestCount"].Caption = "Кол-во сессий отправки заказов";
			result.Columns["LastOrder"].Caption = "Самая поздняя заявка";
			foreach (var row in data.Rows.Cast<DataRow>()) {
				var resultRow = result.NewRow();
				var share = SetTotalSum(row, resultRow);
				resultRow["SuppliersCount"] = row["SuppliersCount"];
				resultRow["LastOrder"] = row["LastOrder"];
				resultRow["OrderSendRequestCount"] = row["OrderSendRequestCount"];
				if (ShowAllSum)
					resultRow["TotalSum"] = row["TotalSum"];
				foreach (var column in _grouping.Columns) {
					resultRow[column.Name] = row[column.Name];
					resultRow[column.Name] = row[column.Name];
				}
				if (!_shareMoreThan.HasValue || share > _shareMoreThan.Value)
					result.Rows.Add(resultRow);
			}
			var emptyRowCount = EmptyRowCount;
			for (var i = 0; i < emptyRowCount; i++)
				result.Rows.InsertAt(result.NewRow(), 0);
		}

		public decimal SetTotalSum(DataRow dataRow, DataRow resultRow)
		{
			var share = 0m;
			var total = Convert.ToDecimal(dataRow["TotalSum"]);
			if (total <= 0)
				resultRow["Share"] = DBNull.Value;
			else {
				var supplierSum = Convert.ToDecimal(dataRow["SupplierSum"]);
				share = Math.Round(supplierSum / total * 100, 2);
				resultRow["Share"] = share.ToString();
				resultRow["SupplierSum"] = supplierSum.ToString("C");

				var prevTotalSum = dataRow["PrevTotalSum"] is DBNull ? null : (decimal?)Convert.ToDecimal(dataRow["PrevTotalSum"]);
				var prevSum = dataRow["PrevSupplierSum"] is DBNull ? null : (decimal?)Convert.ToDecimal(dataRow["PrevSupplierSum"]);
				if (prevTotalSum > 0) {
					var prevShare = NullableHelper.Round(prevSum / prevTotalSum * 100, 2);
					resultRow["ShareDiff"] = (share - prevShare).ToString();
				}
			}
			return share;
		}
	}
}