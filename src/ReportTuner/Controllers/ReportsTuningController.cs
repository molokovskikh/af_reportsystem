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
		public void SelectClients(ulong? report, int? sortOrder, int? startPage, int? pageSize, int? rowsCount,
			int? currentPage, ulong? region, string addBtn, string delBtn, ulong? rpv, 
			byte firmType, ulong r, string findStr, ulong? userId)
		{
			ControllerHelper.InitParameter(ref sortOrder, "sortOrder", 2, PropertyBag);
			ControllerHelper.InitParameter(ref startPage, "startPage", 0, PropertyBag);
			ControllerHelper.InitParameter(ref pageSize, "pageSize", 20, PropertyBag);
			ControllerHelper.InitParameter(ref currentPage, "currentPage", 0, PropertyBag);
			ControllerHelper.InitParameter(ref region, "region", ulong.MaxValue, PropertyBag);
			PropertyBag["firmType"] = firmType;
			PropertyBag["findStr"] = findStr;			

			if (delBtn != null)
			{
				foreach (string key in Request.Params.AllKeys)
					if (key.StartsWith("chd"))
						ReportTunerModel.DeleteClient(rpv.Value, Convert.ToUInt64(Request.Params[key]));

				Response.RedirectToUrl(
					String.Format("SelectClients.rails?sortOrder={0}&startPage={1}&pageSize={2}&currentPage={3}&region={4}&report={5}&rpv={6}&firmType={7}&r={8}&userId={9}",
						sortOrder, startPage, pageSize, currentPage, region, report, rpv, firmType, r, userId));
				return;
			}

			if (addBtn != null)
			{
				foreach (string key in Request.Params.AllKeys)
					if (key.StartsWith("cha"))
						ReportTunerModel.AddClient(rpv.Value, Convert.ToUInt64(Request.Params[key]));

				Response.RedirectToUrl(
					String.Format("SelectClients.rails?sortOrder={0}&startPage={1}&pageSize={2}&currentPage={3}&region={4}&report={5}&rpv={6}&firmType={7}&r={8}&userId={9}",
						sortOrder, startPage, pageSize, currentPage, region, report, rpv, firmType, r, userId));
				return;
			}

			if (region == 0)
				region = ulong.MaxValue;

			var regions = ReportTunerModel.GetAllRegions();
			PropertyBag["Regions"] = regions;
						
			PropertyBag["FilteredClients"] =
				ReportTunerModel.GetAllSuppliers(rpv.Value, sortOrder.Value, currentPage.Value,
				                                 pageSize.Value, ref rowsCount, region.Value, firmType, findStr, userId);

			PropertyBag["AddedClients"] =
				ReportTunerModel.GetAddedSuppliers(report.Value, rpv.Value, sortOrder.Value, startPage.Value, pageSize.Value);

			PropertyBag["rowsCount"] = rowsCount.Value;
			
		}
	}
}
