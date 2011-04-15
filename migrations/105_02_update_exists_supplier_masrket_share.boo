import ReportTuner.Models

reportType = ReportType.Find(cast(ulong, 15))
propertyType = reportType.GetProperty("Type")
for report in Report.FindAll():
	continue if report.ReportType.Id != cast(ulong, 15)
	property = ReportProperty(ReportCode: report.Id, Value: "0", PropertyType: propertyType)
	property.Save()
