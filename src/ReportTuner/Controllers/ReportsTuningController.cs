using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using ReportTuner.Models;
using NHibernate.Criterion;
using ReportTuner.Helpers;

namespace ReportTuner.Controllers
{
	[Layout("MainLayout"), Helper(typeof(ViewHelper))]
	public class ReportsTuningController : SmartDispatcherController
	{
		public void SelectClients(ulong? report, int? sortOrder, int? startPage, int? pageSize,
			int? rowsCount, int? currentPage, ulong? region, string addBtn, string delBtn, string r,
			string rp, ulong? rpv)
		{
			if (!report.HasValue)
				report = 552;
			if (!rpv.HasValue)
				rpv = 3575;

			ControllerHelper.InitParameter(ref sortOrder, "sortOrder", 2, PropertyBag);
			ControllerHelper.InitParameter(ref startPage, "startPage", 0, PropertyBag);
			ControllerHelper.InitParameter(ref pageSize, "pageSize", 20, PropertyBag);
			ControllerHelper.InitParameter(ref currentPage, "currentPage", 0, PropertyBag);
			ControllerHelper.InitParameter(ref region, "region", ulong.MaxValue, PropertyBag);

			if (delBtn != null)
			{
				foreach (string key in Request.Params.AllKeys)
					if (key.StartsWith("chd"))
						ReportTunerModel.DeleteClient(rpv.Value, Convert.ToUInt64(Request.Params[key]));

				Response.RedirectToUrl(
					String.Format("SelectClients.rails?sortOrder={0}&startPage={1}&pageSize={2}&currentPage={3}&region={4}",
						sortOrder, startPage, pageSize, currentPage, region));
			}

			if (addBtn != null)
			{
				foreach (string key in Request.Params.AllKeys)
					if (key.StartsWith("cha"))
						ReportTunerModel.AddClient(rpv.Value, Convert.ToUInt64(Request.Params[key]));

				Response.RedirectToUrl(
					String.Format("SelectClients.rails?sortOrder={0}&startPage={1}&pageSize={2}&currentPage={3}&region={4}", 
						sortOrder, startPage, pageSize, currentPage, region));
			}

			var regions = Region.FindAll();
			PropertyBag["Regions"] = regions;

			PropertyBag["FilteredClients"] =
				ReportTunerModel.GetAllSuppliers(rpv.Value, sortOrder.Value, currentPage.Value, pageSize.Value, ref rowsCount, region.Value);

			PropertyBag["AddedClients"] =
				ReportTunerModel.GetAddedSuppliers(report.Value, rpv.Value, sortOrder.Value, startPage.Value, pageSize.Value, ref rowsCount);

			PropertyBag["rowsCount"] = rowsCount.Value;
		}
	}
}
