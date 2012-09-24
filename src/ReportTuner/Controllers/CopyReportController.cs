using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Common.MySql;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using MySql.Data.MySqlClient;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using ReportTuner.Helpers;
using ReportTuner.Models;
using ViewHelper = ReportTuner.Helpers.ViewHelper;

namespace ReportTuner.Controllers
{
	public class GeneralReportsFilter : PaginableSortable
	{
		public String ReportName { get; set; }
		public UInt64 GeneralReport { get; set; }
		public UInt64 Report { get; set; }


		public GeneralReportsFilter()
		{
			SortKeyMap = new Dictionary<string, string> {
				{ "Id", "Id" },
				{ "Comment", "Comment" }
			};
		}
		public IList<GeneralReport> Find()
		{
			var criteria = DetachedCriteria.For<GeneralReport>();
			criteria.CreateCriteria("Payer", "p", JoinType.InnerJoin);
			criteria.Add(Restrictions.Eq("Temporary", false));
			criteria.Add(Restrictions.Like("Comment", String.Format("%{0}%", ReportName)));
			var result = Find<GeneralReport>(criteria);
			return result;
		}
	}

	[Layout("MainLayout"),
	Helper(typeof(ViewHelper)),
	Helper(typeof(PaginatorHelper)),
	Helper(typeof(ReportAppHelper), "app")]
	public class CopyReportController : BaseController
	{
		public void SelectReport(ulong? rId, ulong? grId, [DataBind("filter")] GeneralReportsFilter filter)
		{
			PropertyBag["Reports"] = filter.Find();
			PropertyBag["filter"] = filter;
		}

		public void CopyReport(ulong? destId, [DataBind("filter")] GeneralReportsFilter filter)
		{
			if(destId == null || filter.Report == null)
				RedirectToUrl("../Reports/Reports.aspx?r=" + filter.GeneralReport);
			var sourceReport = DbSession.Query<Report>().FirstOrDefault(r => r.Id == filter.Report);
			if(sourceReport == null)
				return;

			var destReport = new Report {
				Enabled = sourceReport.Enabled,
				ReportCaption = String.Concat("Копия ", sourceReport.ReportCaption),
				ReportType = sourceReport.ReportType,
				GeneralReport = DbSession.Query<GeneralReport>().First(r => r.Id == destId)
			};
			using (new TransactionScope()) {
				DbSession.Save(destReport);
				DbSession.Flush();
			}
			ReportHelper.CopyReportProperties(sourceReport.Id, destReport.Id);
			RedirectToUrl("../Reports/Reports.aspx?r=" + destId);
		}
	}
}