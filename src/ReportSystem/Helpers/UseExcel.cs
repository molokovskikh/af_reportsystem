using System;
using Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.Helpers
{
	public class UseExcel
	{
		public static void Workbook(string file, Action<Workbook> action)
		{
			Application exApp = new ApplicationClass();
			try
			{
				exApp.DisplayAlerts = false;
				var workbook = exApp.Workbooks.Open(file, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
					Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
					Type.Missing);
				_Worksheet worksheet;
				try
				{
					try
					{
						action(workbook);
					}
					finally
					{
						workbook.SaveAs(file, 56, Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
					}
				}
				finally
				{
					worksheet = null;
					workbook = null;
					try { exApp.Workbooks.Close(); }
					catch { }
				}
			}
			finally
			{
				try { exApp.Quit(); }
				catch { }
				exApp = null;
			}
		}
	}
}
