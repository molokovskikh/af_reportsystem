using System;
using System.IO;
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
				file = Path.GetFullPath(file);
				var workbook = exApp.Workbooks.Open(file);
				_Worksheet worksheet;
				try
				{
					try
					{
						action(workbook);
					}
					finally
					{
						workbook.SaveAs(file, FileFormat: 56);
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
