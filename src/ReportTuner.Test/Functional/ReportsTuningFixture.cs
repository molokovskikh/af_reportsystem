using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using NUnit.Framework;
using ReportTuner.Models;
using Test.Support;
using Test.Support.Web;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class ReportsTuningFixture : WatinFixture2
	{
		TestPayer payer;

		[Test]
		public void ResetClientCodeIfBaseCost()
		{
			Open(string.Format("Reports/Reports.aspx?r=1"));
			// создвем новый отчет
			browser.Button(Find.ByValue("Добавить")).Click();
			var select = browser.SelectLists.Last();
			select.Select("Отчет по минимальным ценам по возрастанию по прайсу");
			var row = browser.TableCells
				.Last(c => !String.IsNullOrEmpty(c.Text) && c.Text.Contains("...")).ContainingTableRow;
			var name = row.OwnTableCells[1].TextFields[0];
			var newReportName = "Для теста" + DateTime.Now.ToString();
			name.AppendText(newReportName);
			browser.Button(Find.ByValue("Применить")).Click();
			var link = browser.Links[browser.Links.Count - 1];
			link.Click();
			// выставляем параметр клинта
			row = browser.TableCell(Find.ByText("Клиент")).ContainingTableRow;
			name = row.OwnTableCells[1].TextFields[0];
			name.AppendText("Тест");
			row.OwnTableCells[1].Buttons[0].Click();
			browser.Button(Find.ByValue("Применить")).Click();
			// проверяем, что клиент установлен
			row = browser.TableCell(Find.ByText("Клиент")).ContainingTableRow;
			Assert.That(row.OwnTableCells[1].TextFields.Count, Is.EqualTo(0));
			Assert.That(row.OwnTableCells[1].SelectLists.Count, Is.EqualTo(1));
			// сохраняем отчет с опцией по базовым ценам, после чего эту опцию снимаем
			var baseRow = browser.TableCell(Find.ByText("По базовым ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = true;
			browser.Button(Find.ByValue("Применить")).Click();
			baseRow = browser.TableCell(Find.ByText("По базовым ценам")).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = false;
			browser.Button(Find.ByValue("Применить")).Click();
			// проверяем, что настройка клиента сброшена
			row = browser.TableCell(Find.ByText("Клиент")).ContainingTableRow;
			Assert.That(row.OwnTableCells[1].TextFields.Count, Is.EqualTo(1));
			Assert.That(row.OwnTableCells[1].SelectLists.Count, Is.EqualTo(0));
			// удаляем созданный отчет
			Open(string.Format("Reports/Reports.aspx?r=1"));
			var cell = browser.TableCells.Last(c => !String.IsNullOrEmpty(c.Text)
				&& c.Text.Contains("Отчет по минимальным ценам по возрастанию по прайсу"));
			row = cell.ContainingTableRow;
			row.OwnTableCells[4].Button(Find.ByValue("Удалить")).Click();
			Click("Применить");
		}
		[Test]
		public void BaseWeightCostTest()
		{
			var report = CreateReport("Spec");
			OpenReport(report);

			browser.ShowWindow(NativeMethods.WindowShowStyle.Maximize);
			AssertText("По базовым ценам");

			Checked("По базовым ценам", true);
			Assert.That(browser.Text, Is.Not.Contains("По взвешенным ценам"));
			AssertText("Список значений \"Региона\"");
			Assert.That(browser.Text, Is.Not.Contains("Список доступных клиенту регионов"));
			Click("Применить");

			AssertText("По базовым ценам");
			Checked("По базовым ценам", false);
			AssertText("По взвешенным ценам");
			Checked("По взвешенным ценам", true);

			Assert.That(browser.Text, Is.Not.Contains("По базовым ценам"));
			AssertText("Список значений \"Региона\"");
			Assert.That(browser.Text, Is.Not.Contains("Список доступных клиенту регионов"));
			Click("Применить");
			AssertText("По взвешенным ценам");
			Checked("По взвешенным ценам", false);
			Click("Применить");
			Checked("По взвешенным ценам", true);
			Click("Добавить параметр");
			var select = browser.SelectLists.Last();
			Assert.That(select.Options.Count(option => option.Text == "Пользователь") == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("Прайс")) == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("поставщик")) > 0);

			Checked("По взвешенным ценам", false);
			Checked("По базовым ценам", true);

			select = browser.SelectLists.Last();
			Assert.That(select.Options.Count(option => option.Text == "Пользователь") == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("Прайс")) > 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("поставщик")) > 0);
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

		[Test]
		public void Select_current_value()
		{
			var report = CreateReport("WaybillsReport");
			var org = payer.Orgs.First();
			report.Properties.First(p => p.PropertyType.PropertyName == "OrgId").Value = org.Id.ToString();
			session.Save(report);
			OpenReport(report);

			var select = browser.SelectList(s => s.Name.EndsWith("ddlValue"));
			Assert.That(select.SelectedItem, Is.StringEnding(org.Name));
		}

		private void Checked(string name, bool value)
		{
			var baseRow = browser.TableCell(Find.ByText(name)).ContainingTableRow;
			baseRow.OwnTableCells[1].CheckBoxes[0].Checked = value;
		}

		private void OpenReport(Report report)
		{
			Open("Reports/ReportProperties.aspx?rp={0}&r={1}", report.Id, report.GeneralReport.Id);
		}

		private Report CreateReport(string reportType)
		{
			payer = new TestPayer();
			var org = new TestLegalEntity(payer, "Тестовое юр. лицо");
			payer.Orgs.Add(org);
			session.Save(payer);
			session.Flush();
			org.Name += " " + org.Id;
			session.Save(org);

			var type = session.Query<ReportType>().First(t => t.ReportTypeFilePrefix == reportType);
			var generalReport = new GeneralReport(session.Load<Payer>(payer.Id));
			var report = generalReport.AddReport(type);
			session.Save(generalReport);
			session.Save(report);
			//что сработал триггер который создаст параметры
			session.Flush();

			report.Refresh();

			return report;
		}
	}
}
