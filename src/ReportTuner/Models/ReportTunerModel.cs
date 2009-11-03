using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Criterion;

namespace ReportTuner.Models
{
	public static class ReportTunerModel
	{
		private static string FormatRegions(Region[] regions)
		{
			string result = regions.Aggregate<Region, string>("", (res, reg) => res += reg.Name + ", ");
			if (result.Length > 2)
				return result.Substring(0, result.Length - 2);
			else
				return String.Empty;
		}

		public static object[] GetAllSuppliers(ulong reportProperty, int sortOrder, int currenPage, int pageSize, 
			ref int? rowsCount, ulong region)
		{
			var addedClients = ReportPropertyValue.FindAll(Expression.Eq("ReportPropertyId", reportProperty));

			string[] clientsCodes = (from cl in addedClients
									 select cl.Value).ToArray();

			ICriterion[] criteries = new[] { 
				Expression.Sql("(MaskRegion & " + region + ")>0"),
				Expression.Eq("FirmType", 0),
				Expression.Not(Expression.In("Id", clientsCodes))};

			string[] headers = new[] { "", "Id", "ShortName", "RegionCode" };
			Order[] orders = new[]{ new Order(headers[Math.Abs(sortOrder)-1], (sortOrder>0)) };

			if (!rowsCount.HasValue)
				rowsCount = Client.FindAll(criteries).Length;

			var clients = Client.SlicedFindAll(currenPage * pageSize, pageSize, orders, criteries);

			var regions = Region.FindAll();

			var clientsWithRegions = from cl in clients
									 select new{
										 Id = cl.Id,
										 ShortName = cl.ShortName,
										 Regions = FormatRegions(regions.Where(r => (r.RegionCode & cl.MaskRegion) > 0).ToArray())};

			return clientsWithRegions.ToArray();
		}

		public static object[] GetAddedSuppliers(ulong reportCode, ulong reportProperty, int sortOrder, int startPage,
			int pageSize, ref int? rowsCount)
		{
			var report = Report.Find(reportCode);

			var property = ReportProperty.Find(reportProperty);

			var clients = Client.FindAll();
			var regions = Region.FindAll();
			var result = from cl in clients
						 where property.Values.Any(pr => pr.Value == cl.Id.ToString())
						 select new
						 {
							 Id = cl.Id,
							 ShortName = cl.ShortName,
							 Regions = FormatRegions(regions.Where(r => (r.RegionCode & cl.MaskRegion) > 0).ToArray())
						 };
			return result.ToArray();
		}

		public static void DeleteClient(ulong reportProperty, ulong clientCode)
		{
			var properties = ReportPropertyValue.FindAll(new[] 
				{Expression.Eq("ReportPropertyId", reportProperty), Expression.Eq("Value", clientCode.ToString())});

			foreach(var property in properties)
				property.DeleteAndFlush();
		}

		public static void AddClient(ulong reportProperty, ulong clientCode)
		{
			var property = new ReportPropertyValue();
			property.ReportPropertyId = reportProperty;
			property.Value = clientCode.ToString();

			property.CreateAndFlush();
		}
	}
}
