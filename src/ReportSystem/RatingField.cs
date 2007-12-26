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

		//Поле, по которому будет производиться выборка
		public string primaryField;
		//Поле, которое будет отображаться в запросе
		public string viewField;
		//Поле, которое будет выбираться в отчет
		public string outputField;
		//То, что будет видно заказчику
		public string outputCaption;
		//Префикс свойств в параметрах отчета
		public string reportPropertyPreffix;
		//Позиция в запросе
		public int position;
		//Будет ли это поле видно в запросе
		public bool visible;
		//Список таблиц для отображения выбранных и исключенных значений поля
		public string tableList;
		//условие для where, которое начинается с and, для отображения выбранных и исключенных значений поля. Может быть неустановленным
		public string whereList;
		//ширина колонки при выводе в Excel
		public int? width;

		//Значения, которым может быть равно primaryField
		public List<ulong> equalValues = null;
		//Значения, которым не может быть равно primaryField
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
				throw new ArgumentException("Параметр не может быть null или пустой строкой.", "TableList");
			tableList = TableList;
			whereList = WhereList;
			equalValuesCaption = EqualValuesCaption;
			nonEqualValuesCaption = NonEqualValuesCaption;
			width = Width;
		}

		public bool LoadFromDB(BaseReport Parent)		
		{
			bool fieldIsSelected = false;

			//Если Position существует, то тогда параметр должен отображаться в заголовке отчета и по этому параметру будет группировка
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
