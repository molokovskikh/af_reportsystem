using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support.Web;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class ReportsTuningFixture : WatinFixture2
	{
		[Test]
		public void BaseWeightCostTest()
		{
			Open(string.Format("Reports/ReportProperties.aspx?rp=1&r=1"));
			AssertText("По базовым ценам");
			var baseRow = browser.TableCell(Find.ByText("По базовым ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = true;
			Assert.That(browser.Text, Is.Not.Contains("По взвешенным ценам"));
			AssertText("Список значений \"Региона\"");
			Assert.That(browser.Text, Is.Not.Contains("Список доступных клиенту регионов"));
			browser.Button(Find.ByValue("Применить")).Click();
			AssertText("По базовым ценам");
			baseRow = browser.TableCell(Find.ByText("По базовым ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = false;
			AssertText("По взвешенным ценам");
			baseRow = browser.TableCell(Find.ByText("По взвешенным ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = true;
			Assert.That(browser.Text, Is.Not.Contains("По базовым ценам"));
			AssertText("Список значений \"Региона\"");
			Assert.That(browser.Text, Is.Not.Contains("Список доступных клиенту регионов"));
			browser.Button(Find.ByValue("Применить")).Click();
			AssertText("По взвешенным ценам");
			baseRow = browser.TableCell(Find.ByText("По взвешенным ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = false;
			browser.Button(Find.ByValue("Применить")).Click();
			baseRow = browser.TableCell(Find.ByText("По взвешенным ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = true;
			browser.Button(Find.ByValue("Добавить")).Click();
			var select = browser.SelectLists.Last();
			Assert.That(select.Options.Count(option => option.Text == "Пользователь") == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("Прайс")) == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("поставщик")) > 0);
			baseRow = browser.TableCell(Find.ByText("По взвешенным ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = false;
			baseRow = browser.TableCell(Find.ByText("По базовым ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = true;
			select = browser.SelectLists.Last();
			Assert.That(select.Options.Count(option => option.Text == "Пользователь") == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("Прайс")) > 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("поставщик")) > 0);
			browser.ShowWindow(NativeMethods.WindowShowStyle.Maximize);
		}
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
	}
}
