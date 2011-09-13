using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.Tools;
using ExecuteTemplate;
using Inforoom.ReportSystem.Model;
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

	public class Grouping
	{
		public Grouping(string @group, Column[] columns)
		{
			Group = group;
			Columns = columns;
		}

		public string Group;
		public Column[] Columns;
	}

	public class Column
	{
		public Column(string name, string caption, string sql)
		{
			Name = name;
			Caption = caption;
			Sql = sql;
		}

		public string Name;
		public string Caption;
		public string Sql;
	}

	public class SupplierMarketShareByUser : OrdersReport
	{
		private uint _supplierId;
		private Period _period;
		private List<ulong> _regions;

		private Grouping[] groupings = new [] {
			new Grouping("oh.UserId",
 				new [] {
 					new Column("ClientName", "Клиент", "c.Name"), 
					new Column("UserName", "Пользователь", "ifnull(u.Name, CAST(u.Id AS CHAR))"), 
				}),
			new Grouping("oh.AddressId", 
				new [] {
					new Column("ClientName", "Клиент", "c.Name"),
					new Column("AddressName", "Адрес", "a.Address"),
				}),
			new Grouping("oh.ClientCode", 
				new [] {
					new Column("ClientName", "Клиент", "c.Name"), 
				}),
			new Grouping("a.LegalEntityId",
				new [] {
					new Column("OrgName", "Юридическое лицо", "le.Name"), 
				})
		};

		private Grouping _grouping;		

		public SupplierMarketShareByUser(ulong reportCode, string reportCaption, MySqlConnection connection, bool temporary, ReportFormats format, DataSet dsProperties) 
			: base(reportCode, reportCaption, connection, temporary, format, dsProperties)
		{}

		public override void ReadReportParams()
		{
			base.ReadReportParams();
			_supplierId = Convert.ToUInt32(getReportParam("SupplierId"));
			_period = new Period(dtFrom, dtTo);
			_regions = (List<ulong>) getReportParam("Regions");
			_grouping = groupings[Convert.ToInt32(getReportParam("Type"))];
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
select {2},
sum(ol.Cost * ol.Quantity) as TotalSum,
sum(if(pd.FirmCode = ?SupplierId, ol.Cost * ol.Quantity, 0)) as SupplierSum
from Orders.OrdersHead oh 
	join Orders.OrdersList ol on ol.OrderId = oh.RowId
	join Future.Clients c on c.Id = oh.ClientCode
		join Future.Users u on u.ClientId = c.Id and oh.UserId = u.Id
	join Future.Addresses a on a.Id = oh.AddressId
		join Billing.LegalEntities le on le.Id = a.LegalEntityId
	join Usersettings.PricesData pd on pd.PriceCode = oh.PriceCode
where oh.WriteTime > ?begin
and oh.WriteTime < ?end
and oh.RegionCode in ({0})
group by {1}
order by {3}",  _regions.Implode(), _grouping.Group,
				_grouping.Columns.Implode(c => String.Format("{0} as {1}", c.Sql, c.Name)),
				_grouping.Columns.Implode(c => c.Name));

			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?SupplierId", _supplierId);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?begin", _period.Begin);
			e.DataAdapter.SelectCommand.Parameters.AddWithValue("?end", _period.End);

#if DEBUG
			Debug.WriteLine(e.DataAdapter.SelectCommand.CommandText);
#endif
			e.DataAdapter.Fill(_dsReport, "data");
			var data = _dsReport.Tables["data"];
			var result = _dsReport.Tables.Add("Results");
			foreach (var column in _grouping.Columns)
			{
				var dataColumn = result.Columns.Add(column.Name);
				dataColumn.Caption = column.Caption;
			}
			result.Columns.Add("Share", typeof(double));

			var supplier = Supplier.Find(_supplierId);
			var regions = _regions
				.Select(id => Region.Find(Convert.ToUInt64(id)));

			result.Rows.Add("Поставщик: " + supplier.Name);
			result.Rows.Add("Период: c " + _period.Begin.Date + " по " + _period.End.Date);
			result.Rows.Add("Регионы: " + regions.Implode(r => r.Name));
			result.Rows.Add("");

			result.Columns["Share"].Caption = "Доля рынка, %";
			foreach (var row in data.Rows.Cast<DataRow>())
			{
				var resultRow = result.NewRow();
				var total = Convert.ToDouble(row["TotalSum"]);
				if (total == 0)
					resultRow["Share"] = DBNull.Value;
				else
					resultRow["Share"] = Math.Round((Convert.ToDouble(row["SupplierSum"])/total) * 100, 2);
				foreach (var column in _grouping.Columns)
				{
					resultRow[column.Name] = row[column.Name];
					resultRow[column.Name] = row[column.Name];
				}
				result.Rows.Add(resultRow);
			}
		}
	}
}