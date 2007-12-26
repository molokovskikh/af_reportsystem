using System;
using System.Collections;
using System.Data;
using System.Collections.Generic;

namespace Inforoom.ReportSystem.RatingReports
{
	/// <summary>
	/// Summary description for RatingField.
	/// </summary>
	public class RatingField
	{
		public const string positionSuffix = "Position";
		public const string equalSuffix = "Equal";
		public const string nonEqualSuffix = "NonEqual";

		//����, �� �������� ����� ������������� �������
		public string primaryField;
		//����, ������� ����� ������������ � �������
		public string viewField;
		//����, ������� ����� ���������� � �����
		public string outputField;
		//��, ��� ����� ����� ���������
		public string outputCaption;
		//������� ������� � ���������� ������
		public string reportPropertyPreffix;
		//������� � �������
		public int position;
		//����� �� ��� ���� ����� � �������
		public bool visible;
		//������ ������ ��� ����������� ��������� � ����������� �������� ����
		public string tableList;
		//������� ��� where, ������� ���������� � and, ��� ����������� ��������� � ����������� �������� ����. ����� ���� ���������������
		public string whereList;
		//������ ������� ��� ������ � Excel
		public int? width;

		//��������, ������� ����� ���� ����� primaryField
		public List<ulong> equalValues = null;
		//��������, ������� �� ����� ���� ����� primaryField
		public List<ulong> nonEqualValues = null;

		public string equalValuesCaption;
		public string nonEqualValuesCaption;



		public RatingField(string PrimaryField, string ViewField, string OutputField, string Preffix, string OutputCaption, string TableList, string WhereList, int DefaultPosition, string EqualValuesCaption, string NonEqualValuesCaption) :
			this(PrimaryField, ViewField, OutputField, Preffix, OutputCaption, TableList, WhereList, DefaultPosition, EqualValuesCaption, NonEqualValuesCaption, null)
		{
		}

		public RatingField(string PrimaryField, string ViewField, string OutputField, string Preffix, string OutputCaption, string TableList, string WhereList, int DefaultPosition, string EqualValuesCaption, string NonEqualValuesCaption, int? Width)
		{
			primaryField = PrimaryField;
			viewField = ViewField;
			outputField = OutputField;
			reportPropertyPreffix = Preffix;
			outputCaption = OutputCaption;
			position = DefaultPosition;
			visible = false;
			if (String.IsNullOrEmpty(TableList))
				throw new ArgumentException("�������� �� ����� ���� null ��� ������ �������.", "TableList");
			tableList = TableList;
			whereList = WhereList;
			equalValuesCaption = EqualValuesCaption;
			nonEqualValuesCaption = NonEqualValuesCaption;
			width = Width;
		}

		public bool LoadFromDB(BaseReport Parent)		
		{
			bool fieldIsSelected = false;

			//���� Position ����������, �� ����� �������� ������ ������������ � ��������� ������ � �� ����� ��������� ����� �����������
			if (Parent.reportParamExists(reportPropertyPreffix + positionSuffix))
			{
				position = (int)Parent.getReportParam(reportPropertyPreffix + positionSuffix);
				visible = true;
				fieldIsSelected = true;
			}

			if (Parent.reportParamExists(reportPropertyPreffix + equalSuffix))
			{
				equalValues = (List<ulong>)Parent.getReportParam(reportPropertyPreffix + equalSuffix);
				fieldIsSelected = true;
			}

			if (Parent.reportParamExists(reportPropertyPreffix + nonEqualSuffix))
			{
				nonEqualValues = (List<ulong>)Parent.getReportParam(reportPropertyPreffix + nonEqualSuffix);
				fieldIsSelected = true;
			}

			return fieldIsSelected;
		}

		private string GetAllValues(List<ulong> ValuesList)
		{
			string Res = "( " + ValuesList[0].ToString();
			for(int i = 1; i < ValuesList.Count; i++)
				Res = String.Concat(Res, ", ", ValuesList[i].ToString());
			Res = String.Concat(Res, ")");
			return Res;
		}

		public string GetEqualValues()
		{
			return String.Format("({0} in {1})", primaryField, GetAllValues(equalValues));
		}

		public string GetEqualValuesSQL()
		{
			return String.Format("select {0} from {1} where ({2} in {3}) {4} order by {5}",
				viewField, tableList, primaryField, GetAllValues(equalValues), whereList, outputField);
		}

		public string GetNonEqualValuesSQL()
		{
			return String.Format("select {0} from {1} where ({2} in {3}) {4} order by {5}",
				viewField, tableList, primaryField, GetAllValues(nonEqualValues), whereList, outputField);
		}

		public string GetNonEqualValues()
		{
			return String.Format("({0} not in {1})", primaryField, GetAllValues(nonEqualValues));
		}
	}
}
