using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReportTuner.Helpers
{
	public class ViewHelper
	{
		public static string GetRowStyle(int rowIndex)
		{
			return rowIndex % 2 == 0 ? "EvenRow" : "OddRow";
		}
	}
}
