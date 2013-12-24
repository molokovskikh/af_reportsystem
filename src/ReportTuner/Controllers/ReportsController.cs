using System;
using System.Linq;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using ReportTuner.Models;

namespace ReportTuner.Controllers
{
	public class ReportsController : BaseController
	{
		//метод используется в административном интерфейсе при удалении плательщика
		public void Delete(ulong[] ids)
		{
			foreach (var id in ids) {
				try {
					var report = DbSession.Get<GeneralReport>(id);
					if (report == null)
						continue;

					report.RemoveTask();

					foreach (var property in report.Reports.SelectMany(r => r.Properties)) {
						property.CleanupFiles();
					}

					DbSession.Delete(report);
				}
				catch (Exception e) {
					Logger.Error("Ошибка при удалении отчета", e);
				}
			}

			RenderText("");
		}
	}
}