﻿using NUnit.Framework;
using ReportTuner.Test.TestHelpers;

namespace ReportTuner.Test.Functional
{
	public class MnnPropertyFixture : ReportWatinFixture
	{
		[Test]
		public void Mnn_filter()
		{
			var report = CreateReport("Mixed");
			OpenReport(report);
			Click("Добавить параметр");
			Css("#ctl00_ReportContentPlaceHolder_dgvOptional select").Select("Список значений \"МНН\"");
			Click("Применить");
			Click(Css("#ctl00_ReportContentPlaceHolder_dgvOptional"), "...");
			AssertText("Fusarium sambuсinum грибы");
		}
	}
}