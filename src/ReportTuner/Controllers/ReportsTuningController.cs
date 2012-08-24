using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using Common.Web.Ui.NHibernateExtentions;
using NHibernate;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using ReportTuner.Helpers;
using ReportTuner.Models;
using NHibernate.Criterion;
using Test.Support.log4net;
using ControllerHelper = ReportTuner.Helpers.ControllerHelper;
using ViewHelper = ReportTuner.Helpers.ViewHelper;

namespace ReportTuner.Controllers
{
	public class AddressesFilter : Sortable, IPaginable
	{
		public UInt64 GeneralReport { get; set; }
		public UInt64 Report { get; set; }
		public UInt64 ReportPropertyValue { get; set; }
		public string addressText { get; set; }

		public ISession DbSession;
		public IList<Address> ThisAddress;

		public int _lastRowsCount;

		public int RowsCount
		{
			get { return _lastRowsCount; }
		}

		public int PageSize
		{
			get { return 25; }
		}

		public int CurrentPage { get; set; }

		public AddressesFilter()
		{
			SortKeyMap = new Dictionary<string, string> {
				{ "Code", "Id" },
				{ "Value", "Value" },
				{ "Client", "c.ShortName" },
				{ "Region", "r.Id" }
			};

			SortBy = "Client";
			SortDirection = "asc";
		}

		public IList<Address> Find()
		{
			var addresses = DbSession.QueryOver<Address>();

			var _report = DbSession.Get<Report>(Report);
			var reportProperties = _report.ReportType.Properties;

			var clientCodeEqual = reportProperties.First(rp => rp.PropertyName == "ClientCodeEqual");
			var clientCodeNonEqual = reportProperties.First(rp => rp.PropertyName == "ClientCodeNonEqual");
			var payerEqual = reportProperties.First(rp => rp.PropertyName == "PayerEqual");
			var payerNonEqual = reportProperties.First(rp => rp.PropertyName == "PayerNonEqual");
			var regionEqual = reportProperties.First(rp => rp.PropertyName == "RegionEqual");
			var regionNonEqual = reportProperties.First(rp => rp.PropertyName == "RegionNonEqual");

			var clientCodeProperty = _report.Properties.FirstOrDefault(rp => rp.PropertyType == clientCodeEqual);
			if (clientCodeProperty != null && clientCodeProperty.Values.Count > 0) {
				var clientIds = clientCodeProperty.Values.Select(v => Convert.ToUInt32(v.Value)).ToList();
				addresses.Where(a => a.Client.Id.IsIn(clientIds));
			}

			var payerCodeProperty = _report.Properties.FirstOrDefault(rp => rp.PropertyType == payerEqual);
			if (payerCodeProperty != null && payerCodeProperty.Values.Count > 0) {
				var payerIds = payerCodeProperty.Values.Select(v => Convert.ToUInt32(v.Value)).ToList();
				addresses.Where(a => a.Payer.Id.IsIn(payerIds));
			}

			var clientCodeNonProperty = _report.Properties.FirstOrDefault(rp => rp.PropertyType == clientCodeNonEqual);
			if (clientCodeNonProperty != null && clientCodeNonProperty.Values.Count > 0) {
				var clientIds = clientCodeNonProperty.Values.Select(v => Convert.ToUInt32(v.Value)).ToList();
				addresses.Where(a => !a.Client.Id.IsIn(clientIds));
			}

			var payerNonEqualProperty = _report.Properties.FirstOrDefault(rp => rp.PropertyType == payerNonEqual);
			if (payerNonEqualProperty != null && payerNonEqualProperty.Values.Count > 0) {
				var payerIds = payerNonEqualProperty.Values.Select(v => Convert.ToUInt32(v.Value)).ToList();
				addresses.Where(a => !a.Payer.Id.IsIn(payerIds));
			}

			var criteria = addresses.RootCriteria
				.CreateCriteria("Client", "c", JoinType.InnerJoin)
				.CreateAlias("c.HomeRegion", "r", JoinType.InnerJoin);

			var regionEqualProperty = _report.Properties.FirstOrDefault(rp => rp.PropertyType == regionEqual);
			if (regionEqualProperty != null && regionEqualProperty.Values.Count > 0) {
				var regions = regionEqualProperty.Values.Select(v => Convert.ToUInt64(v.Value)).ToList();
				if (regions.Count > 0) {
					AbstractCriterion projection = Restrictions.Gt(Projections2.BitOr("r.Id", regions[0]), 0);
					for (int i = 1; i < regions.Count; i++) {
						projection |= Restrictions.Gt(Projections2.BitOr("r.Id", regions[i]), 0);
					}
					criteria.Add(projection);
				}
			}

			var regionNonEqualProperty = _report.Properties.FirstOrDefault(rp => rp.PropertyType == regionNonEqual);
			if (regionNonEqualProperty != null && regionNonEqualProperty.Values.Count > 0) {
				var regions = regionNonEqualProperty.Values.Select(v => Convert.ToUInt64(v.Value)).ToList();
				if (regions.Count > 0) {
					AbstractCriterion projection = Restrictions.Gt(Projections2.BitOr("r.Id", regions[0]), 0);
					for (int i = 1; i < regions.Count; i++) {
						projection |= Restrictions.Eq(Projections2.BitOr("r.Id", regions[i]), 0);
					}
					criteria.Add(projection);
				}
			}

			var thisAddrIds = DbSession.Get<ReportProperty>(ReportPropertyValue).Values.Select(v => Convert.ToUInt64(v.Value)).ToArray();

			addresses.Where(a => !a.Id.IsIn(thisAddrIds));
			addresses.Where(a => a.Enabled);

#if DEBUG
			QueryCatcher.Catch();
#endif
			if (!string.IsNullOrEmpty(addressText))
				addresses.And(Restrictions.On<Address>(l => l.Value).IsLike(addressText, MatchMode.Anywhere));

			ApplySort(addresses.RootCriteria);

			addresses.RootCriteria.AddOrder(Order.Asc("Value"));

			if (CurrentPage > 0)
				addresses.RootCriteria.SetFirstResult(CurrentPage * PageSize);

			addresses.RootCriteria.SetMaxResults(PageSize);

			var addressList = addresses.List();

			_lastRowsCount = addresses.RowCount();

			ThisAddress = DbSession.QueryOver<Address>().Where(a => a.Id.IsIn(thisAddrIds)).List();

			return addressList.ToList();
		}
	}

	[Layout("MainLayout"),
	 Helper(typeof(ViewHelper)),
	 Helper(typeof(PaginatorHelper)),
	 Helper(typeof(ReportAppHelper), "app")]
	public class ReportsTuningController : BaseController
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

			if (delBtn != null) {
				foreach (string key in Request.Params.AllKeys)
					if (key.StartsWith("chd"))
						ReportTunerModel.DeleteClient(rpv.Value, Convert.ToUInt64(Request.Params[key]));

				Response.RedirectToUrl(
					String.Format("SelectClients.rails?sortOrder={0}&startPage={1}&pageSize={2}&currentPage={3}&region={4}&report={5}&rpv={6}&firmType={7}&r={8}&userId={9}",
						sortOrder, startPage, pageSize, currentPage, region, report, rpv, firmType, r, userId));
				return;
			}

			if (addBtn != null) {
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

			PropertyBag["Regions"] = ReportTunerModel.GetAllRegions();

			PropertyBag["FilteredClients"] =
				ReportTunerModel.GetAllSuppliers(rpv.Value, sortOrder.Value, currentPage.Value,
					pageSize.Value, ref rowsCount, region.Value, firmType, findStr, userId);

			PropertyBag["AddedClients"] =
				ReportTunerModel.GetAddedSuppliers(report.Value, rpv.Value, sortOrder.Value, startPage.Value, pageSize.Value);

			PropertyBag["rowsCount"] = rowsCount.Value;
		}

		public void SelectAddresses([DataBind("filter")] AddressesFilter filter)
		{
			filter.DbSession = DbSession;
			PropertyBag["addresses"] = filter.Find();
			PropertyBag["thisAddresses"] = filter.ThisAddress;
			PropertyBag["filter"] = filter;
		}

		[AccessibleThrough(Verb.Get)]
		public void ChangeAddressSet(UInt64 r, UInt64 report, UInt64 rpv, string addBtn, string delBtn)
		{
			if (delBtn != null) {
				foreach (string key in Request.Params.AllKeys)
					if (key.StartsWith("chd"))
						ReportTunerModel.DeleteClient(rpv, Convert.ToUInt64(Request.Params[key]));

				RedirectToReferrer();
				return;
			}

			if (addBtn != null) {
				foreach (string key in Request.Params.AllKeys)
					if (key.StartsWith("cha"))
						ReportTunerModel.AddClient(rpv, Convert.ToUInt64(Request.Params[key]));

				RedirectToReferrer();
				return;
			}
		}

		public void FileForReportTypes()
		{
			PropertyBag["FileForReportTypes"] = DbSession.Query<FileForReportType>().ToList();
			PropertyBag["ReportTypes"] = DbSession.Query<ReportType>().ToList();
		}

		public void SaveFilesForReportType()
		{
			if (!Directory.Exists(Global.Config.SavedFileForReportTypesPath))
				Directory.CreateDirectory(Global.Config.SavedFileForReportTypesPath);
			foreach (var key in Request.Files.Keys) {
				var file = GetFileInRequest(key);
				if (file != null && file.ContentLength != 0) {
					var reportType = DbSession.Get<ReportType>(Convert.ToUInt32(key));
					var newFile = reportType.File;
					if (newFile == null)
						newFile = new FileForReportType { File = file.FileName, ReportType = reportType };
					else {
						File.Delete(newFile.FillPath);
						newFile.File = file.FileName;
					}
					DbSession.SaveOrUpdate(newFile);
					using (Stream intoStream = File.OpenWrite(newFile.FillPath)) {
						FileHelper.CopyStream(file.InputStream, intoStream);
					}
				}
			}
			RedirectToReferrer();
		}

		public void GetFileForReportType(uint id)
		{
			var file = DbSession.Get<FileForReportType>(id);
			this.RenderFile(file.FillPath, file.File);
		}

		public void DeleteFileForReportType(uint fileId)
		{
			var file = DbSession.Get<FileForReportType>(fileId);
			if (File.Exists(file.FillPath))
				File.Delete(file.FillPath);
			DbSession.Delete(file);
			CancelView();
		}
	}
}
