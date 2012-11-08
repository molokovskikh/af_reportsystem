using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support;
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

		[Test]
		public void Shedule_null_firm_code()
		{
			var gr = session.Query<GeneralReport>().ToList().First();
			gr.FirmCode = null;
			session.SaveOrUpdate(gr);
			Assert.IsNull(gr.FirmCode);
			Open("Reports/Schedule.aspx?r=" + gr.Id);
			AssertText("Выполнить отчет за указанный период и отослать по выбранным адресам");
		}

		[Test]
		public void Check_gile_witch_description()
		{
			var gr = session.Query<GeneralReport>().ToList().First();
			gr.SendDescriptionFile = false;
			session.SaveOrUpdate(gr);
			Open(string.Format("Reports/Reports.aspx?r={0}", gr.Id));
			Assert.IsFalse(browser.CheckBox("SendDescriptionFile").Checked);
			browser.CheckBox("SendDescriptionFile").Checked = true;
			Click("Применить");
			session.Refresh(gr);
			Assert.IsTrue(gr.SendDescriptionFile);
		}

		[Test]
		public void RecipietntsRemovedTest()
		{
			browser = Open("Reports/Reports.aspx?r=1");
			Assert.That(browser.Text, Is.Not.Contains("Получатель отчета"));
		}

		[Test]
		public void Select_current_value()
		{
			var payer = new TestPayer();
			var org = new TestLegalEntity(payer, "Тестовое юр. лицо");
			session.Save(payer);
			session.Save(org);
			session.Flush();
			org.Name += " " + org.Id;
			session.Save(org);

			var type = session.Query<ReportType>().First(t => t.ReportClassName.EndsWith("WaybillsReport"));
			var generalReport = new GeneralReport(session.Load<Payer>(payer.Id));
			var report = generalReport.AddReport(type);
			session.Save(generalReport);
			session.Save(report);
			//что сработал триггер который создаст параметры
			session.Flush();

			report.Refresh();
			report.Properties.First(p => p.PropertyType.PropertyName == "OrgId").Value = org.Id.ToString();
			session.Save(report);

			Open("Reports/ReportProperties.aspx?rp={0}&r={1}", report.Id, report.GeneralReport.Id);
			var select = browser.SelectList(s => s.Name.EndsWith("ddlValue"));
			Assert.That(select.SelectedItem, Is.EqualTo(org.Name));
		}
	}
}
