using System;
using System.Collections;
using System.Data;
using System.Collections.Generic;

namespace Inforoom.ReportSystem.RatingReports
{
	public class RatingComparer : IComparer  
	{

		// Calls CaseInsensitiveComparer.Compare with the parameters reversed.
		int IComparer.Compare( Object x, Object y )  
		{
			return ( ((RatingField)x).position - ((RatingField)y).position );
		}

	}

	/// <summary>
	/// Summary description for RatingField.
	/// </summary>
	public class RatingField
	{
		public const string positionSuffix = "Position";
		public const string visibleSuffix = "Visible";
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

		//��������, ������� ����� ���� ����� primaryField
		public ulong[] equalValues = null;
		//��������, ������� �� ����� ���� ����� primaryField
		public ulong[] nonEqualValues = null;



		public RatingField(string PrimaryField, string ViewField, string OutputField, string Preffix, string OutputCaption)
		{
			primaryField = PrimaryField;
			viewField = ViewField;
			outputField = OutputField;
			reportPropertyPreffix = Preffix;
			outputCaption = OutputCaption;
			position = -1;
			visible = false;
		}

		public bool LoadFromDB(Inforoom.ReportSystem.RatingReport Parent)		
		{
			//���� Position � Visible ����������, �� ����� ������ ��������
			if (Parent.reportParamExists(reportPropertyPreffix + positionSuffix) && Parent.reportParamExists(reportPropertyPreffix + visibleSuffix))
			{
				position = (int)Parent.getReportParam(reportPropertyPreffix + positionSuffix);
				visible = (bool)Parent.getReportParam(reportPropertyPreffix + visibleSuffix);

				if (Parent.reportParamExists(reportPropertyPreffix + equalSuffix))
				{
					equalValues = ((List<ulong>)Parent.getReportParam(reportPropertyPreffix + equalSuffix)).ToArray();
				}

				if (Parent.reportParamExists(reportPropertyPreffix + nonEqualSuffix))
				{
					nonEqualValues = ((List<ulong>)Parent.getReportParam(reportPropertyPreffix + nonEqualSuffix)).ToArray();
				}

				return true;
			}
			else
				return false;

		}

		private string GetAllValues(Array al)
		{
			string Res = "( " + al.GetValue(0).ToString();
			for(int i = 1; i < al.Length; i++)
				Res = String.Concat(Res, ", ", al.GetValue(i).ToString());
			Res = String.Concat(Res, ")");
			return Res;
		}

		public string GetEqualValues()
		{
			return String.Format("({0} in {1})", primaryField, GetAllValues(equalValues));
		}

		public string GetNonEqualValues()
		{
			return String.Format("({0} not in {1})", primaryField, GetAllValues(nonEqualValues));
		}
	}
}
