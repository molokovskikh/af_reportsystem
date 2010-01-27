using System;
using System.Data;
using System.Data.OleDb;

namespace Inforoom.ReportSystem.Writers
{
	public class BaseExcelWriter
	{
		public const int MaxStringSize = 250;
		public const int MaxListName = 26;

		protected void DataTableToExcel(DataTable dtExport, string ExlFileName, ulong reportCode)
		{
			//Имя листа генерируем сами, а потом переименовываем, т.к. русские названия листов потом невозможно найти
			string generatedListName = "rep" + reportCode.ToString(); ;
			OleDbConnection ExcellCon = new OleDbConnection();
			try
			{
				ExcellCon.ConnectionString = @"
Provider=Microsoft.Jet.OLEDB.4.0;Password="""";User ID=Admin;Data Source=" + ExlFileName +
@";Mode=Share Deny None;Extended Properties=""Excel 8.0;HDR=no"";";
				string CreateSQL = "create table [" + generatedListName + "] (";
				for (int i = 0; i < dtExport.Columns.Count; i++)
				{
					CreateSQL += "[F" + (i + 1).ToString() + "] ";
					dtExport.Columns[i].ColumnName = "F" + (i + 1).ToString();
					if (dtExport.Columns[i].DataType == typeof(int))
						CreateSQL += " int";
					else
						if (dtExport.Columns[i].DataType == typeof(decimal))
							CreateSQL += " currency";
						else
							if (dtExport.Columns[i].DataType == typeof(double))
								CreateSQL += " real";
							else
								if ((dtExport.Columns[i].DataType == typeof(string)) && (dtExport.Columns[i].MaxLength > -1) && (dtExport.Columns[i].MaxLength <= MaxStringSize))
									CreateSQL += String.Format(" char({0})", MaxStringSize);
								else
									CreateSQL += " memo";
					if (i == dtExport.Columns.Count - 1)
						CreateSQL += ");";
					else
						CreateSQL += ",";
				}
				OleDbCommand cmd = new OleDbCommand(CreateSQL, ExcellCon);
				ExcellCon.Open();
				cmd.ExecuteNonQuery();
				OleDbDataAdapter daExcel = new OleDbDataAdapter("select * from [" + generatedListName + "]", ExcellCon);
				OleDbCommandBuilder cdExcel = new OleDbCommandBuilder(daExcel);
				cdExcel.QuotePrefix = "[";
				cdExcel.QuoteSuffix = "]";
				daExcel.Update(dtExport);
			}
			finally
			{
				ExcellCon.Close();
			}
		}
	}
}
