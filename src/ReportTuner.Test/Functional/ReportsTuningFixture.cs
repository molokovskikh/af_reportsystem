using System;
using System.Globalization;
using System.Linq;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using ReportTuner.Models;
using ReportTuner.Test.TestHelpers;

namespace ReportTuner.Test.Functional
{
	[TestFixture]
	public class ReportsTuningFixture : ReportSeleniumFixture
	{
		[Test]
		public void ResetClientCodeIfBaseCost()
		{
			Open("Reports/Reports.aspx?r=1");
			// создвем новый отчет
			Click("Добавить");
			var select = new SelectElement(browser.FindElementsByClassName("select").Last());
			select.SelectByText("Отчет по минимальным ценам по возрастанию по прайсу");
			var row = GetParent(browser.FindElements(By.CssSelector("td"))
				.Last(c => !String.IsNullOrEmpty(c.Text) && c.Text.Contains("...")));
			var name = row.FindElement(By.ClassName("td")).FindElement(By.CssSelector("input[type=\"text\"]"));
			var newReportName = "Для теста" + DateTime.Now.ToString(CultureInfo.InvariantCulture);
			name.SendKeys(newReportName);
			Click("Применить");

			browser.FindElementByClassName("a").Click();
			// выставляем параметр клинта
			row = GetParent(FindCell("Клиент"));
			var cell = row.FindElements(By.ClassName("td")).Skip(1).First();
			name = cell.FindElement(By.CssSelector("input[type=\"text\"]"));
			name.SendKeys("Тест");
			cell.FindElement(By.CssSelector("input[type=\"submit\"]")).Click();
			Click("Применить");

			// проверяем, что клиент установлен
			row = GetParent(FindCell("Клиент"));
			cell = row.FindElements(By.ClassName("td")).Skip(1).First();
			Assert.That(cell.FindElements(By.CssSelector("input[type=\"text\"]")).Count, Is.EqualTo(0));
			Assert.That(cell.FindElements(By.ClassName("select")).Count, Is.EqualTo(1));
			// сохраняем отчет с опцией по базовым ценам, после чего эту опцию снимаем
			row = GetParent(FindCell("По базовым ценам"));
			SetChecked(row.FindElements(By.ClassName("td")).Skip(1).First(), true);
			Click("Применить");

			row = GetParent(FindCell("По базовым ценам"));
			SetChecked(GetCell(row, 1), false);
			Click("Применить");
			// проверяем, что настройка клиента сброшена
			row = GetParent(FindCell("Клиент"));
			cell = GetCell(row, 1);
			Assert.That(cell.FindElements(By.CssSelector("input[type=\"text\"]")).Count, Is.EqualTo(1));
			Assert.That(cell.FindElements(By.ClassName("select")).Count, Is.EqualTo(0));
			// удаляем созданный отчет

			Open("Reports/Reports.aspx?r=1");
			cell = browser.FindElementsByClassName("td").Last(c => !String.IsNullOrEmpty(c.Text)
				&& c.Text.Contains("Отчет по минимальным ценам по возрастанию по прайсу"));
			row = GetParent(cell);
			Click(GetCell(row, 4), "Удалить");
			Click("Применить");
		}

		private static IWebElement GetCell(IWebElement row, int index)
		{
			return row.FindElements(By.ClassName("td")).Skip(index).First();
		}

		private IWebElement FindCell(string text)
		{
			return browser.FindElements(By.ClassName("td")).First(x => x.Text.Contains(text));
		}

		[Test]
		public void BaseWeightCostTest()
		{
			var report = CreateReport("Spec");
			OpenReport(report);

			AssertText("По базовым ценам");

			Checked("По базовым ценам", true);
			AssertNoText("По взвешенным ценам");
			AssertText("Список значений \"Региона\"");
			AssertNoText("Список доступных клиенту регионов");
			Click("Применить");

			AssertText("По базовым ценам");
			Checked("По базовым ценам", false);
			AssertText("По взвешенным ценам");
			Checked("По взвешенным ценам", true);

			AssertNoText("По базовым ценам");
			AssertText("Список значений \"Региона\"");
			AssertNoText("Список доступных клиенту регионов");
			Click("Применить");
			AssertText("По взвешенным ценам");
			Checked("По взвешенным ценам", false);
			Click("Применить");
			Checked("По взвешенным ценам", true);
			Click("Добавить параметр");
			var select = new SelectElement(browser.FindElementsByClassName("select").Last());
			Assert.That(select.Options.Count(option => option.Text == "Пользователь") == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("Прайс")) == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("поставщик")) > 0);

			Checked("По взвешенным ценам", false);
			Checked("По базовым ценам", true);

			select = new SelectElement(browser.FindElementsByClassName("select").Last());
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
			session.Save(gr);
			Assert.IsNull(gr.FirmCode);
			Open("Reports/Schedule.aspx?r=" + gr.Id);
			AssertText("Выполнить отчет за указанный период и отослать по выбранным адресам");
		}

		[Test]
		public void Check_gile_witch_description()
		{
			var gr = session.Query<GeneralReport>().ToList().First();
			gr.SendDescriptionFile = false;
			session.Save(gr);
			Open($"Reports/Reports.aspx?r={gr.Id}");
			var checkBox = browser.FindElementById("SendDescriptionFile");
			Assert.IsFalse(checkBox.Selected);
			checkBox.Click();
			Click("Применить");
			session.Refresh(gr);
			Assert.IsTrue(gr.SendDescriptionFile);
		}

		[Test]
		public void RecipietntsRemovedTest()
		{
			Open("Reports/Reports.aspx?r=1");
			AssertNoText("Получатель отчета");
		}

		[Test]
		public void Select_current_value()
		{
			var report = CreateReport("WaybillsReport");
			var org = payer.Orgs.First();
			report.Properties.First(p => p.PropertyType.PropertyName == "OrgId").Value = org.Id.ToString();
			session.Save(report);
			OpenReport(report);

			var row = GetParent(FindCell("Юридическое лицо накладные которого будут включены в отчет"));
			var select = new SelectElement(row.FindElements(By.ClassName("td")).First().FindElement(By.CssSelector("select")));
			Assert.That(select.SelectedOption.Text, Is.StringEnding(org.Name));
		}

		private void Checked(string name, bool value)
		{
			var row = GetParent(FindCell(name));
			var cell = row.FindElements(By.CssSelector("td")).Skip(1).First();
			var checkBox = cell.FindElement(By.CssSelector("input[type=\"checkbox\"]"));
			SetChecked(checkBox, value);
		}

		private void SetChecked(IWebElement checkBox, bool value)
		{
			if (checkBox.Selected != value)
				checkBox.Click();
		}
	}
}
