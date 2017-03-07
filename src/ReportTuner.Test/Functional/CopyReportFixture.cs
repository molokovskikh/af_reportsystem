using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Test.Support.Selenium;
using Test.Support.Web;
using WatiN.Core;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class CopyReportFixture : SeleniumFixture
	{
		[Test]
		public void SelectReportLinkTest()
		{
			Open("CopyReport/SelectReport?filter.Report=1&filter.GeneralReport=1&filter.ReportName=Тест");
			Click("Показать");
			AssertText("Тестовый отчет АК Инфорум");
			Click("Тестовый отчет АК Инфорум");
			AssertText("Настройка отчетов");
			Click("Удалить");
			AssertText("Настройка отчетов");
			Click("Применить");
			AssertText("Настройка отчетов");
		}
	}
}
