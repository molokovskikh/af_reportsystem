using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Test.Support.Web;
using WatiN.Core;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class CopyReportFixture : WatinFixture2
	{
		[Test]
		public void SelectReportLinkTest()
		{
			Open("CopyReport/SelectReport?filter.Report=1&filter.GeneralReport=1&filter.ReportName=Тест");
			browser.Link(Find.ByText("Тестовый отчет АК Инфорум")).Url.Contains("filter.GeneralReport=1&filter.Report=1&destId=1");
			Assert.That(browser.Link(Find.ByText("Тестовый отчет АК Инфорум")).Url.Contains("filter.GeneralReport=1&filter.Report=1&destId=1"));
			Click("Показать");
			Assert.That(browser.Link(Find.ByText("Тестовый отчет АК Инфорум")).Url.Contains("filter.GeneralReport=1&filter.Report=1&destId=1"));
		}
	}
}
