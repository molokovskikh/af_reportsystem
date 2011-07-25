import System.Collections.Generic
import ReportTuner.Models

reportTypes = List[of ReportType]()
reportTypes.Add(ReportType.Find(cast(ulong, 1)))
reportTypes.Add(ReportType.Find(cast(ulong, 2)))
reportTypes.Add(ReportType.Find(cast(ulong, 3)))
reportTypes.Add(ReportType.Find(cast(ulong, 5)))
reportTypes.Add(ReportType.Find(cast(ulong, 7)))
reportTypes.Add(ReportType.Find(cast(ulong, 12)))
reportTypes.Add(ReportType.Find(cast(ulong, 13)))
reportTypes.Add(ReportType.Find(cast(ulong, 14)))

for reportType in reportTypes:
		property = ReportTypeProperty("Retail", "BOOL", "Готовить по розничному сегменту")
		reportType.AddProperty(property)
		property.Save()
		reportType.Save()
		reportType.FixExistReports()
