using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using ReportTuner.Models;

namespace ReportTuner.Controllers
{
	public class PropertiesController : SmartDispatcherController
	{
		public void File(ulong id)
		{
			var property = ReportProperty.Find(id);
			this.RenderFile(property.Filename, property.Value);
		}
	}
}