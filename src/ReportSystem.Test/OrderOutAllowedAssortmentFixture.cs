using System;
using System.Collections.Generic;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOrders;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OrderOutAllowedAssortmentFixture : BaseProfileFixture
	{
		[Test]
		public void Build_report()
		{
			/*AddProperty("ClientCode", 4221);
			AddProperty("Begin", DateTime.Now.AddDays(-10));
			AddProperty("End", DateTime.Now);
			AddProperty("Regions", new List<long> { 1l });
			AddProperty("ByPreviousMonth", false);
			AddProperty("ReportInterval", 12);*/
			var type = ReportsTypes.OrderOutAllowedAssortment;
			var props = TestHelper.LoadProperties(type);
			report = new OrderOutAllowedAssortment(1, "OrderOutAllowedAssortment.xls", Conn, ReportFormats.Excel, props);
			
			BuildReport();
		}
	}
}