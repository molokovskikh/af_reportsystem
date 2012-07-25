using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using ReportTuner.Models;

namespace ReportTuner.Controllers
{
	public class PropertiesController : BaseController
	{
		public void File(ulong id)
		{
			var property = ReportProperty.Find(id);
			this.RenderFile(property.Filename, property.Value);
		}

		public void FileGeneral(uint id)
		{
			var file = DbSession.Get<FileSendWithReport>(id);
			this.RenderFile(file.FileNameForSave, file.FileName);
		}
	}
}