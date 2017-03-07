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
			var select = new SelectElement(browser.FindElementsByCssSelector("select").Last());
			select.SelectByText("Отчет по минимальным ценам по возрастанию по прайсу");
			var row = GetParent(browser.FindElements(By.CssSelector("td"))
				.Last(c => c.Text?.Contains("...") == true));
			var name = GetCell(row, 1).FindElement(By.CssSelector("input[type=\"text\"]"));
			var newReportName = "Для теста" + DateTime.Now.ToString(CultureInfo.InvariantCulture);
			name.SendKeys(newReportName);
			Click("Применить");

			browser.FindElementsByCssSelector("a").Last().Click();
			// выставляем параметр клинта
			row = GetParent(FindCell("Клиент"));
			var cell = row.FindElements(By.CssSelector("td")).Skip(1).First();
			name = cell.FindElement(By.CssSelector("input[type=\"text\"]"));
			name.SendKeys("Тест");
			cell.FindElement(By.CssSelector("input[type=\"submit\"]")).Click();
			Click("Применить");

			// проверяем, что клиент установлен
			row = GetParent(FindCell("Клиент"));
			cell = row.FindElements(By.CssSelector("td")).Skip(1).First();
			Assert.That(cell.FindElements(By.CssSelector("input[type=\"text\"]")).Count, Is.EqualTo(0));
			Assert.That(cell.FindElements(By.CssSelector("select")).Count, Is.EqualTo(1));

			// сохраняем отчет с опцией по базовым ценам, после чего эту опцию снимаем
			SetProperty("По базовым ценам", true);
			HandleStatestate(() => Click("Применить"));

			SetProperty("По базовым ценам", false);
			HandleStatestate(() => Click("Применить"));

			// проверяем, что настройка клиента сброшена
			row = GetParent(FindCell("Клиент"));
			cell = GetCell(row, 1);
			Assert.That(cell.FindElements(By.CssSelector("input[type=\"text\"]")).Count, Is.EqualTo(1));
			Assert.That(cell.FindElements(By.CssSelector("select")).Count, Is.EqualTo(0));
			// удаляем созданный отчет

			Open("Reports/Reports.aspx?r=1");
			cell = browser.FindElementsByCssSelector("td").Last(c => !String.IsNullOrEmpty(c.Text)
				&& c.Text.Contains("Отчет по минимальным ценам по возрастанию по прайсу"));
			row = GetParent(cell);
			Click(GetCell(row, 4), "Удалить");
			Click("Применить");
		}

		private static IWebElement GetCell(IWebElement row, int index)
		{
			return row.FindElements(By.CssSelector("td")).Skip(index).First();
		}

		private IWebElement FindCell(string text)
		{
			return browser.FindElements(By.CssSelector("td")).First(x => x.Text.Contains(text));
		}

		[Test]
		public void BaseWeightCostTest()
		{
			OpenReport(CreateReport("Spec"));

			AssertText("По базовым ценам");

			SetProperty("По базовым ценам", true);
			AssertNoText("По взвешенным ценам");
			AssertText("Список значений \"Региона\"");
			AssertNoText("Список доступных клиенту регионов");
			Click("Применить");

			AssertText("По базовым ценам");
			SetProperty("По базовым ценам", false);
			AssertText("По взвешенным ценам");
			SetProperty("По взвешенным ценам", true);

			AssertNoText("По базовым ценам");
			AssertText("Список значений \"Региона\"");
			AssertNoText("Список доступных клиенту регионов");
			Click("Применить");
			AssertText("По взвешенным ценам");
			SetProperty("По взвешенным ценам", false);
			HandleStatestate(() => Click("Применить"));
			SetProperty("По взвешенным ценам", true);
			HandleStatestate(() => Click("Добавить параметр"));
			var select = new SelectElement(browser.FindElementsByCssSelector("select").Last());
			Assert.That(select.Options.Count(option => option.Text == "Пользователь") == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("Прайс")) == 0);
			Assert.That(select.Options.Count(option => option.Text.Contains("поставщик")) > 0);

			SetProperty("По взвешенным ценам", false);
			SetProperty("По базовым ценам", true);

			HandleStatestate(() => {
				select = new SelectElement(browser.FindElementsByCssSelector("select").Last());
				Assert.That(select.Options.Count(option => option.Text == "Пользователь") == 0);
				Assert.That(select.Options.Count(option => option.Text.Contains("Прайс")) > 0);
				Assert.That(select.Options.Count(option => option.Text.Contains("поставщик")) > 0);
			});
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
			var select = new SelectElement(GetCell(row, 1).FindElement(By.CssSelector("select")));
			Assert.That(select.SelectedOption.Text, Is.StringEnding(org.Name));
		}

		private void SetProperty(string name, bool value)
		{
			HandleStatestate(() => {
				var row = GetParent(FindCell(name));
				var cell = GetCell(row, 1);
				var checkBox = cell.FindElement(By.CssSelector("input[type=\"checkbox\"]"));
				SetChecked(checkBox, value);
			});
		}

		private void SetChecked(IWebElement checkBox, bool value)
		{
			if (checkBox.Selected != value)
				checkBox.Click();
		}
	}
}
