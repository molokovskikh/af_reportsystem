using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common.Web.Ui.Helpers;

namespace ReportTuner.Helpers
{
	public class ReportAppHelper : AppHelper
	{
		public override bool HavePermission(string controller, string action)
		{
			return true;
		}
	}
}