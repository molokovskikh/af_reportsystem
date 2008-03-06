using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Data;
using MySql.Data.MySqlClient;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using Inforoom.ReportSystem.RatingReports;
using ExecuteTemplate;
using System.Collections.Generic;
using System.Drawing;

namespace Inforoom.ReportSystem
{
	class MixedReport : RatingReport
	{
		private const string reportIntervalProperty = "ReportInterval";
		private const string byPreviousMonthProperty = "ByPreviousMonth";
		private const string sourceFirmCodeProperty = "SourceFirmCode";
		private const string businessRivalsProperty = "BusinessRivals";
		private const string showCodeProperty = "ShowCode";

		private List<RatingField> selectField;

		private DateTime dtFrom;
		private DateTime dtTo;
		private bool ByPreviousMonth;
		private int _reportInterval;
		//���������, �� �������� ����� ������������� �����
		private int sourceFirmCode;
		//������ ����������� ������� ����������
		private List<ulong> businessRivals;
		//������ ����������-����������� � ���� ������
		private string businessRivalsList;

		//���������� ���� Code �� �����-����� ����������?
		private bool showCode;

		//���� �� ����� "������������ ��������", "������ ������������", "������������"
		private RatingField nameField;
		//���� �������������
		private RatingField firmCrField;

		public MixedReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
		}

		public override void ReadReportParams()
		{
			FillRatingFields();
			filter = new List<string>();
			showCode = (bool)(bool)getReportParam(showCodeProperty);
			//List<string> s = businessRivals.ConvertAll<string>(delegate(ulong value) { return value.ToString(); });
			ByPreviousMonth = (bool)getReportParam(byPreviousMonthProperty);
			if (ByPreviousMonth)
			{
				dtTo = DateTime.Now;
				dtTo = dtTo.AddDays(-(dtTo.Day - 1)).Date;
				dtFrom = dtTo.AddMonths(-1).Date;
			}
			else
			{
				_reportInterval = (int)getReportParam(reportIntervalProperty);
				dtTo = DateTime.Now;
				//�� ������� ���� �������� �������� - ���� ������ ������
				dtFrom = dtTo.AddDays(-_reportInterval).Date;
				//� ������� ���� 00 ����� 00 ����� �������� ���������� ������� � �� � ����� �� ��������
				dtTo = dtTo.Date;
			}
			filter.Add(String.Format("������ ���: {0} - {1}", dtFrom.ToString("dd.MM.yyyy HH:mm:ss"), dtTo.ToString("dd.MM.yyyy HH:mm:ss")));

			sourceFirmCode = (int)getReportParam(sourceFirmCodeProperty);
			businessRivals = (List<ulong>)getReportParam(businessRivalsProperty);

			if (sourceFirmCode == 0)
				throw new Exception("�� ���������� �������� \"���������\".");

			if (businessRivals.Count == 0)
				throw new Exception("�� ���������� �������� \"������ �����������\".");

			List<string> s = businessRivals.ConvertAll<string>(delegate(ulong value) { return value.ToString(); });
			businessRivalsList = String.Join(", ", s.ToArray());

			selectField = new List<RatingField>();
			foreach (RatingField rf in allField)
			{
				if (rf.LoadFromDB(this))
					selectField.Add(rf);
			}

			if (!selectField.Exists(delegate(RatingField x) { return x.visible; }))
				throw new Exception("�� ������� ���� ��� ����������� � ��������� ������.");

			selectField.Sort(delegate(RatingField x, RatingField y) { return (x.position - y.position); });

			//�������� ����� ������ ����������� �� ���������
			RatingField firmCodeField = selectField.Find(delegate(RatingField value) { return value.reportPropertyPreffix == "FirmCode"; });
			if ((firmCodeField != null) && (firmCodeField.equalValues != null))
			{
				//���� � ������ ��������� �������� ��� ������������� ����������, �� ��������� ��� ����
				if (!firmCodeField.equalValues.Contains(Convert.ToUInt64(sourceFirmCode)))
					firmCodeField.equalValues.Add(Convert.ToUInt64(sourceFirmCode));

				//��� ������� ���������� �� ������ ����������� ���������: ���� �� �� � ������ ��������� ��������, ���� ���, �� ��������� ���
				businessRivals.ForEach(delegate(ulong value) { if (!firmCodeField.equalValues.Contains(value)) firmCodeField.equalValues.Add(value); });
			}

			//���������, ��� ������������ ������ ���� "�������������"
			firmCrField = selectField.Find(delegate(RatingField value) { return value.reportPropertyPreffix == "FirmCr"; });

			List<RatingField> nameFields = selectField.FindAll(delegate(RatingField value) 
				{
					return (value.reportPropertyPreffix == "ProductName") || (value.reportPropertyPreffix == "FullName") || (value.reportPropertyPreffix == "ShortName"); 
				});
			if (nameFields.Count == 0)
				throw new Exception("�� ����� \"������������ ��������\", \"������ ������������\", \"������������\" �� ������� �� ���� ����.");
			else
				if (nameFields.Count > 1)
					throw new Exception("�� ����� \"������������ ��������\", \"������ ������������\", \"������������\" ������ ���� ������� ������ ���� ����.");
				else
					nameField = nameFields[0];

		}

		public override void GenerateReport(ExecuteArgs e)
		{
			filter.Add(String.Format("��������� ��������� : {0}", GetValuesFromSQL(e, "select concat(cd.ShortName, ' - ', rg.Region) as FirmShortName from usersettings.clientsdata cd, farm.regions rg where rg.RegionCode = cd.RegionCode and cd.FirmCode = " + sourceFirmCode)));
			filter.Add(String.Format("������ �����������-����������� : {0}", GetValuesFromSQL(e, "select concat(cd.ShortName, ' - ', rg.Region) as FirmShortName from usersettings.clientsdata cd, farm.regions rg  where rg.RegionCode = cd.RegionCode and cd.FirmCode in (" + businessRivalsList + ") order by cd.ShortName")));

			if (showCode)
			{
				e.DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS ProviderCodes;
create temporary table ProviderCodes (Code varchar(20), CatalogCode int unsigned, codefirmcr int unsigned,
key Code(Code), key CatalogCode(CatalogCode), key CodeFirmCr(CodeFirmCr)) engine=MEMORY;
insert into ProviderCodes "
					+
					"select ol.Code, " + nameField.primaryField + ((firmCrField != null) ? ", " + firmCrField.primaryField : ", null ") +
					@"
from 
  orders.OrdersHead oh, 
  orders.OrdersList ol,
  catalogs.products p,
  catalogs.catalog c,
  catalogs.catalognames cn, 
  farm.CatalogFirmCr cfc, 
  usersettings.pricesdata pd 
where 
    ol.OrderID = oh.RowID 
and oh.deleted = 0
and oh.processed = 1
and ol.Junk = 0
and ol.Await = 0
and p.Id = ol.ProductId
and c.Id = p.CatalogId
and cn.id = c.NameId
and cfc.CodeFirmCr = if(ol.CodeFirmCr is not null, ol.CodeFirmCr, 1)
and pd.PriceCode = oh.PriceCode
and pd.FirmCode = " + sourceFirmCode.ToString() +
					" and oh.WriteTime > '" + dtFrom.ToString(MySQLDateFormat) + "' " +
					" and oh.WriteTime < '" + dtTo.ToString(MySQLDateFormat) + "' " +
					" group by " + nameField.primaryField + ((firmCrField != null) ? ", " + firmCrField.primaryField : String.Empty);

				e.DataAdapter.SelectCommand.ExecuteNonQuery();
			}

			string SelectCommand = "select ";

			foreach (RatingField rf in selectField)
				if (rf.visible)
					SelectCommand = String.Concat(SelectCommand, rf.primaryField, ", ", rf.viewField, ", ");

			if (showCode)
				SelectCommand += " ProviderCodes.Code, ";

			SelectCommand = String.Concat(SelectCommand, String.Format(@"
sum(if(pd.firmcode = {0}, ol.cost*ol.quantity, NULL)) as SourceFirmCodeSum,
sum(if(pd.firmcode = {0}, ol.quantity, NULL)) SourceFirmCodeRows,
Min(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeMinCost,
Avg(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeAvgCost,
Max(if(pd.firmcode = {0}, ol.cost, NULL)) as SourceFirmCodeMaxCost,
Count(distinct if(pd.firmcode = {0}, oh.RowId, NULL)) as SourceFirmDistinctOrderId,
Count(distinct if(pd.firmcode = {0}, oh.ClientCode, NULL)) as SourceFirmDistinctClientCode,

sum(if(pd.firmcode in ({1}), ol.cost*ol.quantity, NULL)) as RivalsSum,
sum(if(pd.firmcode in ({1}), ol.quantity, NULL)) RivalsRows,
Min(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsMinCost,
Avg(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsAvgCost,
Max(if(pd.firmcode in ({1}), ol.cost, NULL)) as RivalsMaxCost,
Count(distinct if(pd.firmcode in ({1}), oh.RowId, NULL)) as RivalsDistinctOrderId,
Count(distinct if(pd.firmcode in ({1}), oh.ClientCode, NULL)) as RivalsDistinctClientCode,

sum(ol.cost*ol.quantity) as AllSum,
sum(ol.quantity) AllRows,
Min(ol.cost) as AllMinCost,
Avg(ol.cost) as AllAvgCost,
Max(ol.cost) as AllMaxCost,
Count(distinct oh.RowId) as AllDistinctOrderId,
Count(distinct oh.ClientCode) as AllDistinctClientCode ", sourceFirmCode, businessRivalsList));
			SelectCommand = String.Concat(
				SelectCommand, @"
from 
  (
  orders.OrdersHead oh, 
  orders.OrdersList ol,
  catalogs.products p,
  catalogs.catalog c,
  catalogs.catalognames cn,
  catalogs.catalogforms cf, 
  farm.CatalogFirmCr cfc, 
  usersettings.clientsdata cd,
  usersettings.retclientsset rcs, 
  farm.regions rg, 
  usersettings.pricesdata pd, 
  usersettings.clientsdata prov,
  billing.payers 
  )" +
	((showCode) ? " left join ProviderCodes on ProviderCodes.CatalogCode = " + nameField.primaryField + (((firmCrField != null) ? " and ProviderCodes.CodeFirmCr = " + firmCrField.primaryField : String.Empty)) : String.Empty) +
@"
where 
    ol.OrderID = oh.RowID 
and oh.deleted = 0
and oh.processed = 1
and ol.Junk = 0
and ol.Await = 0
and p.Id = ol.ProductId
and c.Id = p.CatalogId
and cn.id = c.NameId
and cf.Id = c.FormId
and cfc.CodeFirmCr = if(ol.CodeFirmCr is not null, ol.CodeFirmCr, 1) 
and cd.FirmCode = oh.ClientCode
and cd.BillingCode <> 921
and payers.PayerId = cd.BillingCode
and rcs.ClientCode = oh.ClientCode
and rcs.InvisibleOnFirm < 2 
and rg.RegionCode = oh.RegionCode 
and pd.PriceCode = oh.PriceCode 
and prov.FirmCode = pd.FirmCode");

			foreach (RatingField rf in selectField)
			{
				if ((rf.equalValues != null) && (rf.equalValues.Count > 0))
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetEqualValues());
					filter.Add(String.Format("{0}: {1}", rf.equalValuesCaption, GetValuesFromSQL(e, rf.GetEqualValuesSQL())));
				}
				if ((rf.nonEqualValues != null) && (rf.nonEqualValues.Count > 0))
				{
					SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "and ", rf.GetNonEqualValues());
					filter.Add(String.Format("{0}: {1}", rf.nonEqualValuesCaption, GetValuesFromSQL(e, rf.GetNonEqualValuesSQL())));
				}
			}

			SelectCommand = String.Concat(SelectCommand, String.Format(Environment.NewLine + "and (oh.WriteTime > '{0}')", dtFrom.ToString(MySQLDateFormat)));
			SelectCommand = String.Concat(SelectCommand, String.Format(Environment.NewLine + "and (oh.WriteTime < '{0}')", dtTo.ToString(MySQLDateFormat)));

			//��������� ����������� � ����������
			List<string> GroupByList = new List<string>();
			List<string> OrderByList = new List<string>();
			foreach (RatingField rf in selectField)
				if (rf.visible)
				{
					GroupByList.Add(rf.primaryField);
					OrderByList.Add(rf.outputField);
				}
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "group by ", String.Join(",", GroupByList.ToArray()));
			SelectCommand = String.Concat(SelectCommand, Environment.NewLine + "order by ", String.Join(",", OrderByList.ToArray()));

#if DEBUG
			Debug.WriteLine(SelectCommand);
#endif

			DataTable SelectTable = new DataTable();

			e.DataAdapter.SelectCommand.CommandText = SelectCommand;
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.Fill(SelectTable);

			System.Data.DataTable res = new System.Data.DataTable();
			DataColumn dc;

			if (showCode)
			{
				dc = res.Columns.Add("Code", typeof(System.String));
				dc.Caption = "���";
			}

			foreach (RatingField rf in selectField)
			{
				if (rf.visible)
				{
					dc = res.Columns.Add(rf.outputField, SelectTable.Columns[rf.outputField].DataType);
					dc.Caption = rf.outputCaption;
					if (rf.width.HasValue)
						dc.ExtendedProperties.Add("Width", rf.width);
				}
			}

			dc = res.Columns.Add("SourceFirmCodeSum", typeof(System.Decimal));
			dc.Caption = "����� �� ����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc = res.Columns.Add("SourceFirmCodeRows", typeof(System.Int32));
			dc.Caption = "���-�� �� ���������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc = res.Columns.Add("SourceFirmCodeMinCost", typeof(System.Decimal));
			dc.Caption = "����������� ���� �� ����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc = res.Columns.Add("SourceFirmCodeAvgCost", typeof(System.Decimal));
			dc.Caption = "������� ���� �� ����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc = res.Columns.Add("SourceFirmCodeMaxCost", typeof(System.Decimal));
			dc.Caption = "������������ ���� �� ����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc = res.Columns.Add("SourceFirmDistinctOrderId", typeof(System.Int32));
			dc.Caption = "���-�� ������ �� ��������� �� ����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));
			dc = res.Columns.Add("SourceFirmDistinctClientCode", typeof(System.Int32));
			dc.Caption = "���-�� ��������, ���������� ��������, �� ���������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(197, 217, 241));

			dc = res.Columns.Add("RivalsSum", typeof(System.Decimal));
			dc.Caption = "����� �� �����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc = res.Columns.Add("RivalsRows", typeof(System.Int32));
			dc.Caption = "���-�� �� �����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc = res.Columns.Add("RivalsMinCost", typeof(System.Decimal));
			dc.Caption = "����������� ���� �� �����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc = res.Columns.Add("RivalsAvgCost", typeof(System.Decimal));
			dc.Caption = "������� ���� �� �����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc = res.Columns.Add("RivalsMaxCost", typeof(System.Decimal));
			dc.Caption = "������������ ���� �� �����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc = res.Columns.Add("RivalsDistinctOrderId", typeof(System.Int32));
			dc.Caption = "���-�� ������ �� ��������� �� �����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));
			dc = res.Columns.Add("RivalsDistinctClientCode", typeof(System.Int32));
			dc.Caption = "���-�� ��������, ���������� ��������, �� �����������";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(234, 241, 221));

			dc = res.Columns.Add("AllSum", typeof(System.Decimal));
			dc.Caption = "����� �� ����";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc = res.Columns.Add("AllRows", typeof(System.Int32));
			dc.Caption = "���-�� �� ����";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc = res.Columns.Add("AllMinCost", typeof(System.Decimal));
			dc.Caption = "����������� ���� �� ����";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc = res.Columns.Add("AllAvgCost", typeof(System.Decimal));
			dc.Caption = "������� ���� �� ����";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc = res.Columns.Add("AllMaxCost", typeof(System.Decimal));
			dc.Caption = "������������ ���� �� ����";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc = res.Columns.Add("AllDistinctOrderId", typeof(System.Int32));
			dc.Caption = "���-�� ������ �� ��������� �� ����";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));
			dc = res.Columns.Add("AllDistinctClientCode", typeof(System.Int32));
			dc.Caption = "���-�� ��������, ���������� ��������, �� ����";
			dc.ExtendedProperties.Add("Color", Color.FromArgb(253, 233, 217));

			DataRow newrow;
			try
			{
				int visbleCount = selectField.FindAll(delegate(RatingField x) { return x.visible; }).Count;
				res.BeginLoadData();
				foreach (DataRow dr in SelectTable.Rows)
				{
					newrow = res.NewRow();

					foreach (RatingField rf in selectField)
						if (rf.visible)
							newrow[rf.outputField] = dr[rf.outputField];

					for (int i = (visbleCount * 2); i < SelectTable.Columns.Count; i++)
					{
						if (!(dr[SelectTable.Columns[i].ColumnName] is DBNull))
							newrow[SelectTable.Columns[i].ColumnName] = Convert.ChangeType(dr[SelectTable.Columns[i].ColumnName], res.Columns[SelectTable.Columns[i].ColumnName].DataType);
					}

					res.Rows.Add(newrow);
				}
			}
			finally
			{
				res.EndLoadData();
			}

			//��������� ��������� ������ �����, ����� ����� ������� � ��� �������� ������� � Excel
			for (int i = 0; i < filter.Count; i++)
				res.Rows.InsertAt(res.NewRow(), 0);

			res = res.DefaultView.ToTable();
			res.TableName = "Results";
			_dsReport.Tables.Add(res);
		}
	}
}
