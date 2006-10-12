using System;
using System.Collections;
using System.Data;

namespace Inforoom.RatingReport
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
		//�������� ���� "����� ������" � �������
		public const string colReportCode = "Reports_ReportCode";
		//�������� ���� "������������ ��������" � �������
		public const string colPropertyName = "PropertyName";
		//�������� ���� "�������� ��������" � �������
		public const string colPropertyValue = "PropertyValue";

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
		public int[] equalValues = null;
		//��������, ������� �� ����� ���� ����� primaryField
		public int[] nonEqualValues = null;



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

		public bool LoadFromDB(DataTable dtProperties)
		{
			DataRow[] dr = dtProperties.Select(String.Format("{0} like '{1}*'", colPropertyName, reportPropertyPreffix));
			if (dr.Length > 0)
			{
				dr = dtProperties.Select(String.Format("{0} like '{1}{2}'", colPropertyName, reportPropertyPreffix, positionSuffix));
				if (dr.Length == 1)
					position = Convert.ToInt32(dr[0][colPropertyValue]);
				else
					throw new Exception(String.Format("���-�� �������� {0} �� ����� 1 ({1})", positionSuffix, dr.Length));

				dr = dtProperties.Select(String.Format("{0} like '{1}{2}'", colPropertyName, reportPropertyPreffix, visibleSuffix));
				if (dr.Length == 1)
					visible = ("1" == dr[0][colPropertyValue].ToString() ? true : false);
				else
					throw new Exception(String.Format("���-�� �������� {0} �� ����� 1 ({1})", visibleSuffix, dr.Length));

				dr = dtProperties.Select(String.Format("{0} like '{1}{2}'", colPropertyName, reportPropertyPreffix, equalSuffix));
				if (dr.Length > 0)
				{
					equalValues = new int[dr.Length];
					for(int i = 0; i<dr.Length; i++)
						equalValues[i] = Convert.ToInt32(dr[i][colPropertyValue]);
				}

				dr = dtProperties.Select(String.Format("{0} like '{1}{2}'", colPropertyName, reportPropertyPreffix, nonEqualSuffix));
				if (dr.Length > 0)
				{
					nonEqualValues = new int[dr.Length];
					for(int i = 0; i<dr.Length; i++)
						nonEqualValues[i] = Convert.ToInt32(dr[i][colPropertyValue]);
				}

				return true;

			}
			else
				return false;
		}

		private string GetAllValues(Array al)
		{
			string Res = "( " + ((int)(al.GetValue(0))).ToString();
			for(int i = 1; i < al.Length; i++)
				Res = String.Concat(Res, ", ", ((int)(al.GetValue(i))).ToString());
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
