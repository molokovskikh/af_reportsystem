using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Test.Support.Web;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class ReportsTuningFixture : WatinFixture2
	{
		[Test]
		public void FileForReportTypesTest()
		{
			Open("ReportsTuning/FileForReportTypes");
			AssertText("Тип отчета");
			AssertText("Выбор файла");
			AssertText("Существующий файл");
			Click("Сохранить");
			AssertText("Тип отчета");
		}
	}
}
