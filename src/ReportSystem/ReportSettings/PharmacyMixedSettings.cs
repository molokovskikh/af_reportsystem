using System.Collections.Generic;
using Inforoom.ReportSystem.Filters;
namespace Inforoom.ReportSystem.ReportSettings
{
	public class PharmacyMixedSettings : BaseReportSettings
	{
		public PharmacyMixedSettings(ulong reportCode, string reportCaption, List<string> filter, List<FilterField> selectedField) 
			: base(reportCode, reportCaption)
		{
			Filter = filter;
			SelectedField = selectedField;
		}

		public List<string> Filter;
		public List<FilterField> SelectedField;
	}
}
