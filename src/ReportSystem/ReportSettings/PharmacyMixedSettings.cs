using System.Collections.Generic;
using Inforoom.ReportSystem.Filters;

namespace Inforoom.ReportSystem.ReportSettings
{
	public class PharmacyMixedSettings : BaseReportSettings
	{
		public PharmacyMixedSettings(ulong reportCode, string reportCaption, IList<string> filter, List<FilterField> selectedField,
			List<ColumnGroupHeader> groupHeaders)
			: base(reportCode, reportCaption)
		{
			Filter = filter;
			SelectedField = selectedField;
			GroupHeaders = groupHeaders;
		}

		public IList<string> Filter;
		public List<FilterField> SelectedField;
		public List<ColumnGroupHeader> GroupHeaders;
	}
}